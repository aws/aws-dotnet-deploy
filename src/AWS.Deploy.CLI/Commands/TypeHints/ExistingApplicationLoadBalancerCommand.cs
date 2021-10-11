// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;
using LoadBalancer = Amazon.ElasticLoadBalancingV2.Model.LoadBalancer;

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

        private async Task<List<LoadBalancer>> GetData()
        {
            return await _awsResourceQueryer.ListOfLoadBalancers(LoadBalancerTypeEnum.Application);
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var loadBalancers = await GetData();
            return loadBalancers.Select(x => new TypeHintResource(x.LoadBalancerArn, x.LoadBalancerName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var loadBalancers = await GetData();
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
