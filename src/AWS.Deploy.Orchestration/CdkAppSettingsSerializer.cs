// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes.CDK.Common;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkAppSettingsSerializer
    {
        /// <summary>
        /// Creates the contents for the appsettings.json file inside the CDK project. This file is deserialized into <see cref="IRecipeProps{T}"/> to be used the by the CDK templates.
        /// </summary>
        string Build(CloudApplication cloudApplication, Recommendation recommendation, OrchestratorSession session);
    }

    public class CdkAppSettingsSerializer : ICdkAppSettingsSerializer
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IDirectoryManager _directoryManager;

        public CdkAppSettingsSerializer(IOptionSettingHandler optionSettingHandler, IDirectoryManager directoryManager)
        {
            _optionSettingHandler = optionSettingHandler;
            _directoryManager = directoryManager;
        }

        public string Build(CloudApplication cloudApplication, Recommendation recommendation, OrchestratorSession session)
        {
            var projectPath = new FileInfo(recommendation.ProjectPath).Directory?.FullName;
            if (string.IsNullOrEmpty(projectPath))
                throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path provided is invalid.");

            // General Settings
            var appSettingsContainer = new RecipeProps<Dictionary<string, object>>(
                cloudApplication.Name,
                projectPath,
                recommendation.Recipe.Id,
                recommendation.Recipe.Version,
                session.AWSAccountId,
                session.AWSRegion,
                settings: _optionSettingHandler.GetOptionSettingsMap(recommendation, session.ProjectDefinition, _directoryManager, OptionSettingsType.Recipe)
                )
            {
                // These deployment bundle settings need to be set separately because they are not configurable by the user.
                // These settings will not be part of the CloudFormation template metadata.
                // The only exception to this is the ECR Repository name.
                ECRRepositoryName = recommendation.DeploymentBundle.ECRRepositoryName ?? "",
                ECRImageTag = recommendation.DeploymentBundle.ECRImageTag ?? "",
                DotnetPublishZipPath = recommendation.DeploymentBundle.DotnetPublishZipPath ?? "",
                DotnetPublishOutputDirectory = recommendation.DeploymentBundle.DotnetPublishOutputDirectory ?? "",
                EnvironmentArchitecture = recommendation.DeploymentBundle.EnvironmentArchitecture.ToString()
            };

            // Persist deployment bundle settings
            var deploymentBundleSettingsMap = _optionSettingHandler.GetOptionSettingsMap(recommendation, session.ProjectDefinition, _directoryManager, OptionSettingsType.DeploymentBundle);
            appSettingsContainer.DeploymentBundleSettings = JsonConvert.SerializeObject(deploymentBundleSettingsMap);

            return JsonConvert.SerializeObject(appSettingsContainer, Formatting.Indented);
        }
    }
}
