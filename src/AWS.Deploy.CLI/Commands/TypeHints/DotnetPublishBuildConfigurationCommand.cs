// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishBuildConfigurationCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DotnetPublishBuildConfigurationCommand(IConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue =
                _consoleUtilities.AskUserForValue(
                    string.Empty,
                    recommendation.GetOptionSettingValue<string>(optionSetting),
                    allowEmpty: false,
                    resetValue: recommendation.GetOptionSettingDefaultValue<string>(optionSetting) ?? "");
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        /// <summary>
        /// This method will be invoked to set the Dotnet build configuration in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="configuration">The user specified Dotnet build configuration.</param>
        public void Overridevalue(Recommendation recommendation, string configuration)
        {
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = configuration;
        }
    }
}
