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

            return new VpcTypeHintResponse(
                userResponse.SelectedOption?.IsDefault == true,
                userResponse.CreateNew,
                userResponse.SelectedOption?.VpcId ?? ""
                );
        }
    }
}
