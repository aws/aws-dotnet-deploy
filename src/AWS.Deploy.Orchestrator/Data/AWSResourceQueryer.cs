// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestrator.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<string>> GetListOfECSClusters(OrchestratorSession session);
        Task<List<string>> GetListOfElasticBeanstalkApplications(OrchestratorSession session);
        Task<List<string>> GetListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName);
        Task<List<string>> GetListOfVpcEndpoints(OrchestratorSession session);
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public async Task<List<string>> GetListOfECSClusters(OrchestratorSession session)
        {
            var ecsClient = _awsClientFactory.GetAWSClient<IAmazonECS>(session.AWSCredentials, session.AWSRegion);

            var results = new List<string>();

            var request = new ListClustersRequest();

            do
            {
                var response = await ecsClient.ListClustersAsync(request);
                request.NextToken = response.NextToken;

                results.AddRange(response.ClusterArns);


            } while (!string.IsNullOrEmpty(request.NextToken));

            return results;
        }

        public async Task<List<string>> GetListOfElasticBeanstalkApplications(OrchestratorSession session)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(session.AWSCredentials, session.AWSRegion);

            return
                (await beanstalkClient.DescribeApplicationsAsync())
                    .Applications
                    .Select(x => x.ApplicationName).ToList();

        }

        public async Task<List<string>> GetListOfElasticBeanstalkEnvironments(
            OrchestratorSession session,
            string applicationName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(session.AWSCredentials, session.AWSRegion);

            var environmentNames = new List<string>();

            var request = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName
            };
           
            do
            {
                var response = await beanstalkClient.DescribeEnvironmentsAsync(request);
                request.NextToken = response.NextToken;

                environmentNames.AddRange(response.Environments.Select(x => x.EnvironmentName));

            } while (!string.IsNullOrEmpty(request.NextToken));

            return environmentNames;
        }

        public async Task<List<string>> GetListOfVpcEndpoints(OrchestratorSession session)
        {
            var vpcClient = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);

            var vpcEndpoints = new List<string>();

            var request = new DescribeVpcsRequest();

            do
            {
                var response = await vpcClient.DescribeVpcsAsync(request);
                request.NextToken = response.NextToken;

                vpcEndpoints.AddRange(response.Vpcs.Select(x => x.VpcId));

            } while (!string.IsNullOrEmpty(request.NextToken));

            return vpcEndpoints;
        }
    }
}
