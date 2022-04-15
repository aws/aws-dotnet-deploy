// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// Typehint that lets the user specify a path to a file.
    /// This can either be an absolute path to the file or relative to the project path.
    /// </summary>
    public class FilePathCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IFileManager _fileManager;

        public FilePathCommand(IConsoleUtilities consoleUtilities , IOptionSettingHandler optionSettingHandler, IFileManager fileManager)
        {
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
            _fileManager = fileManager;
        }

        /// <summary>
        /// Not implemented, specific files are not suggested to the user
        /// </summary>
        /// <returns>Empty list</returns>
        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        /// <summary>
        /// Prompts the user to enter a path to a file
        /// </summary>
        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var typeHintData = optionSetting.GetTypeHintData<FilePathTypeHintData>();

            var userFilePath = _consoleUtilities
               .AskUserForValue(
                   string.Empty,
                   _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting),
                   allowEmpty: typeHintData?.AllowEmpty ?? true,
                   resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "") ;

            return Task.FromResult<object>(userFilePath);
        }

        /// <summary>
        /// This method will be invoked to set a file path setting in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="filePath">File path entered by the user</param>
        public async Task OverrideValue(Recommendation recommendation, string filePath)
        {
            var validator = new FileExistsValidator(_fileManager);
            var validationResult = await (validator as IOptionSettingItemValidator).Validate(filePath, recommendation);

            if (!validationResult.IsValid)
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidFilePath, validationResult.ValidationFailedMessage ?? validator.ValidationFailedMessage);
        }
    }
}
