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
            var appSettingsContainer = new RecipeConfiguration<Dictionary<string, object>>()
            {
                StackName = cloudApplication.StackName,
                ProjectPath = new FileInfo(recommendation.ProjectPath).Directory.FullName,
                ECRRepositoryName = recommendation.DeploymentBundle.ECRRepositoryName ?? "",
                ECRImageTag = recommendation.DeploymentBundle.ECRImageTag ?? "",
                DotnetPublishZipPath = recommendation.DeploymentBundle.DotnetPublishZipPath ?? "",
                DotnetPublishOutputDirectory = recommendation.DeploymentBundle.DotnetPublishOutputDirectory ?? "",
                Settings = new Dictionary<string, object>()
            };

            appSettingsContainer.RecipeId = recommendation.Recipe.Id;
            appSettingsContainer.RecipeVersion = recommendation.Recipe.Version;

            // Option Settings
            foreach (var optionSetting in recommendation.Recipe.OptionSettings)
            {
                appSettingsContainer.Settings[optionSetting.Id] = recommendation.GetOptionSettingValue(optionSetting);
            }

            return JsonConvert.SerializeObject(appSettingsContainer, Formatting.Indented);
        }
    }
}
