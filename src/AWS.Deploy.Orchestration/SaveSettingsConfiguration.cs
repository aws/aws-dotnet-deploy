// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration
{
    /// <summary>
    /// This enum controls which settings are persisted when <see cref="DeploymentSettingsHandler.SaveSettings(SaveSettingsConfiguration, Recommendation, CloudApplication, OrchestratorSession)"/> is invoked
    /// </summary>
    public enum SaveSettingsType
    {
        None,
        Modified,
        All
    }

    public class SaveSettingsConfiguration
    {
        /// <summary>
        /// Specifies which settings are persisted when <see cref="DeploymentSettingsHandler.SaveSettings(SaveSettingsConfiguration, Recommendation, CloudApplication, OrchestratorSession)"/> is invoked
        /// </summary>
        public readonly SaveSettingsType SettingsType;

        /// <summary>
        /// The absolute file path where deployment settings will be persisted
        /// </summary>
        public readonly string FilePath;

        public SaveSettingsConfiguration(SaveSettingsType settingsType, string filePath)
        {
            SettingsType = settingsType;
            FilePath = filePath;
        }
    }
}
