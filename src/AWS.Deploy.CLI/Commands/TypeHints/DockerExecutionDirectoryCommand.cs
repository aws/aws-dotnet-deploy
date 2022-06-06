// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DockerExecutionDirectoryCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DockerExecutionDirectoryCommand(IConsoleUtilities consoleUtilities, IDirectoryManager directoryManager, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult(new TypeHintResourceTable());

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting),
                    allowEmpty: true,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "",
                    validators: async executionDirectory => await ValidateExecutionDirectory(executionDirectory, recommendation, optionSetting));

            recommendation.DeploymentBundle.DockerExecutionDirectory = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        /// <summary>
        /// Validates that the Docker execution directory exists as either an
        /// absolute path or a path relative to the project directory.
        /// </summary>
        /// <param name="executionDirectory">Proposed Docker execution directory</param>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="optionSettingItem">The selected option setting item</param>
        /// <returns>Empty string if the directory is valid, an error message if not</returns>
        private async Task<string> ValidateExecutionDirectory(string executionDirectory, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var validationResult = await new DirectoryExistsValidator(_directoryManager).Validate(executionDirectory, recommendation, optionSettingItem);

            if (validationResult.IsValid)
            {
                return string.Empty;
            }
            else
            {   // Override the generic ValidationResultMessage with one about the the Docker execution directory
                return "The directory specified for Docker execution does not exist.";
            }
        }
    }
}
