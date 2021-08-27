// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ExistingApplicationLoadBalancerCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public ExistingApplicationLoadBalancerCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var loadBalancers = await _awsResourceQueryer.ListOfLoadBalancers(LoadBalancerTypeEnum.Application);
            var currentValue = recommendation.GetOptionSettingValue<string>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<LoadBalancer>(
                loadBalancer => loadBalancer.LoadBalancerName,
                loadBalancer => loadBalancer.LoadBalancerArn.Equals(currentValue))
            {
                AskNewName = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(loadBalancers, "Select Load Balancer to deploy to:", userInputConfiguration);

            return userResponse.SelectedOption?.LoadBalancerArn ?? string.Empty;
        }
    }
}
