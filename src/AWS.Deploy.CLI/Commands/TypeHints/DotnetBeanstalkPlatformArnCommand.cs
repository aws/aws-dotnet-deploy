// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;

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

        private async Task<List<PlatformSummary>> GetData()
        {
            return await _awsResourceQueryer.GetElasticBeanstalkPlatformArns();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var platformArns = await GetData();
            return platformArns.Select(x => new TypeHintResource(x.PlatformArn, $"{x.PlatformBranchName} v{x.PlatformVersion}")).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = recommendation.GetOptionSettingValue(optionSetting);
            var platformArns = await GetData();

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
