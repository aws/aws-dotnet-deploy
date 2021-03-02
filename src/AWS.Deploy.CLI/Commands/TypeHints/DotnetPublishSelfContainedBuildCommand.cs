// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishSelfContainedBuildCommand : ITypeHintCommand
    {
        private readonly ConsoleUtilities _consoleUtilities;

        public DotnetPublishSelfContainedBuildCommand(ConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, recommendation.GetOptionSettingValue<string>(optionSetting));
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = answer == ConsoleUtilities.YesNo.Yes;
            return answer == ConsoleUtilities.YesNo.Yes ? "true" : "false";
        }
    }
}
