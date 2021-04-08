// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DockerExecutionDirectoryCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DockerExecutionDirectoryCommand(IConsoleUtilities consoleUtilities)
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
                    resetValue: recommendation.GetOptionSettingDefaultValue<string>(optionSetting),
                    // validators:
                    executionDirectory => ValidateExecutionDirectory(executionDirectory));

            recommendation.DeploymentBundle.DockerExecutionDirectory = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        private string ValidateExecutionDirectory(string executionDirectory)
        {
            if (!string.IsNullOrEmpty(executionDirectory) && !Directory.Exists(executionDirectory))
                return "The directory you specified does not exist.";
            else
                return "";
        }
    }
}
