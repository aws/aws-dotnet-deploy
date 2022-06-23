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
    public class DockerBuildArgsCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DockerBuildArgsCommand(IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
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
                    allowEmpty: true,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "",
                    validators: async buildArgs => await ValidateBuildArgs(buildArgs, recommendation, optionSetting))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DockerBuildArgs = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        private async Task<string> ValidateBuildArgs(string buildArgs, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var validationResult = await new DockerBuildArgsValidator().Validate(buildArgs, recommendation, optionSettingItem);

            if (validationResult.IsValid)
            {
                return string.Empty;
            }
            else
            {
                return validationResult.ValidationFailedMessage ?? "Invalid value for additional Docker build options.";
            }
        }
    }
}
