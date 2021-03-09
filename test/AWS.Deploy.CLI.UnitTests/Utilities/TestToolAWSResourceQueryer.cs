// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using Amazon.EC2.Model;
using Amazon.ECR.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.IdentityManagement.Model;
using AWS.Deploy.Orchestrator;
using AWS.Deploy.Orchestrator.Data;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestToolAWSResourceQueryer : IAWSResourceQueryer
    {
        public Task<string> CreateEC2KeyPair(OrchestratorSession session, string keyName, string saveLocation) => throw new NotImplementedException();
        public Task<Repository> CreateECRRepository(OrchestratorSession session, string repositoryName) => throw new NotImplementedException();
        public Task<List<Stack>> GetCloudFormationStacks(OrchestratorSession session) => throw new NotImplementedException();

        public Task<List<AuthorizationData>> GetECRAuthorizationToken(OrchestratorSession session)
        {
            var authorizationData = new AuthorizationData
            {
                //  Test authorization token is encoded dummy 'username:password' string
                AuthorizationToken = "dXNlcm5hbWU6cGFzc3dvcmQ=",
                ProxyEndpoint = "endpoint"
            };
            return Task.FromResult<List<AuthorizationData>>(new List<AuthorizationData>(){ authorizationData });
        }

        public Task<List<Repository>> GetECRRepositories(OrchestratorSession session, List<string> repositoryNames)
        {
            if (repositoryNames.Count == 0)
                return Task.FromResult<List<Repository>>(new List<Repository>() { });

            var repository = new Repository
            {
                RepositoryName = repositoryNames[0]
            };

            return Task.FromResult<List<Repository>>(new List<Repository>() { repository });
        }

        public Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(OrchestratorSession session) => throw new NotImplementedException();
        public Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(OrchestratorSession session) => throw new NotImplementedException();
        public Task<List<Vpc>> GetListOfVpcs(OrchestratorSession session) => throw new NotImplementedException();
        public Task<List<KeyPairInfo>> ListOfEC2KeyPairs(OrchestratorSession session) => throw new NotImplementedException();
        public Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(OrchestratorSession session) => throw new NotImplementedException();
        public Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName) => throw new NotImplementedException();
        public Task<List<Role>> ListOfIAMRoles(OrchestratorSession session, string servicePrincipal) => throw new NotImplementedException();
    }
}
