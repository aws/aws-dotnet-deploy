// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration.LocalUserSettings
{
    public interface ILocalUserSettingsEngine
    {
        Task UpdateLastDeployedStack(string stackName, string projectName, string? awsAccountId, string? awsRegion);
        Task DeleteLastDeployedStack(string stackName, string projectName, string? awsAccountId, string? awsRegion);
        Task CleanOrphanStacks(List<string> deployedStacks, string projectName, string? awsAccountId, string? awsRegion);
        Task<LocalUserSettings?> GetLocalUserSettings();
    }

    public class LocalUserSettingsEngine : ILocalUserSettingsEngine
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;

        private const string LOCAL_USER_SETTINGS_FILE_NAME = "local-user-settings.json";

        public LocalUserSettingsEngine(IFileManager fileManager, IDirectoryManager directoryManager)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
        }

        /// <summary>
        /// This method updates the local user settings json file by adding the name of the stack that was most recently used.
        /// If the file does not exists then a new file is generated.
        /// </summary>
        public async Task UpdateLastDeployedStack(string stackName, string projectName, string? awsAccountId, string? awsRegion)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                    throw new FailedToUpdateLocalUserSettingsFileException("The Project Name is not defined.");
                if (string.IsNullOrEmpty(awsAccountId) || string.IsNullOrEmpty(awsRegion))
                    throw new FailedToUpdateLocalUserSettingsFileException("The AWS Account Id or Region is not defined.");

                var localUserSettings = await GetLocalUserSettings();
                var lastDeployedStack = localUserSettings?.LastDeployedStacks?
                    .FirstOrDefault(x => x.Exists(awsAccountId, awsRegion, projectName));

                if (localUserSettings != null)
                {
                    if (lastDeployedStack != null)
                    {
                        if (lastDeployedStack.Stacks == null)
                        {
                            lastDeployedStack.Stacks = new List<string> { stackName };
                        }
                        else
                        {
                            if (!lastDeployedStack.Stacks.Contains(stackName))
                                lastDeployedStack.Stacks.Add(stackName);
                            lastDeployedStack.Stacks.Sort();
                        }
                    }
                    else
                    {
                        localUserSettings.LastDeployedStacks = new List<LastDeployedStack>() {
                            new LastDeployedStack(
                                awsAccountId,
                                awsRegion,
                                projectName,
                                new List<string>() { stackName })};
                    }
                }
                else
                {

                    var lastDeployedStacks = new List<LastDeployedStack> {
                        new LastDeployedStack(
                            awsAccountId,
                            awsRegion,
                            projectName,
                            new List<string>() { stackName }) };
                    localUserSettings = new LocalUserSettings(lastDeployedStacks);
                }

                await WriteLocalUserSettingsFile(localUserSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"kmalhar found Exception - {ex.PrettyPrint()}");
                throw new FailedToUpdateLocalUserSettingsFileException($"Failed to update the local user settings file " +
                    $"to include the last deployed to stack '{stackName}'.", ex);
            }
        }

        /// <summary>
        /// This method updates the local user settings json file by deleting the stack that was most recently used.
        /// </summary>
        public async Task DeleteLastDeployedStack(string stackName, string projectName, string? awsAccountId, string? awsRegion)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                    throw new FailedToUpdateLocalUserSettingsFileException("The Project Name is not defined.");
                if (string.IsNullOrEmpty(awsAccountId) || string.IsNullOrEmpty(awsRegion))
                    throw new FailedToUpdateLocalUserSettingsFileException("The AWS Account Id or Region is not defined.");

                var localUserSettings = await GetLocalUserSettings();
                var lastDeployedStack = localUserSettings?.LastDeployedStacks?
                    .FirstOrDefault(x => x.Exists(awsAccountId, awsRegion, projectName));

                if (localUserSettings == null || lastDeployedStack == null)
                    return;

                lastDeployedStack.Stacks.Remove(stackName);

                await WriteLocalUserSettingsFile(localUserSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"kmalhar found Exception - {ex.PrettyPrint()}");
                throw new FailedToUpdateLocalUserSettingsFileException($"Failed to update the local user settings file " +
                    $"to delete the stack '{stackName}'.", ex);
            }
        }

        /// <summary>
        /// This method updates the local user settings json file by deleting orphan stacks.
        /// </summary>
        public async Task CleanOrphanStacks(List<string> deployedStacks, string projectName, string? awsAccountId, string? awsRegion)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName))
                    throw new FailedToUpdateLocalUserSettingsFileException("The Project Name is not defined.");
                if (string.IsNullOrEmpty(awsAccountId) || string.IsNullOrEmpty(awsRegion))
                    throw new FailedToUpdateLocalUserSettingsFileException("The AWS Account Id or Region is not defined.");

                var localUserSettings = await GetLocalUserSettings();
                var localStacks = localUserSettings?.LastDeployedStacks?
                    .FirstOrDefault(x => x.Exists(awsAccountId, awsRegion, projectName));

                if (localUserSettings == null || localStacks == null || localStacks.Stacks == null)
                    return;

                var validStacks = deployedStacks.Intersect(localStacks.Stacks);

                localStacks.Stacks = validStacks.ToList();

                await WriteLocalUserSettingsFile(localUserSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"kmalhar found Exception - {ex.PrettyPrint()}");
                throw new FailedToUpdateLocalUserSettingsFileException($"Failed to update the local user settings file " +
                    $"to delete orphan stacks.", ex);
            }
        }

        /// <summary>
        /// This method parses the <see cref="LocalUserSettings"/> into a string and writes it to disk.
        /// </summary>
        private async Task<string> WriteLocalUserSettingsFile(LocalUserSettings deploymentManifestModel)
        {
            var localUserSettingsFilePath = GetLocalUserSettingsFilePath();
            var settingsFilejsonString = JsonConvert.SerializeObject(deploymentManifestModel, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new SerializeModelContractResolver()
            });

            await _fileManager.WriteAllTextAsync(localUserSettingsFilePath, settingsFilejsonString);
            return localUserSettingsFilePath;
        }

        /// <summary>
        /// This method parses the local user settings file into a <see cref="LocalUserSettings"/>
        /// </summary>
        public async Task<LocalUserSettings?> GetLocalUserSettings()
        {
            try
            {
                var localUserSettingsFilePath = GetLocalUserSettingsFilePath();

                if (!_fileManager.Exists(localUserSettingsFilePath))
                    return null;
                var settingsFilejsonString = await _fileManager.ReadAllTextAsync(localUserSettingsFilePath);
                return JsonConvert.DeserializeObject<LocalUserSettings>(settingsFilejsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"kmalhar found Exception - {ex.PrettyPrint()}");
                throw new InvalidLocalUserSettingsFileException("The Local User Settings file is invalid.", ex);
            }
        }

        /// <summary>
        /// This method returns the path at which the local user settings file will be stored.
        /// </summary>
        private string GetLocalUserSettingsFilePath()
        {
            var deployToolWorkspace = _directoryManager.GetDirectoryInfo(Constants.CDK.DeployToolWorkspaceDirectoryRoot).FullName;
            var localUserSettingsFileFullPath = Path.Combine(deployToolWorkspace, LOCAL_USER_SETTINGS_FILE_NAME);
            return localUserSettingsFileFullPath;
        }
    }
}
