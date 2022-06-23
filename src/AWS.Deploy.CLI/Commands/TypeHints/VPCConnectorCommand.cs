// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class VPCConnectorCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public VPCConnectorCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<VpcConnector>> GetData()
        {
            return await _awsResourceQueryer.DescribeAppRunnerVpcConnectors();
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcConnectors = await GetData();

            var resourceTable = new TypeHintResourceTable()
            {
                Rows = vpcConnectors.Select(vpcConnector => new TypeHintResource(vpcConnector.VpcConnectorArn, vpcConnector.VpcConnectorName)).ToList()
            };

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            _toolInteractiveService.WriteLine();
            var useVpcConnectorOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("UseVPCConnector"));
            var useVpcConnectorValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, useVpcConnectorOptionSetting) ?? "false";
            var useVpcConnectorAnswer = _consoleUtilities.AskYesNoQuestion(useVpcConnectorOptionSetting.Description, useVpcConnectorValue);
            var useVpcConnector = useVpcConnectorAnswer == YesNo.Yes;

            if (!useVpcConnector)
                return new VPCConnectorTypeHintResponse()
                {
                    UseVPCConnector = false
                };

            var vpcConnectors = await GetData();
            var createNewVPCConnector = true;

            if (vpcConnectors.Any())
            {
                _toolInteractiveService.WriteLine();
                var createNewOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("CreateNew"));
                var createNew = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, createNewOptionSetting) ?? "false";
                var createNewVPCConnectorAnswer = _consoleUtilities.AskYesNoQuestion(createNewOptionSetting.Description, createNew);
                createNewVPCConnector = createNewVPCConnectorAnswer == YesNo.Yes;
            }

            _toolInteractiveService.WriteLine();
            if (createNewVPCConnector)
            {
                _toolInteractiveService.WriteLine("In order to create a new VPC Connector, you need to select 1 or more Subnets as well as 1 or more Security groups.");
                _toolInteractiveService.WriteLine();

                var vpcOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("VpcId"));
                var currentVpcValue = _optionSettingHandler.GetOptionSettingValue(recommendation, vpcOptionSetting).ToString();
                var userInputConfigurationVPCs = new UserInputConfiguration<Vpc>(
                    idSelector: vpc => vpc.VpcId,
                    displaySelector: vpc => vpc.VpcId,
                    defaultSelector: vpc => !string.IsNullOrEmpty(currentVpcValue) ? vpc.VpcId.Equals(currentVpcValue) : vpc.IsDefault)
                {
                    CanBeEmpty = false,
                    CreateNew = false
                };
                var availableVpcs = await _awsResourceQueryer.GetListOfVpcs();
                var vpc = _consoleUtilities.AskUserToChooseOrCreateNew<Vpc>(availableVpcs, "Select a VPC:", userInputConfigurationVPCs);

                if (vpc.SelectedOption == null)
                    return new VPCConnectorTypeHintResponse()
                    {
                        UseVPCConnector = false
                    };

                var availableSubnets = (await _awsResourceQueryer.DescribeSubnets(vpc.SelectedOption.VpcId)).OrderBy(x => x.SubnetId).ToList();
                var userInputConfigurationSubnets = new UserInputConfiguration<Subnet>(
                    idSelector: subnet => subnet.SubnetId,
                    displaySelector: subnet => $"{subnet.SubnetId.PadRight(24)} | {subnet.VpcId.PadRight(21)} | {subnet.AvailabilityZone}",
                    defaultSelector: subnet => false)
                {
                    CanBeEmpty = false,
                    CreateNew = false
                };
                var subnetsOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("Subnets"));
                _toolInteractiveService.WriteLine($"{subnetsOptionSetting.Id}:");
                _toolInteractiveService.WriteLine(subnetsOptionSetting.Description);
                var subnets = _consoleUtilities.AskUserForList<Subnet>(userInputConfigurationSubnets, availableSubnets, subnetsOptionSetting, recommendation);

                var availableSecurityGroups = (await _awsResourceQueryer.DescribeSecurityGroups(vpc.SelectedOption.VpcId)).OrderBy(x => x.VpcId).ToList();
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
                    CanBeEmpty = false,
                    CreateNew = false
                };
                var securityGroupsOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("SecurityGroups"));
                _toolInteractiveService.WriteLine($"{securityGroupsOptionSetting.Id}:");
                _toolInteractiveService.WriteLine(securityGroupsOptionSetting.Description);
                var securityGroups = _consoleUtilities.AskUserForList<SecurityGroup>(userInputConfigurationSecurityGroups, availableSecurityGroups, securityGroupsOptionSetting, recommendation);

                return new VPCConnectorTypeHintResponse()
                {
                    UseVPCConnector = true,
                    CreateNew = true,
                    VpcId = vpc.SelectedOption.VpcId,
                    Subnets = subnets,
                    SecurityGroups = securityGroups
                };
            }
            else
            {
                var userInputConfiguration = new UserInputConfiguration<VpcConnector>(
                    idSelector: vpcConnector => vpcConnector.VpcConnectorArn,
                    displaySelector: vpcConnector => vpcConnector.VpcConnectorName,
                    defaultSelector: vpcConnector => false
                    )
                {
                    CanBeEmpty = false,
                    CreateNew = false
                };

                var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(vpcConnectors, "Select VPC Connector:", userInputConfiguration);

                var selectedVpcConnector = userResponse.SelectedOption?.VpcConnectorArn ?? string.Empty;

                return new VPCConnectorTypeHintResponse()
                {
                    UseVPCConnector = true,
                    CreateNew = false,
                    VpcConnectorId = selectedVpcConnector
                };
            }
        }
    }
}
