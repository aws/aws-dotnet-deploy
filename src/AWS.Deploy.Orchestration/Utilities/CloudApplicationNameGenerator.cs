// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;

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
        string GenerateValidName(ProjectDefinition target, List<CloudApplication> existingApplications, DeploymentTypes? deploymentType = null);

        /// <summary>
        /// Validates the cloud application name
        /// </summary>
        /// <param name="name">User provided cloud application name</param>
        /// <param name="deploymentType">The deployment type of the selected recommendation</param>
        /// <param name="existingApplications">List of existing deployed applications</param>
        /// <returns><see cref="CloudApplicationNameValidationResult"/></returns>
        CloudApplicationNameValidationResult IsValidName(string name, IList<CloudApplication> existingApplications, DeploymentTypes? deploymentType = null);
    }

    /// <summary>
    /// Stores the result from validating the cloud application name.
    /// </summary>
    public class CloudApplicationNameValidationResult
    {
        public readonly bool IsValid;
        public readonly string ErrorMessage;

        public CloudApplicationNameValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
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

        public string GenerateValidName(ProjectDefinition target, List<CloudApplication> existingApplications, DeploymentTypes? deploymentType = null)
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
            var suffixString = "";
            var recommendationCharArray = recommendation.ToCharArray();
            for (var i = recommendationCharArray.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(recommendationCharArray[i]))
                    suffixString = $"{recommendationCharArray[i]}{suffixString}";
                else
                    break;
            }

            var prefix = !string.IsNullOrEmpty(suffixString) ? recommendation[..^suffixString.Length] : recommendedPrefix;
            var suffix = !string.IsNullOrEmpty(suffixString) ? int.Parse(suffixString): 0;
            while (suffix < int.MaxValue)
            {
                var validationResult = IsValidName(recommendation, existingApplications, deploymentType);

                if (validationResult.IsValid)
                    return recommendation;

                recommendation = $"{prefix}{++suffix}";
            }

            throw new ArgumentException("Failed to generate a valid and unique name.");
        }


        public CloudApplicationNameValidationResult IsValidName(string name, IList<CloudApplication> existingApplications, DeploymentTypes? deploymentType = null)
        {
            var errorMessage = string.Empty;

            if (!SatisfiesRegex(name))
            {
                errorMessage += $"The application name can contain only alphanumeric characters (case-sensitive) and hyphens. " +
                $"It must start with an alphabetic character and can't be longer than 128 characters.{Environment.NewLine}";
            }
            if (MatchesExistingDeployment(name, existingApplications, deploymentType))
            {
                errorMessage += "A cloud application already exists with this name.";
            }

            if (string.IsNullOrEmpty(errorMessage))
                return new CloudApplicationNameValidationResult(true, string.Empty);

            return new CloudApplicationNameValidationResult(false, $"Invalid cloud application name: {name}{Environment.NewLine}{errorMessage}");
        }

        /// <summary>
        /// This method first filters the existing applications by the current deploymentType if the deploymentType is not null
        /// It will then check if the current name matches the filtered list of existing applications
        /// </summary>
        /// <param name="name">User provided cloud application name</param>
        /// <param name="deploymentType">The deployment type of the selected recommendation</param>
        /// <param name="existingApplications">List of existing deployed applications</param>
        /// <returns>true if found a match. false otherwise</returns>
        private bool MatchesExistingDeployment(string name, IList<CloudApplication> existingApplications, DeploymentTypes? deploymentType = null)
        {
            if (!existingApplications.Any())
                return false;

            if (deploymentType != null)
                existingApplications = existingApplications.Where(x => x.DeploymentType == deploymentType).ToList();

            return existingApplications.Any(x => x.Name == name);
        }

        /// <summary>
        /// https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/cfn-using-console-create-stack-parameters.html
        /// </summary>
        private bool SatisfiesRegex(string name)
        {
            return _validatorRegex.IsMatch(name);
        }
    }
}
