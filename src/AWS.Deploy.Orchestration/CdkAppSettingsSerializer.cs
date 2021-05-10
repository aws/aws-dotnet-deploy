// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes.CDK.Common;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class CdkAppSettingsSerializer
    {
        public string Build(CloudApplication cloudApplication, Recommendation recommendation)
        {
            // General Settings
            var appSettingsContainer = new RecipeConfiguration<Dictionary<string, object>>(
                cloudApplication.StackName,
                new FileInfo(recommendation.ProjectPath).Directory.FullName,
                recommendation.Recipe.Id,
                recommendation.Recipe.Version,
                new ()
                )
            {
                ECRRepositoryName = recommendation.DeploymentBundle.ECRRepositoryName ?? "",
                ECRImageTag = recommendation.DeploymentBundle.ECRImageTag ?? "",
                DotnetPublishZipPath = recommendation.DeploymentBundle.DotnetPublishZipPath ?? "",
                DotnetPublishOutputDirectory = recommendation.DeploymentBundle.DotnetPublishOutputDirectory ?? ""
            };

            // Option Settings
            foreach (var optionSetting in recommendation.Recipe.OptionSettings)
            {
                var optionSettingValue = recommendation.GetOptionSettingValue(optionSetting);

                if (optionSettingValue != null)
                    appSettingsContainer.Settings[optionSetting.Id] = optionSettingValue;
            }

            return JsonConvert.SerializeObject(appSettingsContainer, Formatting.Indented);
        }
    }
}
