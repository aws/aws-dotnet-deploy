// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.TypeHintData;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DockerHttpPortCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DockerHttpPortCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult(new TypeHintResourceTable());

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting) ?? string.Empty,
                    allowEmpty: false,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? string.Empty,
                    validators: async httpPort => await ValidateHttpPort(httpPort, recommendation, optionSetting));

            var settingValueInt = int.Parse(settingValue);
            recommendation.DeploymentBundle.DockerfileHttpPort = settingValueInt;
            return Task.FromResult<object>(settingValueInt);
        }

        private async Task<string> ValidateHttpPort(string httpPort, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var validationResult = await new RangeValidator() { Min = 0, Max = 65535 }.Validate(httpPort, recommendation, optionSettingItem);

            if (validationResult.IsValid)
            {
                return string.Empty;
            }
            else
            {
                return validationResult.ValidationFailedMessage ?? "Invalid value for Docker HTTP Port.";
            }
        }
    }
}
