// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    /// <summary>
    /// The <see cref="VpcCommand"/> type hint orchestrates the VPC object in ECS Fargate environments.
    /// </summary>
    public class VpcCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IToolInteractiveService _toolInteractiveService;

        public VpcCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IToolInteractiveService toolInteractiveService)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _toolInteractiveService = toolInteractiveService;
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
            var currentVpcTypeHintResponse = optionSetting.GetTypeHintData<VpcTypeHintResponse>();

            var vpcs = await GetData();

            if (!vpcs.Any())
            {
                _toolInteractiveService.WriteLine();
                _toolInteractiveService.WriteLine("There are no VPCs in the selected account. The only option is to create a new one.");
                return new VpcTypeHintResponse(false, true, string.Empty);
            }

            var userInputConfig = new UserInputConfiguration<Vpc>(
                idSelector: vpc => vpc.VpcId,
                displaySelector: vpc => vpc.GetDisplayableVpc(),
                defaultSelector: vpc =>
                    !string.IsNullOrEmpty(currentVpcTypeHintResponse?.VpcId)
                        ? vpc.VpcId == currentVpcTypeHintResponse.VpcId
                        : vpc.IsDefault);

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                vpcs,
                "Select a VPC",
                userInputConfig);

            var response = new VpcTypeHintResponse(
                userResponse.SelectedOption?.IsDefault == true,
                userResponse.CreateNew,
                userResponse.SelectedOption?.VpcId ?? ""
                );

            // If creating a new VPC, the user is unable to specify specific subnet(s) at this point
            if (response.CreateNew)
            {
                return response;
            }

            // Otherwise prompt the user to select one or more subnets
            if (!string.IsNullOrEmpty(response.VpcId))
            {
                var availableSubnets = (await _awsResourceQueryer.DescribeSubnets(response.VpcId)).OrderBy(x => x.SubnetId).ToList();

                // If there are no subnets, don't use this VPC
                if (!availableSubnets.Any())
                {
                    _toolInteractiveService.WriteLine();
                    _toolInteractiveService.WriteLine("The selected VPC does not have any Subnets. Please select a VPC with Subnets or create a new one.");

                    // Reset back to "Create New". In the event that the default VPC was selected AND doesn't have subnets,
                    // there isn't a clear subnet to reset back to.
                    return new VpcTypeHintResponse(false, true, string.Empty);
                }

               var userInputConfigurationSubnets = new UserInputConfiguration<Subnet>(
                idSelector: subnet => subnet.SubnetId,
                displaySelector: subnet => $"{subnet.SubnetId.PadRight(24)} | {subnet.VpcId.PadRight(21)} | {subnet.AvailabilityZone}",
                defaultSelector: subnet => false)
                {
                    CanBeEmpty = true,  // Subnets were added to this type hint after 1.0, so we cannot require them here. We'll continue to fallback on CDK's defaulting logic
                    CreateNew = false
                };

                var subnetsOptionSetting = optionSetting.ChildOptionSettings.First(x => x.Id.Equals("Subnets"));
                _toolInteractiveService.WriteLine($"{subnetsOptionSetting.Id}:");
                _toolInteractiveService.WriteLine(subnetsOptionSetting.Description);
                var subnets = _consoleUtilities.AskUserForList(userInputConfigurationSubnets, availableSubnets, subnetsOptionSetting, recommendation);

                response.Subnets = subnets;
            }

            return response;
        }
    }
}
