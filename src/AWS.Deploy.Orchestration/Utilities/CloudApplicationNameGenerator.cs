// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface ICloudApplicationNameGenerator
    {
        /// <summary>
        /// Generates a valid candidate for <see cref="CloudApplication.Name"/> based on <paramref name="target"/>.
        /// Name  is checked to ensure it is unique, ie <paramref name="existingApplications"/> doesn't already have it.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if can't generate a valid name from <paramref name="target"/>.
        /// </exception>
        string GenerateValidName(ProjectDefinition target, List<CloudApplication> existingApplications);

        /// <summary>
        /// https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-using-console-create-stack-parameters.html
        /// </summary>
        bool IsValidName(string name);
    }

    public class CloudApplicationNameGenerator : ICloudApplicationNameGenerator
    {
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        /// <summary>
        /// https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-using-console-create-stack-parameters.html
        /// </summary>
        private readonly Regex _validatorRegex = new ("^[a-zA-Z][a-zA-Z0-9-]{0,127}$", RegexOptions.Compiled);

        public CloudApplicationNameGenerator(IFileManager fileManager, IDirectoryManager directoryManager)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
        }

        public string GenerateValidName(ProjectDefinition target, List<CloudApplication> existingApplications)
        {
            // generate recommendation
            var recommendedPrefix = "deployment";

            if (_fileManager.Exists(target.ProjectPath))
                recommendedPrefix = Path.GetFileNameWithoutExtension(target.ProjectPath) ?? "";
            else if (_directoryManager.Exists(target.ProjectPath))
                recommendedPrefix = Path.GetDirectoryName(target.ProjectPath) ?? "";

            // sanitize recommendation
            recommendedPrefix =
                new string(
                    recommendedPrefix
                        .ToCharArray()
                        .SkipWhile(c => !char.IsLetter(c) && ((int)c) < 127)
                        .Where(c =>
                            char.IsNumber(c) ||
                            char.IsLetter(c) && ((int)c) < 127 ||
                            c == '-')
                        .ToArray());

            // make sure the recommendation doesn't exist already in existingApplications
            var recommendation = recommendedPrefix;
            var suffix = 1;
            while (suffix < 100)
            {
                if (existingApplications.All(x => x.Name != recommendation) && IsValidName(recommendation))
                    return recommendation;

                recommendation = $"{recommendedPrefix}{suffix++}";
            }

            throw new ArgumentException("Failed to generate a valid and unique name.");
        }

        /// <remarks>
        /// https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-using-console-create-stack-parameters.html
        /// </remarks>>
        public bool IsValidName(string name) => _validatorRegex.IsMatch(name);
    }
}
