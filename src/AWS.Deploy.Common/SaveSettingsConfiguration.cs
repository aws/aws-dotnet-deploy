// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// This enum controls which settings are persisted when deploymentSettingsHandler.SaveSettings(..) is invoked
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
        /// This enum controls which settings are persisted when deploymentSettingsHandler.SaveSettings(..) is invoked
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
