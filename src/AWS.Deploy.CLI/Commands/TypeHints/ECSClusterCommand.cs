// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ECSClusterCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;

        public ECSClusterCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
        }

        private async Task<List<Cluster>> GetData()
        {
            return await _awsResourceQueryer.ListOfECSClusters();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var clusters = await GetData();
            return clusters.Select(x => new TypeHintResource(x.ClusterArn, x.ClusterName)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var clusters = await GetData();
            var currentTypeHintResponse = recommendation.GetOptionSettingValue<ECSClusterTypeHintResponse>(optionSetting);

            var userInputConfiguration = new UserInputConfiguration<Cluster>(
                idSelector: cluster => cluster.ClusterArn,
                displaySelector: cluster => cluster.ClusterName,
                defaultSelector: cluster => cluster.ClusterArn.Equals(currentTypeHintResponse?.ClusterArn),
                defaultNewName: currentTypeHintResponse.NewClusterName)
            {
                AskNewName = true
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(clusters, "Select ECS cluster to deploy to:", userInputConfiguration);

            return new ECSClusterTypeHintResponse(
                userResponse.CreateNew,
                userResponse.SelectedOption?.ClusterArn ?? string.Empty,
                userResponse.NewName ?? string.Empty);
        }
    }
}
