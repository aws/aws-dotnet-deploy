// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishSelfContainedBuildCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DotnetPublishSelfContainedBuildCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting));
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = answer == YesNo.Yes;
            var result = answer == YesNo.Yes ? "true" : "false";
            return Task.FromResult<object>(result);
        }

        /// <summary>
        /// This method will be invoked to indiciate if this is a self-contained build in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="publishSelfContainedBuild">The user specified value to indicate if this is a self-contained build.</param>
        public void OverrideValue(Recommendation recommendation, bool publishSelfContainedBuild)
        {
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = publishSelfContainedBuild;
        }
    }
}
