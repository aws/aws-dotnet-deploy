// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ExistingSecurityGroupsCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ExistingSecurityGroupsCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<SecurityGroup>> GetData(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            string? vpcId = null;
            if (!string.IsNullOrEmpty(optionSetting.ParentSettingId))
            {
                var vpcIdOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, optionSetting.ParentSettingId);
                vpcId = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, vpcIdOptionSetting);
            }
            return await _awsResourceQueryer.DescribeSecurityGroups(vpcId);
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var securityGroups = await GetData(recommendation, optionSetting);

            var resourceTable = new TypeHintResourceTable
            {
                Columns = new List<TypeHintResourceColumn>()
                {
                    new TypeHintResourceColumn("Group Name"),
                    new TypeHintResourceColumn("Group Id"),
                    new TypeHintResourceColumn("VPC Id")
                }
            };

            foreach (var securityGroup in securityGroups.OrderBy(securityGroup => securityGroup.VpcId))
            {
                var row = new TypeHintResource(securityGroup.GroupId, $"{securityGroup.GroupName} ({securityGroup.GroupId})");
                row.ColumnValues.Add(securityGroup.GroupName);
                row.ColumnValues.Add(securityGroup.GroupId);
                row.ColumnValues.Add(securityGroup.VpcId);

                resourceTable.Rows.Add(row);
            }

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var resourceTable = await GetResources(recommendation, optionSetting);

            var groupNamePadding = 0;
            resourceTable.Rows.ForEach(row =>
            {
                if (row.ColumnValues[0].Length > groupNamePadding)
                    groupNamePadding = row.ColumnValues[0].Length;
            });

            var userInputConfigurationSecurityGroups = new UserInputConfiguration<TypeHintResource>(
                idSelector: securityGroup => securityGroup.SystemName,
                displaySelector: securityGroup => $"{securityGroup.ColumnValues[0].PadRight(groupNamePadding)} | {securityGroup.ColumnValues[1].PadRight(20)} | {securityGroup.ColumnValues[2]}",
                defaultSelector: securityGroup => false)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            return _consoleUtilities.AskUserForList(userInputConfigurationSecurityGroups, resourceTable.Rows, optionSetting, recommendation);
        }
    }
}
