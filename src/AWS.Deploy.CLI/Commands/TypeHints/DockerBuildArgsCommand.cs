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
        private readonly ConsoleUtilities _consoleUtilities;

        public DockerBuildArgsCommand(ConsoleUtilities consoleUtilities)
        {
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    recommendation.GetOptionSettingValue<string>(optionSetting),
                    allowEmpty: true,
                    // validators:
                    buildArgs => ValidateBuildArgs(buildArgs))
                .ToString()
                .Replace("\"", "\"\"");

            recommendation.DeploymentBundle.DockerBuildArgs = settingValue;
            return settingValue;
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
