// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validator that validates if a given directory exists
    /// </summary>
    public class DirectoryExistsValidator : IOptionSettingItemValidator
    {
        private readonly IDirectoryManager _directoryManager;

        public DirectoryExistsValidator(IDirectoryManager directoryManager)
        {
            _directoryManager = directoryManager;
        }

        /// <summary>
        /// Validates that the given directory exists.
        /// This can be either an absolute path, or a path relative to the project directory.
        /// </summary>
        /// <param name="input">Path to validate</param>
        /// <returns>Valid if the directory exists, invalid otherwise</returns>
        public Task<ValidationResult> Validate(object input)
        {
            var executionDirectory = (string)input;

            if (!string.IsNullOrEmpty(executionDirectory) && !_directoryManager.Exists(executionDirectory))
                return ValidationResult.FailedAsync("The specified directory does not exist.");
            else
                return ValidationResult.ValidAsync();
        }
    }
}
