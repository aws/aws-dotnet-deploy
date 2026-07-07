// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.EC2.Model;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class AgentCoreVpcCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;

        public AgentCoreVpcCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcs = await _awsResourceQueryer.GetListOfVpcs() ?? new List<Vpc>();
            var resourceTable = new TypeHintResourceTable
            {
                Rows = vpcs
                    .Select(x => new TypeHintResource(x.VpcId, x.GetDisplayableVpc()))
                    .ToList()
            };
            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            _toolInteractiveService.WriteLine();

            var currentResponse = optionSetting.GetTypeHintData<AgentCoreVpcTypeHintResponse>();
            var currentUseVpc = currentResponse?.UseVPC ?? false;
            var useVpcAnswer = _consoleUtilities.AskYesNoQuestion(
                "Do you want to place the AgentCore Runtime in a VPC?", currentUseVpc ? "true" : "false");

            if (useVpcAnswer == YesNo.No)
            {
                return new AgentCoreVpcTypeHintResponse { UseVPC = false };
            }

            var vpcs = await _awsResourceQueryer.GetListOfVpcs() ?? new List<Vpc>();

            if (!vpcs.Any())
            {
                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteLine("There are no VPCs in the selected account. A new one will be created.");
                return new AgentCoreVpcTypeHintResponse { UseVPC = true, CreateNew = true };
            }

            _toolInteractiveService.WriteLine();

            var userInputConfig = new UserInputConfiguration<Vpc>(
                idSelector: vpc => vpc.VpcId,
                displaySelector: vpc => vpc.GetDisplayableVpc(),
                defaultSelector: vpc =>
                    !string.IsNullOrEmpty(currentResponse?.VpcId)
                        ? vpc.VpcId == currentResponse.VpcId
                        : vpc.IsDefault ?? false)
            {
                CanBeEmpty = false,
                CreateNew = true
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                vpcs, "Select a VPC:", userInputConfig);

            if (userResponse.CreateNew || userResponse.SelectedOption == null)
            {
                return new AgentCoreVpcTypeHintResponse { UseVPC = true, CreateNew = true };
            }

            var selectedVpcId = userResponse.SelectedOption.VpcId;

            // Ask for security groups in the selected VPC
            var securityGroups = await AskForSecurityGroups(selectedVpcId, optionSetting, recommendation);

            return new AgentCoreVpcTypeHintResponse
            {
                UseVPC = true,
                CreateNew = false,
                VpcId = selectedVpcId,
                SecurityGroups = securityGroups
            };
        }

        private async Task<SortedSet<string>> AskForSecurityGroups(string vpcId, OptionSettingItem optionSetting, Recommendation recommendation)
        {
            var availableSecurityGroups = (await _awsResourceQueryer.DescribeSecurityGroups(vpcId) ?? new List<SecurityGroup>())
                .OrderBy(x => x.GroupName)
                .ToList();

            if (!availableSecurityGroups.Any())
                return new SortedSet<string>();

            var groupNamePadding = availableSecurityGroups.Max(x => x.GroupName.Length);

            var userInputConfig = new UserInputConfiguration<SecurityGroup>(
                idSelector: sg => sg.GroupId,
                displaySelector: sg => $"{sg.GroupName.PadRight(groupNamePadding)} | {sg.GroupId.PadRight(20)} | {sg.VpcId}",
                defaultSelector: sg => false)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            var securityGroupsOptionSetting = optionSetting.ChildOptionSettings.FirstOrDefault(x => x.Id.Equals("SecurityGroups"));
            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine("Security Groups:");
            _toolInteractiveService.WriteLine(securityGroupsOptionSetting?.Description ?? "Select security groups to assign to the AgentCore Runtime.");

            return _consoleUtilities.AskUserForList(userInputConfig, availableSecurityGroups, securityGroupsOptionSetting ?? optionSetting, recommendation);
        }
    }
}
