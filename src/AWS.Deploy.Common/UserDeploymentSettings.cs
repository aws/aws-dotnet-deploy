// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// A container for User Deployment Settings file that is supplied with the deployment
    /// to apply defaults and bypass some user prompts.
    /// </summary>
    public class UserDeploymentSettings
    {
        public string? AWSProfile { get; set; }

        public string? AWSRegion { get; set; }

        public string? StackName { get; set; }

        public string? SelectedRecipeId { get; set; }

        public Dictionary<string, object> OptionSettingConfigs { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Reads the User Deployment Settings file and deserializes it into a <see cref="UserDeploymentSettings"/> object.
        /// </summary>
        /// <exception cref="InvalidUserDeploymentSettingsException">Thrown if an error occured while reading or deserializing the User Deployment Settings file.</exception>
        public static UserDeploymentSettings? ReadSettings(string filePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<UserDeploymentSettings>(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                throw new InvalidUserDeploymentSettingsException("An error occured while trying to deserialize the User Deployment Settings file.", ex);
            }
        }
    }
}
