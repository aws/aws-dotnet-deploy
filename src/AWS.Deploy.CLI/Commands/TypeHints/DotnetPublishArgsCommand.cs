// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishArgsCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DotnetPublishArgsCommand(IConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    recommendation.GetOptionSettingValue<string>(optionSetting),
                    allowEmpty: true,
                    resetValue: recommendation.GetOptionSettingDefaultValue<string>(optionSetting) ?? "",
                    validators: publishArgs => ValidateDotnetPublishArgs(publishArgs))
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
        public void OverrideValue(Recommendation recommendation, string publishArgs)
        {
            var resultString = ValidateDotnetPublishArgs(publishArgs);
            if (!string.IsNullOrEmpty(resultString))
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidDotnetPublishArgs, resultString);
            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = publishArgs.Replace("\"", "\"\"");
        }

        private string ValidateDotnetPublishArgs(string publishArgs)
        {
            var resultString = string.Empty;

            if (publishArgs.Contains("-o ") || publishArgs.Contains("--output "))
                resultString += "You must not include -o/--output as an additional argument as it is used internally." + Environment.NewLine;
            if (publishArgs.Contains("-c ") || publishArgs.Contains("--configuration "))
                resultString += "You must not include -c/--configuration as an additional argument. You can set the build configuration in the advanced settings." + Environment.NewLine;
            if (publishArgs.Contains("--self-contained") || publishArgs.Contains("--no-self-contained"))
                resultString += "You must not include --self-contained/--no-self-contained as an additional argument. You can set the self-contained property in the advanced settings." + Environment.NewLine;

            if (!string.IsNullOrEmpty(resultString))
                return "Invalid valid value for Dotnet Publish Arguments." + Environment.NewLine + resultString.Trim();
            return "";
        }
    }
}
