// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

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

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var subnets = await GetData(recommendation, optionSetting);
            return subnets.Select(subnet => new TypeHintResource(subnet.SubnetId, subnet.SubnetId)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var availableSubnets = (await GetData(recommendation, optionSetting)).OrderBy(x => x.VpcId).ToList();
            var userInputConfigurationSubnets = new UserInputConfiguration<Subnet>(
                idSelector: subnet => subnet.SubnetId,
                displaySelector: subnet => $"{subnet.SubnetId.PadRight(24)} | {subnet.VpcId.PadRight(21)} | {subnet.AvailabilityZone}",
                defaultSelector: subnet => false)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            return _consoleUtilities.AskUserForList<Subnet>(userInputConfigurationSubnets, availableSubnets, optionSetting, recommendation);
        }
    }
}
