// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI
{
    public interface IAWSUtilities
    {
        Task<Tuple<AWSCredentials, string?>> ResolveAWSCredentials(string? profileName);
        string ResolveAWSRegion(string? region, string? lastRegionUsed = null);
    }

    public class AWSUtilities : IAWSUtilities
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICredentialProfileStoreChainFactory _credentialChainFactory;
        private readonly ISharedCredentialsFileFactory _sharedCredentialsFileFactory;
        private readonly IAWSCredentialsFactory _awsCredentialsFactory;

        public AWSUtilities(
            IServiceProvider serviceProvider,
            IToolInteractiveService toolInteractiveService,
            IConsoleUtilities consoleUtilities,
            IDirectoryManager directoryManager,
            IOptionSettingHandler optionSettingHandler,
            ICredentialProfileStoreChainFactory credentialChainFactory,
            ISharedCredentialsFileFactory sharedCredentialsFileFactory,
            IAWSCredentialsFactory awsCredentialsFactory)
        {
            _serviceProvider = serviceProvider;
            _toolInteractiveService = toolInteractiveService;
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
            _optionSettingHandler = optionSettingHandler;
            _credentialChainFactory = credentialChainFactory;
            _sharedCredentialsFileFactory = sharedCredentialsFileFactory;
            _awsCredentialsFactory = awsCredentialsFactory;
        }


        /// <summary>
        /// At a high level there are 2 possible return values for this function:
        /// 1. <Credentials, regionName> In this case, both the credentials and region were able to be read from the profile.
        /// 2. <Credentials, null>: In this case, the region was not able to be read from the profile, so we return null for it. The null case will be handled later on by <see cref="ResolveAWSRegion">.
        /// </summary>
        public async Task<Tuple<AWSCredentials, string?>> ResolveAWSCredentials(string? profileName)
        {
            async Task<Tuple<AWSCredentials, string?>> Resolve()
            {
                var chain = _credentialChainFactory.Create();

                // Use provided profile to read credentials
                if (!string.IsNullOrEmpty(profileName))
                {
                    if (chain.TryGetAWSCredentials(profileName, out var profileCredentials) &&
                    // Skip checking CanLoadCredentials for AssumeRoleAWSCredentials because it might require an MFA token and the callback hasn't been setup yet.
                    (profileCredentials is AssumeRoleAWSCredentials || await CanLoadCredentials(profileCredentials)))
                    {
                        _toolInteractiveService.WriteLine($"Configuring AWS Credentials from Profile {profileName}.");
                        chain.TryGetProfile(profileName, out var profile);
                        // Return the credentials since they must be found at this point.
                        // For region, we try to read it from the profile. If it's not found in the profile, then return null and region selection will be handled later on by ResolveAWSRegion.
                        return Tuple.Create<AWSCredentials, string?>(profileCredentials, profile.Region?.SystemName);
                    }
                    else
                    {
                        var message = $"Failed to get credentials for profile \"{profileName}\". Please provide a valid profile name and try again.";
                        throw new FailedToGetCredentialsForProfile(DeployToolErrorCode.FailedToGetCredentialsForProfile, message);
                    }
                }

                // Use default credentials
                try
                {
                    var fallbackCredentials = _awsCredentialsFactory.Create();

                    if (await CanLoadCredentials(fallbackCredentials))
                    {
                        // Always return the credentials since they must be found at this point.
                        // For region, we return null here, since it will read from default region in ResolveAWSRegion
                        _toolInteractiveService.WriteLine("Configuring AWS Credentials using AWS SDK credential search.");
                        return Tuple.Create<AWSCredentials, string?>(fallbackCredentials, null);
                    }
                }
                catch (AmazonServiceException ex)
                {
                    // FallbackCredentialsFactory throws an exception if no credentials are found. Burying exception because if no credentials are found
                    // we want to continue and ask the user to select a profile.
                    _toolInteractiveService.WriteDebugLine(ex.PrettyPrint());
                }

                // Use Shared Credentials
                var sharedCredentials = _sharedCredentialsFileFactory.Create();
                if (sharedCredentials.ListProfileNames().Count == 0)
                {
                    throw new NoAWSCredentialsFoundException(DeployToolErrorCode.UnableToResolveAWSCredentials, "Unable to resolve AWS credentials to access AWS.");
                }

                var selectedProfileName = _consoleUtilities.AskUserToChoose(sharedCredentials.ListProfileNames(), "Select AWS Credentials Profile", null);

                if (chain.TryGetAWSCredentials(selectedProfileName, out var selectedProfileCredentials) &&
                    (await CanLoadCredentials(selectedProfileCredentials)))
                {
                    // Return the credentials since they must be found at this point.
                    // For region, we try to read it from the profile. If it's not found in the profile, then return null and region selection will be handled later on by ResolveAWSRegion.
                    chain.TryGetProfile(selectedProfileName, out var profile);
                    return Tuple.Create<AWSCredentials, string?>(selectedProfileCredentials, profile.Region?.SystemName);
                }

                throw new NoAWSCredentialsFoundException(DeployToolErrorCode.UnableToCreateAWSCredentials, $"Unable to create AWS credentials for profile {selectedProfileName}.");
            }

            var credentialsAndRegion = await Resolve();

            if (credentialsAndRegion.Item1 is AssumeRoleAWSCredentials assumeRoleAWSCredentials)
            {
                var assumeOptions = assumeRoleAWSCredentials.Options;
                assumeOptions.MfaTokenCodeCallback = ActivatorUtilities.CreateInstance<AssumeRoleMfaTokenCodeCallback>(_serviceProvider, assumeOptions).Execute;
            }

            return credentialsAndRegion;
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
