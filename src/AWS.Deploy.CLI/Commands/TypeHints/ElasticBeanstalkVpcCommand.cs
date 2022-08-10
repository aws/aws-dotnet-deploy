// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.ECS.Model;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// The <see cref="ElasticBeanstalkVpcCommand"/> type hint orchestrates the VPC object in Elastic Beanstalk environments.
    /// </summary>
    public class ElasticBeanstalkVpcCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ElasticBeanstalkVpcCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<Vpc>> GetData()
        {
            return await _awsResourceQueryer.GetListOfVpcs();
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcs = await GetData();
            var resourceTable = new TypeHintResourceTable();

            resourceTable.Rows =  vpcs.ToDictionary(x => x.VpcId, x => x.GetDisplayableVpc()).Select(x => new TypeHintResource(x.Key, x.Value)).ToList();

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            _toolInteractiveService.WriteLine();

            // Ask user if they want to Use a VPC
            var useVpcOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("UseVPC"));
            var useVpcValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, useVpcOptionSetting) ?? "false";
            var useVpcAnswer = _consoleUtilities.AskYesNoQuestion(useVpcOptionSetting.Description, useVpcValue);
            var useVpc = useVpcAnswer == YesNo.Yes;

            // If user doesn't want to Use VPC, no need to continue
            if (!useVpc)
                return new ElasticBeanstalkVpcTypeHintResponse()
                {
                    UseVPC = false
                };

            // Retrieve all the available VPCs
            var vpcs = await GetData();

            // If there are no VPCs, create a new one
            if (!vpcs.Any())
            {
                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteLine("There are no VPCs in the selected account. The only option is to create a new one.");
                return new ElasticBeanstalkVpcTypeHintResponse
                {
                    UseVPC = true,
                    CreateNew = true
                };
            }

            // Ask user to select a VPC from the available ones
            _toolInteractiveService.WriteLine();
            var currentVpcTypeHintResponse = optionSetting.GetTypeHintData<ElasticBeanstalkVpcTypeHintResponse>();
            var vpcOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("VpcId"));
            var currentVpcValue = _optionSettingHandler.GetOptionSettingValue(recommendation, vpcOptionSetting).ToString();
            var userInputConfigurationVPCs = new UserInputConfiguration<Vpc>(
                idSelector: vpc => vpc.VpcId,
                displaySelector: vpc => vpc.GetDisplayableVpc(),
                defaultSelector: vpc =>
                    !string.IsNullOrEmpty(currentVpcTypeHintResponse?.VpcId)
                        ? vpc.VpcId == currentVpcTypeHintResponse.VpcId
                        : vpc.IsDefault)
            {
                CanBeEmpty = false,
                CreateNew = true
            };
            var vpc = _consoleUtilities.AskUserToChooseOrCreateNew<Vpc>(vpcs, "Select a VPC:", userInputConfigurationVPCs);

            // Create a new VPC if the user wants to do that
            if (vpc.CreateNew)
                return new ElasticBeanstalkVpcTypeHintResponse
                {
                    UseVPC = true,
                    CreateNew = true
                };

            // If for some reason an option was not selected, don't use a VPC
            if (vpc.SelectedOption == null)
                return new ElasticBeanstalkVpcTypeHintResponse
                {
                    UseVPC = false
                };

            // Retrieve available Subnets based on the selected VPC
            var availableSubnets = (await _awsResourceQueryer.DescribeSubnets(vpc.SelectedOption.VpcId)).OrderBy(x => x.SubnetId).ToList();

            // If there are no subnets, don't use a VPC
            if (!availableSubnets.Any())
            {
                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteLine("The selected VPC does not have any Subnets. Please select a VPC with Subnets.");
                return new ElasticBeanstalkVpcTypeHintResponse
                {
                    UseVPC = false
                };
            }

            // Ask user to select subnets based on the selected VPC
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

            // Retrieve available security groups based on the selected VPC
            var availableSecurityGroups = (await _awsResourceQueryer.DescribeSecurityGroups(vpc.SelectedOption.VpcId)).OrderBy(x => x.VpcId).ToList();
            if (!availableSecurityGroups.Any())
                return new ElasticBeanstalkVpcTypeHintResponse
                {
                    UseVPC = true,
                    CreateNew = false,
                    VpcId = vpc.SelectedOption.VpcId,
                    Subnets = subnets
                };

            // Get the length of the longest group name to do padding when displaying the security groups
            var groupNamePadding = 0;
            availableSecurityGroups.ForEach(x =>
            {
                if (x.GroupName.Length > groupNamePadding)
                    groupNamePadding = x.GroupName.Length;
            });

            // Ask user to select security groups
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

            return new ElasticBeanstalkVpcTypeHintResponse
            {
                UseVPC = true,
                CreateNew = false,
                VpcId = vpc.SelectedOption.VpcId,
                Subnets = subnets,
                SecurityGroups = securityGroups
            };
        }
    }
}
