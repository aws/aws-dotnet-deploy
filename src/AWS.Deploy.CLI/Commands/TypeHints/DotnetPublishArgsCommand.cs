// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetPublishArgsCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DotnetPublishArgsCommand(IConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    recommendation.GetOptionSettingValue<string>(optionSetting),
                    allowEmpty: true,
                    resetValue: recommendation.GetOptionSettingDefaultValue<string>(optionSetting) ?? "",
                    // validators:
                    publishArgs =>
                        (publishArgs.Contains("-o ") || publishArgs.Contains("--output "))
                            ? "You must not include -o/--output as an additional argument as it is used internally."
                            : "",
                    publishArgs =>
                        (publishArgs.Contains("-c ") || publishArgs.Contains("--configuration ")
                            ? "You must not include -c/--configuration as an additional argument. You can set the build configuration in the advanced settings."
                            : ""),
                    publishArgs =>
                        (publishArgs.Contains("--self-contained") || publishArgs.Contains("--no-self-contained")
                            ? "You must not include --self-contained/--no-self-contained as an additional argument. You can set the self-contained property in the advanced settings."
                            : ""))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DotnetPublishAdditionalBuildArguments = settingValue;

            return Task.FromResult<object>(settingValue);
        }
    }
}
