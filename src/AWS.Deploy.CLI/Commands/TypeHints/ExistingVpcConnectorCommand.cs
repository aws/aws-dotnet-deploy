// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ExistingVpcConnectorCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public ExistingVpcConnectorCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<VpcConnector>> GetData()
        {
            return await _awsResourceQueryer.DescribeAppRunnerVpcConnectors();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcConnectors = await GetData();
            return vpcConnectors.Select(vpcConnector => new TypeHintResource(vpcConnector.VpcConnectorArn, vpcConnector.VpcConnectorName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcConnectors = await GetData();
            var currentVpcConnector = recommendation.GetOptionSettingValue<string>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<VpcConnector>(
                vpcConnector => vpcConnector.VpcConnectorName,
                vpcConnector => vpcConnector.VpcConnectorArn.Equals(currentVpcConnector),
                currentVpcConnector)
            {
                CanBeEmpty = true,
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(vpcConnectors, "Select VPC Connector:", userInputConfiguration);

            return userResponse.SelectedOption?.VpcConnectorArn ?? string.Empty;
        }
    }
}
