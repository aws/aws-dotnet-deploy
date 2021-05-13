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
using Amazon.SecurityToken.Model;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;
using Tag = Amazon.ECR.Model.Tag;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestToolAWSResourceQueryer : IAWSResourceQueryer
    {
        public Task<string> CreateEC2KeyPair(string keyName, string saveLocation) => throw new NotImplementedException();
        public Task DeleteECRRepository(string repositoryName) => throw new NotImplementedException();
        public Task<Repository> CreateECRRepository(string name, string recipeId) => throw new NotImplementedException();
        public Task<List<Tag>> ListTagsForECRResource(string resourceArn) => throw new NotImplementedException();
        public Task<List<Stack>> GetCloudFormationStacks() => throw new NotImplementedException();
        public Task<GetCallerIdentityResponse> GetCallerIdentity() => throw new NotImplementedException();

        public Task<List<AuthorizationData>> GetECRAuthorizationToken()
        {
            var authorizationData = new AuthorizationData
            {
                //  Test authorization token is encoded dummy 'username:password' string
                AuthorizationToken = "dXNlcm5hbWU6cGFzc3dvcmQ=",
                ProxyEndpoint = "endpoint"
            };
            return Task.FromResult<List<AuthorizationData>>(new List<AuthorizationData>(){ authorizationData });
        }

        public Task<List<Repository>> GetECRRepositories(List<string> repositoryNames)
        {
            if (repositoryNames.Count == 0)
                return Task.FromResult<List<Repository>>(new List<Repository>() { });

            var repository = new Repository
            {
                RepositoryName = repositoryNames[0]
            };

            return Task.FromResult<List<Repository>>(new List<Repository>() { repository });
        }

        public Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns() => throw new NotImplementedException();
        public Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn() => throw new NotImplementedException();
        public Task<List<Vpc>> GetListOfVpcs() => throw new NotImplementedException();
        public Task<List<KeyPairInfo>> ListOfEC2KeyPairs() => throw new NotImplementedException();
        public Task<List<Amazon.ECS.Model.Cluster>> ListOfECSClusters() => throw new NotImplementedException();
        public Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications() => throw new NotImplementedException();
        public Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string applicationName) => throw new NotImplementedException();
        public Task<List<Role>> ListOfIAMRoles(string servicePrincipal) => throw new NotImplementedException();
    }
}
