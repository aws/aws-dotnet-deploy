// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestrator.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<string>> GetListOfElasticBeanstalkApplications(OrchestratorSession session);
        Task<List<string>> GetListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName);
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
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
                var response = await beanstalkClient.DescribeEnvironmentsAsync();
                request.NextToken = response.NextToken;

                environmentNames.AddRange(response.Environments.Select(x => x.EnvironmentName));

            } while (!string.IsNullOrEmpty(request.NextToken));

            return environmentNames;
        }
    }
}
