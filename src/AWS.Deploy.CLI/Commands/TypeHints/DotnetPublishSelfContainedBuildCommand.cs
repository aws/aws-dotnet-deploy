// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishSelfContainedBuildCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DotnetPublishSelfContainedBuildCommand(IConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, recommendation.GetOptionSettingValue<string>(optionSetting));
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = answer == YesNo.Yes;
            var result = answer == YesNo.Yes ? "true" : "false";
            return Task.FromResult<object>(result);
        }
    }
}
