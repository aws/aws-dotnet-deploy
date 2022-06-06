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
    public class ExistingSubnetsCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ExistingSubnetsCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<Subnet>> GetData(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            string? vpcId = null;
            if (!string.IsNullOrEmpty(optionSetting.ParentSettingId))
            {
                var vpcIdOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, optionSetting.ParentSettingId);
                vpcId = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, vpcIdOptionSetting);
            }
            return await _awsResourceQueryer.DescribeSubnets(vpcId);
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var subnets = await GetData(recommendation, optionSetting);

            var resourceTable = new TypeHintResourceTable
            {
                Columns = new List<TypeHintResourceColumn>()
                {
                    new TypeHintResourceColumn("Subnet Id"),
                    new TypeHintResourceColumn("VPC Id"),
                    new TypeHintResourceColumn("Availability Zone")
                }
            };

            foreach (var subnet in subnets.OrderBy(subnet => subnet.VpcId))
            {
                var row = new TypeHintResource(subnet.SubnetId, subnet.SubnetId);
                row.ColumnValues.Add(subnet.SubnetId);
                row.ColumnValues.Add(subnet.VpcId);
                row.ColumnValues.Add(subnet.AvailabilityZone);

                resourceTable.Rows.Add(row);
            }

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var availableSubnets = (await GetData(recommendation, optionSetting)).OrderBy(x => x.VpcId).ToList();
            var resourceTable = await GetResources(recommendation, optionSetting);

            var userInputConfigurationSubnets = new UserInputConfiguration<TypeHintResource>(
                idSelector: subnet => subnet.SystemName,
                displaySelector: subnet => $"{subnet.ColumnValues[0].PadRight(24)} | {subnet.ColumnValues[1].PadRight(21)} | {subnet.ColumnValues[2]}",
                defaultSelector: subnet => false)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            return _consoleUtilities.AskUserForList(userInputConfigurationSubnets, resourceTable.Rows, optionSetting, recommendation);
        }
    }
}
