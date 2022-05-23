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
                    validators: async publishArgs => await ValidateDotnetPublishArgs(publishArgs))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = settingValue;

            return Task.FromResult<object>(settingValue);
        }

        /// <summary>
        /// This method will be invoked to set any additional Dotnet build arguments in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="publishArgs">The user specified Dotnet build arguments.</param>
        public async Task OverrideValue(Recommendation recommendation, string publishArgs)
        {
            var resultString = await ValidateDotnetPublishArgs(publishArgs);
            if (!string.IsNullOrEmpty(resultString))
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidDotnetPublishArgs, resultString);
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = publishArgs.Replace("\"", "\"\"");
        }

        private async Task<string> ValidateDotnetPublishArgs(string publishArgs)
        {
            var validationResult = await new DotnetPublishArgsValidator().Validate(publishArgs);

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
