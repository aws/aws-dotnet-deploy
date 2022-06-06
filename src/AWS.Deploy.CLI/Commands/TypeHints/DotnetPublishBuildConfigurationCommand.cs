// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishBuildConfigurationCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DotnetPublishBuildConfigurationCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult(new TypeHintResourceTable());
        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue =
                _consoleUtilities.AskUserForValue(
                    string.Empty,
                    _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting),
                    allowEmpty: false,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "");
            recommendation.DeploymentBundle.DotnetPublishBuildConfiguration = settingValue;
            return Task.FromResult<object>(settingValue);
        }
    }
}
