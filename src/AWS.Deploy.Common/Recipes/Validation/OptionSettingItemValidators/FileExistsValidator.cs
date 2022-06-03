// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that a recipe or deployment bundle option with a FilePath typehint points to an actual file.
    /// This can either be an absolute path to the file or relative to the project path
    /// </summary>
    public class FileExistsValidator : IOptionSettingItemValidator
    {
        private readonly IFileManager _fileManager;

        public FileExistsValidator(IFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public string ValidationFailedMessage { get; set; } = "The specified file does not exist";

        /// <summary>
        /// Whether or not an empty filepath is valid (essentially whether this option is required)
        /// </summary>
        public bool AllowEmptyString { get; set; } = true;

        public Task<ValidationResult> Validate(object input, Recommendation recommendation, OptionSettingItem optionSettingItem)
        {
            var inputFilePath = input?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(inputFilePath))
            {
                if (AllowEmptyString)
                {
                    return ValidationResult.ValidAsync();
                }
                else
                {
                    return ValidationResult.FailedAsync("A file must be specified");
                }
            }

            // Otherwise if there is a value, verify that it points to an actual file
            if (_fileManager.Exists(inputFilePath, recommendation.GetProjectDirectory()))
            {
                return ValidationResult.ValidAsync();
            }
            else
            {
                return ValidationResult.FailedAsync($"The specified file {inputFilePath} does not exist");
            }
        }
    }
}
