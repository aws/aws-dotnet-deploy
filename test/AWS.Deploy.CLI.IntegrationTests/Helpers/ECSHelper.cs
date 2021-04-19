// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECS;
using Amazon.ECS.Model;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class ECSHelper
    {
        private readonly IAmazonECS _client;

        public ECSHelper(IAmazonECS client)
        {
            _client = client;
        }

        public async Task<string> GetLogGroup(string clusterName)
        {
            var taskDefinition = await GetTaskDefinition(clusterName);
            return await GetAwsLogGroup(taskDefinition);
        }

        public async Task<Cluster> GetCluster(string clusterName)
        {
            var request = new DescribeClustersRequest
            {
                Clusters = new List<string>
                {
                    clusterName
                }
            };
            var response = await _client.DescribeClustersAsync(request);
            if (response.Clusters.Count != 1)
            {
                throw new AmazonECSException($"{clusterName} cluster does not exist.");
            }

            return response.Clusters.First();
        }

        private async Task<string> GetTaskDefinition(string clusterName)
        {
            var request = new ListTaskDefinitionsRequest();

            var response = await _client.ListTaskDefinitionsAsync(request);
            return response.TaskDefinitionArns.First(taskDefinitionArn =>
            {
                // arn:aws:ecs:us-west-2:727033484140:task-definition/ConsoleAppServiceTaskDefinition6663F6FD:1
                var taskDefinitionName = taskDefinitionArn.Split('/')[1];
                return taskDefinitionName.StartsWith(clusterName);
            });
        }

        private async Task<string> GetAwsLogGroup(string taskDefinition)
        {
            var request = new DescribeTaskDefinitionRequest
            {
                TaskDefinition = taskDefinition
            };

            var response = await _client.DescribeTaskDefinitionAsync(request);
            var containerDefinition = response.TaskDefinition.ContainerDefinitions.First();
            return containerDefinition.LogConfiguration.Options["awslogs-group"];
        }
    }
}
