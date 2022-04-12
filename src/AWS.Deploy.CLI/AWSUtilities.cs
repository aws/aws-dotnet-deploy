// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI
{
    public interface IAWSUtilities
    {
        Task<AWSCredentials> ResolveAWSCredentials(string? profileName);
        string ResolveAWSRegion(string? region, string? lastRegionUsed = null);
    }

    public class AWSUtilities : IAWSUtilities
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IDirectoryManager _directoryManager;

        public AWSUtilities(
            IToolInteractiveService toolInteractiveService,
            IConsoleUtilities consoleUtilities,
            IDirectoryManager directoryManager)
        {
            _toolInteractiveService = toolInteractiveService;
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
        }

        public async Task<AWSCredentials> ResolveAWSCredentials(string? profileName)
        {
            async Task<AWSCredentials> Resolve()
            {
                var chain = new CredentialProfileStoreChain();

                if (!string.IsNullOrEmpty(profileName))
                {
                    if (chain.TryGetAWSCredentials(profileName, out var profileCredentials) &&
                    // Skip checking CanLoadCredentials for AssumeRoleAWSCredentials because it might require an MFA token and the callback hasn't been setup yet.
                    (profileCredentials is AssumeRoleAWSCredentials || await CanLoadCredentials(profileCredentials)))
                    {
                        _toolInteractiveService.WriteLine($"Configuring AWS Credentials from Profile {profileName}.");
                        return profileCredentials;
                    }
                    else
                    {
                        var message = $"Failed to get credentials for profile \"{profileName}\". Please provide a valid profile name and try again.";
                        throw new FailedToGetCredentialsForProfile(DeployToolErrorCode.FailedToGetCredentialsForProfile, message);
                    }
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
                catch (AmazonServiceException ex)
                {
                    // FallbackCredentialsFactory throws an exception if no credentials are found. Burying exception because if no credentials are found
                    // we want to continue and ask the user to select a profile.
                    _toolInteractiveService.WriteDebugLine(ex.PrettyPrint());
                }

                var sharedCredentials = new SharedCredentialsFile();
                if (sharedCredentials.ListProfileNames().Count == 0)
                {
                    throw new NoAWSCredentialsFoundException(DeployToolErrorCode.UnableToResolveAWSCredentials, "Unable to resolve AWS credentials to access AWS.");
                }

                var selectedProfileName = _consoleUtilities.AskUserToChoose(sharedCredentials.ListProfileNames(), "Select AWS Credentials Profile", null);

                if (chain.TryGetAWSCredentials(selectedProfileName, out var selectedProfileCredentials) &&
                    (await CanLoadCredentials(selectedProfileCredentials)))
                {
                    return selectedProfileCredentials;
                }

                throw new NoAWSCredentialsFoundException(DeployToolErrorCode.UnableToCreateAWSCredentials, $"Unable to create AWS credentials for profile {selectedProfileName}.");
            }

            var credentials = await Resolve();

            if (credentials is AssumeRoleAWSCredentials assumeRoleAWSCredentials)
            {
                var assumeOptions = assumeRoleAWSCredentials.Options;
                assumeOptions.MfaTokenCodeCallback = new AssumeRoleMfaTokenCodeCallback(_toolInteractiveService, _directoryManager, assumeOptions).Execute;
            }

            return credentials;
        }

        private async Task<bool> CanLoadCredentials(AWSCredentials credentials)
        {
            try
            {
                await credentials.GetCredentialsAsync();
                return true;
            }
            catch (Exception ex)
            {
                _toolInteractiveService.WriteDebugLine(ex.PrettyPrint());
                return false;
            }
        }

        public string ResolveAWSRegion(string? region, string? lastRegionUsed = null)
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
