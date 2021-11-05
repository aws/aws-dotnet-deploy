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
        public string Build(CloudApplication cloudApplication, Recommendation recommendation, OrchestratorSession session)
        {
            var projectPath = new FileInfo(recommendation.ProjectPath).Directory?.FullName;
            if (string.IsNullOrEmpty(projectPath))
                throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path provided is invalid.");

            // General Settings
            var appSettingsContainer = new RecipeProps<Dictionary<string, object>>(
                cloudApplication.StackName,
                projectPath,
                recommendation.Recipe.Id,
                recommendation.Recipe.Version,
                session.AWSAccountId,
                session.AWSRegion,
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
