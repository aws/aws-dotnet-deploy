// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ExistingVpcConnectorCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ExistingVpcConnectorCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<VpcConnector>> GetData()
        {
            return await _awsResourceQueryer.DescribeAppRunnerVpcConnectors();
        }

        public async Task<TypeHintResourceTable> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcConnectors = await GetData();

            var resourceTable = new TypeHintResourceTable
            {
                Rows = vpcConnectors.Select(vpcConnector => new TypeHintResource(vpcConnector.VpcConnectorArn, vpcConnector.VpcConnectorName)).ToList()
            };

            return resourceTable;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcConnectors = await GetData();
            var currentVpcConnector = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting);

            var userInputConfiguration = new UserInputConfiguration<VpcConnector>(
                idSelector: vpcConnector => vpcConnector.VpcConnectorArn,
                displaySelector: vpcConnector => vpcConnector.VpcConnectorName,
                defaultSelector: vpcConnector => vpcConnector.VpcConnectorArn.Equals(currentVpcConnector),
                defaultNewName: currentVpcConnector)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(vpcConnectors, "Select VPC Connector:", userInputConfiguration);

            return userResponse.SelectedOption?.VpcConnectorArn ?? string.Empty;
        }
    }
}
