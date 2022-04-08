// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public string? ApplicationName { get; set; }

        public string? RecipeId { get; set; }

        public JObject? OptionSettingsConfig { get; set; }

        public Dictionary<string, string> LeafOptionSettingItems = new Dictionary<string, string>();

        /// <summary>
        /// Reads the User Deployment Settings file and deserializes it into a <see cref="UserDeploymentSettings"/> object.
        /// </summary>
        /// <exception cref="InvalidUserDeploymentSettingsException">Thrown if an error occurred while reading or deserializing the User Deployment Settings file.</exception>
        public static UserDeploymentSettings? ReadSettings(string filePath)
        {
            try
            {
                var userDeploymentSettings = JsonConvert.DeserializeObject<UserDeploymentSettings>(File.ReadAllText(filePath));
                if (userDeploymentSettings.OptionSettingsConfig != null)
                    userDeploymentSettings.TraverseRootToLeaf(userDeploymentSettings.OptionSettingsConfig.Root);
                return userDeploymentSettings;
            }
            catch (Exception ex)
            {
                throw new InvalidUserDeploymentSettingsException(DeployToolErrorCode.FailedToDeserializeUserDeploymentFile, "An error occured while trying to deserialize the User Deployment Settings file.", ex);
            }
        }

        /// <summary>
        /// This method is responsible for traversing all paths from root node to leaf nodes in a Json blob.
        /// These paths and the corresponding leaf node values are stored in a dictionary <see cref="LeafOptionSettingItems"/>
        /// </summary>
        /// <param name="node">The current node that is being processed.</param>
        private void TraverseRootToLeaf(JToken node)
        {
            if (!string.IsNullOrEmpty(node.Path) && node.Type.ToString().Equals("Array"))
            {
                var list = node.Values<string>().Select(x => x.ToString()).ToList();
                LeafOptionSettingItems.Add(node.Path, JsonConvert.SerializeObject(list));
                return;
            }

            if (!node.HasValues)
            {
                // The only way to reach a leaf node of type object is if the object is empty.
                if (node.Type.ToString() == "Object")
                    return;

                var path = node.Path;
                if (path.Contains("['"))
                    path = path.Substring(2, node.Path.Length - 4);
                LeafOptionSettingItems.Add(path, node.Value<string>());
                return;
            }

            foreach (var childNode in node.Children())
            {
                TraverseRootToLeaf(childNode);
            }
        }
    }
}
