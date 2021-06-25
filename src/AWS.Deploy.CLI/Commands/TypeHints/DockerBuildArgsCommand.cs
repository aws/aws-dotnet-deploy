// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DockerBuildArgsCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;

        public DockerBuildArgsCommand(IConsoleUtilities consoleUtilities)
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
                    buildArgs => ValidateBuildArgs(buildArgs))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DockerBuildArgs = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        /// <summary>
        /// This method will be invoked to set the Docker build arguments in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="dockerBuildArgs">Arguments to be passed when performing a Docker build</param>
        public void OverrideValue(Recommendation recommendation, string dockerBuildArgs)
        {
            var resultString = ValidateBuildArgs(dockerBuildArgs);
            if (!string.IsNullOrEmpty(resultString))
                throw new InvalidOverrideValueException(resultString);
            recommendation.DeploymentBundle.DockerBuildArgs = dockerBuildArgs;
        }

        private string ValidateBuildArgs(string buildArgs)
        {
            var argsList = buildArgs.Split(",");
            if (argsList.Length == 0)
                return "";

            foreach (var arg in argsList)
            {
                var keyValue = arg.Split("=");
                if (keyValue.Length == 2)
                    return "";
                else
                    return "The Docker Build Args must have the following pattern 'arg1=val1,arg2=val2'.";
            }

            return "";
        }
    }
}
