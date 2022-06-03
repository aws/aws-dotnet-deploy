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
using AWS.Deploy.Orchestration.Data;

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

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var securityGroups = await GetData(recommendation, optionSetting);
            return securityGroups.Select(securityGroup => new TypeHintResource(securityGroup.GroupId, securityGroup.GroupId)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var availableSecurityGroups = (await GetData(recommendation, optionSetting)).OrderBy(x => x.VpcId).ToList();
            var groupNamePadding = 0;
            availableSecurityGroups.ForEach(x =>
            {
                if (x.GroupName.Length > groupNamePadding)
                    groupNamePadding = x.GroupName.Length;
            });
            var userInputConfigurationSecurityGroups = new UserInputConfiguration<SecurityGroup>(
                idSelector: securityGroup => securityGroup.GroupId,
                displaySelector: securityGroup => $"{securityGroup.GroupName.PadRight(groupNamePadding)} | {securityGroup.GroupId.PadRight(20)} | {securityGroup.VpcId}",
                defaultSelector: securityGroup => false)
            {
                CanBeEmpty = true,
                CreateNew = false
            };
            return _consoleUtilities.AskUserForList<SecurityGroup>(userInputConfigurationSecurityGroups, availableSecurityGroups, optionSetting, recommendation);
        }
    }
}
