// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.EC2.Model;
using System.IO;

namespace AWS.Deploy.CLI
{
    public class AWSUtilities
    {
        private readonly IToolInteractiveService _toolInteractiveService;

        public AWSUtilities(IToolInteractiveService toolInteractiveService)
        {
            _toolInteractiveService = toolInteractiveService;
        }

        public async Task<AWSCredentials> ResolveAWSCredentials(string profileName, string lastUsedProfileName)
        {
            AWSCredentials credentials;

            var chain = new CredentialProfileStoreChain();

            if (!string.IsNullOrEmpty(profileName))
            {
                if (chain.TryGetAWSCredentials(profileName, out credentials) &&
                    await CanLoadCredentials(credentials))
                {
                    _toolInteractiveService.WriteLine($"Configuring AWS Credentials from Profile {profileName}.");
                    return credentials;
                }
            }

            if (!string.IsNullOrEmpty(lastUsedProfileName) &&
                chain.TryGetAWSCredentials(lastUsedProfileName, out credentials) &&
                await CanLoadCredentials(credentials))
            {
                _toolInteractiveService.WriteLine($"Configuring AWS Credentials with previous configured profile value {lastUsedProfileName}.");
                return credentials;
            }

            try
            {
                credentials = FallbackCredentialsFactory.GetCredentials();

                if (await CanLoadCredentials(credentials))
                {
                    _toolInteractiveService.WriteLine("Configuring AWS Credentials using AWS SDK credential search.");
                    return credentials;
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
                _toolInteractiveService.WriteErrorLine("Unable to resolve AWS credentials to access AWS.");
                throw new NoAWSCredentialsFoundException();
            }

            var consoleUtilities = new ConsoleUtilities(_toolInteractiveService);
            var selectedProfileName = consoleUtilities.AskUserToChoose(sharedCredentials.ListProfileNames(), "Select AWS Credentials Profile", null);

            if (!chain.TryGetAWSCredentials(selectedProfileName, out credentials) ||
                !(await CanLoadCredentials(credentials)))
            {
                _toolInteractiveService.WriteErrorLine($"Unable to create AWS credentials for profile {selectedProfileName}.");
                throw new NoAWSCredentialsFoundException();
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

        public string ResolveAWSRegion(string region, string lastRegionUsed)
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

            var consoleUtilities = new ConsoleUtilities(_toolInteractiveService);
            var selectedRegion = consoleUtilities.AskUserToChoose(availableRegions, "Select AWS Region", null);

            // Strip display name
            selectedRegion = selectedRegion.Substring(0, selectedRegion.IndexOf('(') - 1).Trim();

            return selectedRegion;
        }
    }
}
