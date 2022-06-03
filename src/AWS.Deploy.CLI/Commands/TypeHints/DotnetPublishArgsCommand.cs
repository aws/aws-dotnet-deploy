// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishArgsCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DotnetPublishArgsCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting),
                    allowEmpty: true,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "",
                    validators: async publishArgs => await ValidateDotnetPublishArgs(publishArgs, recommendation, optionSetting))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = settingValue;

            return Task.FromResult<object>(settingValue);
        }

        private async Task<string> ValidateDotnetPublishArgs(string publishArgs, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var validationResult = await new DotnetPublishArgsValidator().Validate(publishArgs, recommendation, optionSettingItem);

            if (validationResult.IsValid)
            {
                return string.Empty;
            }
            else
            {
                return validationResult.ValidationFailedMessage ?? "Invalid value for Dotnet Publish Arguments.";
            }
        }
    }
}
