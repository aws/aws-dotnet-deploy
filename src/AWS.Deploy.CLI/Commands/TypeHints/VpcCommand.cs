// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class VpcCommand : ITypeHintCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly OrchestratorSession _session;
        private readonly ConsoleUtilities _consoleUtilities;

        public VpcCommand(IToolInteractiveService toolInteractiveService, IAWSResourceQueryer awsResourceQueryer, OrchestratorSession session, ConsoleUtilities consoleUtilities)
        {
            _toolInteractiveService = toolInteractiveService;
            _awsResourceQueryer = awsResourceQueryer;
            _session = session;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentVpcTypeHintResponse = optionSetting.GetTypeHintData<VpcTypeHintResponse>();

            var vpcs = await _awsResourceQueryer.GetListOfVpcs(_session);

            var userInputConfig = new UserInputConfiguration<Vpc>
            {
                DisplaySelector = vpc =>
                {
                    var name = vpc.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? string.Empty;
                    var namePart =
                        string.IsNullOrEmpty(name)
                            ? ""
                            : $" ({name}) ";

                    var isDefaultPart =
                        vpc.IsDefault
                            ? " *** Account Default VPC ***"
                            : "";

                    return $"{vpc.VpcId}{namePart}{isDefaultPart}";
                },
                DefaultSelector = vpc =>
                    !string.IsNullOrEmpty(currentVpcTypeHintResponse?.VpcId)
                        ? vpc.VpcId == currentVpcTypeHintResponse.VpcId
                        : vpc.IsDefault
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                vpcs,
                "Select a VPC",
                userInputConfig);

            return new VpcTypeHintResponse
            {
                IsDefault = userResponse.SelectedOption?.IsDefault == true,
                CreateNew = userResponse.CreateNew,
                VpcId = userResponse.SelectedOption?.VpcId ?? ""
            };
        }
    }
}
