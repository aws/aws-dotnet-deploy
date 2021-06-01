// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.EC2.Model;
using System.IO;
using AWS.Deploy.CLI.Utilities;

namespace AWS.Deploy.CLI
{
    public interface IAWSUtilities
    {
        Task<AWSCredentials> ResolveAWSCredentials(string? profileName, string? lastUsedProfileName);
        string ResolveAWSRegion(string? region, string? lastRegionUsed);
    }

    public class AWSUtilities : IAWSUtilities
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IConsoleUtilities _consoleUtilities;

        public AWSUtilities(IToolInteractiveService toolInteractiveService, IConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<AWSCredentials> ResolveAWSCredentials(string? profileName, string? lastUsedProfileName)
        {


            async Task<AWSCredentials> Resolve()
            {
            var chain = new CredentialProfileStoreChain();

                if (!string.IsNullOrEmpty(profileName) && chain.TryGetAWSCredentials(profileName, out var profileCredentials) &&
                    // Skip checking CanLoadCredentials for AssumeRoleAWSCredentials because it might require an MFA token and the callback hasn't been setup yet.
                    (profileCredentials is AssumeRoleAWSCredentials || await CanLoadCredentials(profileCredentials)))
            {
                    _toolInteractiveService.WriteLine($"Configuring AWS Credentials from Profile {profileName}.");
                    return profileCredentials;
                }

            if (!string.IsNullOrEmpty(lastUsedProfileName) &&
                    chain.TryGetAWSCredentials(lastUsedProfileName, out var lastUsedCredentials) &&
                    await CanLoadCredentials(lastUsedCredentials))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS Credentials with previous configured profile value {lastUsedProfileName}.");
                    return lastUsedCredentials;
            }

            try
            {
                    var fallbackCredentials = FallbackCredentialsFactory.GetCredentials();

                    if (await CanLoadCredentials(fallbackCredentials))
                {
                    _toolInteractiveService.WriteLine("Configuring AWS Credentials using AWS SDK credential search.");
                        return fallbackCredentials;
                }
            }
            catch (AmazonServiceException)
            {
                // FallbackCredentialsFactory throws an exception if no credentials are found. Burying exception because if no credentials are found
                // we want to continue and ask the user to select a profile.
            }

            var sharedCredentials = new SharedCredentialsFile();
            if (sharedCredentials.ListProfileNames().Count == 0)
            {
                throw new NoAWSCredentialsFoundException("Unable to resolve AWS credentials to access AWS.");
            }

            var selectedProfileName = _consoleUtilities.AskUserToChoose(sharedCredentials.ListProfileNames(), "Select AWS Credentials Profile", null);

                if (chain.TryGetAWSCredentials(selectedProfileName, out var selectedProfileCredentials) &&
                    (await CanLoadCredentials(selectedProfileCredentials)))
            {
                    return selectedProfileCredentials;
                }

                throw new NoAWSCredentialsFoundException($"Unable to create AWS credentials for profile {selectedProfileName}.");
            }

            var credentials = await Resolve();

            if (credentials is AssumeRoleAWSCredentials assumeRoleAWSCredentials)
            {
                var assumeOptions = assumeRoleAWSCredentials.Options;
                assumeOptions.MfaTokenCodeCallback = new AssumeRoleMfaTokenCodeCallback(_toolInteractiveService, assumeOptions).Execute;
            }

            return credentials;
        }

        private async Task<bool> CanLoadCredentials(AWSCredentials credentials)
        {
            if (null == credentials)
                return false;

            try
            {
                await credentials.GetCredentialsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string ResolveAWSRegion(string? region, string? lastRegionUsed)
        {
            if (!string.IsNullOrEmpty(region))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region with specified value {region}.");
                return region;
            }

            if (!string.IsNullOrEmpty(lastRegionUsed))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region with previous configured value {lastRegionUsed}.");
                return lastRegionUsed;
            }

            var fallbackRegion = FallbackRegionFactory.GetRegionEndpoint()?.SystemName;
            if (!string.IsNullOrEmpty(fallbackRegion))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS region using AWS SDK region search to {fallbackRegion}.");
                return fallbackRegion;
            }

            var availableRegions = new List<string>();
            foreach (var value in Amazon.RegionEndpoint.EnumerableAllRegions.OrderBy(x => x.SystemName))
            {
                availableRegions.Add($"{value.SystemName} ({value.DisplayName})");
            }

            var selectedRegion = _consoleUtilities.AskUserToChoose(availableRegions, "Select AWS Region", null);

            // Strip display name
            selectedRegion = selectedRegion.Substring(0, selectedRegion.IndexOf('(') - 1).Trim();

            return selectedRegion;
        }
    }
}
