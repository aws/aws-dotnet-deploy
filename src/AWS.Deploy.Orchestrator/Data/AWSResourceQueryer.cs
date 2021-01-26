// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.EC2;
using AWS.Deploy.Common;
using Amazon.EC2.Model;
using System.IO;

namespace AWS.Deploy.Orchestrator.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<string>> GetListOfElasticBeanstalkApplications(OrchestratorSession session);
        Task<List<string>> GetListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName);
        Task<IList<string>> GetListOfEC2KeyPairs(OrchestratorSession session);
        Task<string> CreateEC2KeyPair(OrchestratorSession session, string keyName, string saveLocation);
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
                var response = await beanstalkClient.DescribeEnvironmentsAsync(request);
                request.NextToken = response.NextToken;

                environmentNames.AddRange(response.Environments.Select(x => x.EnvironmentName));

            } while (!string.IsNullOrEmpty(request.NextToken));

            return environmentNames;
        }

        public async Task<IList<string>> GetListOfEC2KeyPairs(OrchestratorSession session)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);

            var response = await ec2Client.DescribeKeyPairsAsync();

            var keyPairNames = new List<string>();
            foreach (var keyPair in response.KeyPairs)
            {
                keyPairNames.Add(keyPair.KeyName);
            }

            return keyPairNames;
        }

        public async Task<string> CreateEC2KeyPair(OrchestratorSession session, string keyName, string saveLocation)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);

            var request = new CreateKeyPairRequest() { KeyName = keyName };

            var response = await ec2Client.CreateKeyPairAsync(request);

            File.WriteAllText(Path.Combine(saveLocation, $"{keyName}.pem"), response.KeyPair.KeyMaterial);

            return response.KeyPair.KeyName;
        }
    }
}
