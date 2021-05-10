// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DotnetBeanstalkPlatformArnCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public DotnetBeanstalkPlatformArnCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = recommendation.GetOptionSettingValue(optionSetting);
            var platformArns = await _awsResourceQueryer.GetElasticBeanstalkPlatformArns();

            var userInputConfiguration = new UserInputConfiguration<PlatformSummary>(
                platform => $"{platform.PlatformBranchName} v{platform.PlatformVersion}",
                platform => platform.PlatformArn.Equals(currentValue))
            {
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(platformArns, "Select the Platform to use:", userInputConfiguration);

            return userResponse.SelectedOption?.PlatformArn!;
        }
    }
}
