// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
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
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DotnetBeanstalkPlatformArnCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<PlatformSummary>> GetData()
        {
            return await _awsResourceQueryer.GetElasticBeanstalkPlatformArns(BeanstalkPlatformType.Linux);
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var platformArns = await GetData();

            var resourceTable = new TypeHintResourceTable
            {
                Columns = new List<TypeHintResourceColumn>()
                {
                    new TypeHintResourceColumn("Platform Branch"),
                    new TypeHintResourceColumn("Platform Version")
                }
            };

            foreach (var platformArn in platformArns)
            {
                var row = new TypeHintResource(platformArn.PlatformArn, $"{platformArn.PlatformBranchName} v{platformArn.PlatformVersion}");
                row.ColumnValues.Add(platformArn.PlatformBranchName);
                row.ColumnValues.Add(platformArn.PlatformVersion);

                resourceTable.Rows.Add(row);
            }

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentValue = _optionSettingHandler.GetOptionSettingValue(recommendation, optionSetting);
            var resourceTable = await GetResources(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<TypeHintResource>(
                idSelector: platform => platform.SystemName,
                displaySelector: platform => platform.DisplayName,
                defaultSelector: platform => platform.SystemName.Equals(currentValue))
            {
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(resourceTable.Rows, "Select the Platform to use:", userInputConfiguration);

            return userResponse.SelectedOption?.SystemName!;
        }
    }
}
