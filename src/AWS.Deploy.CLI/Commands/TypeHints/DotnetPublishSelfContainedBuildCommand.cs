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

        public Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult(new TypeHintResourceTable());

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var answer = _consoleUtilities.AskYesNoQuestion(string.Empty, _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting));
            recommendation.DeploymentBundle.DotnetPublishSelfContainedBuild = answer == YesNo.Yes;
            var result = answer == YesNo.Yes ? "true" : "false";
            return Task.FromResult<object>(result);
        }
    }
}
