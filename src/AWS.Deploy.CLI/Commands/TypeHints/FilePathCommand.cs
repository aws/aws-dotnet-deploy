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
    }
}
