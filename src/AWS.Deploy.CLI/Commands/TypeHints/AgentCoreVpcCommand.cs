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

            var useVpcAnswer = _consoleUtilities.AskYesNoQuestion(
                "Do you want to place the AgentCore Runtime in a VPC?", "false");

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

            var currentResponse = optionSetting.GetTypeHintData<AgentCoreVpcTypeHintResponse>();
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

            if (userResponse.CreateNew)
            {
                return new AgentCoreVpcTypeHintResponse { UseVPC = true, CreateNew = true };
            }

            return new AgentCoreVpcTypeHintResponse
            {
                UseVPC = true,
                CreateNew = false,
                VpcId = userResponse.SelectedOption!.VpcId
            };
        }
    }
}
