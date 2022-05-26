// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using Amazon.CloudControlApi.Model;
using Amazon.CloudFormation.Model;
using Amazon.CloudFront.Model;
using Amazon.CloudWatchEvents.Model;
using Amazon.EC2.Model;
using Amazon.ECR.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken.Model;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.UnitTests.Utilities
{
    public class TestToolAWSResourceQueryer : IAWSResourceQueryer
    {
        public Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn) => throw new NotImplementedException();

        public Task<string> CreateEC2KeyPair(string keyName, string saveLocation) => throw new NotImplementedException();
        public Task<Repository> CreateECRRepository(string repositoryName) => throw new NotImplementedException();
        public Task<List<Stack>> GetCloudFormationStacks() => throw new NotImplementedException();
        public Task<Stack> GetCloudFormationStack(string stackName) => throw new NotImplementedException();

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

        public Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn()
        {
            return Task.FromResult(new PlatformSummary() { PlatformArn = string.Empty });
        }

        public Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns() => throw new NotImplementedException();
        public Task<List<Vpc>> GetListOfVpcs() => throw new NotImplementedException();
        public Task<List<KeyPairInfo>> ListOfEC2KeyPairs() => throw new NotImplementedException();
        public Task<List<Amazon.ECS.Model.Cluster>> ListOfECSClusters(string ecsClusterName) => throw new NotImplementedException();
        public Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(string applicationName) => throw new NotImplementedException();
        public Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string applicationName, string environmentName) => throw new NotImplementedException();
        public Task<List<Role>> ListOfIAMRoles(string servicePrincipal) => throw new NotImplementedException();
        public Task<List<StackResource>> DescribeCloudFormationResources(string stackName) => throw new NotImplementedException();
        public Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentId) => throw new NotImplementedException();
        public Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn) => throw new NotImplementedException();
        public Task<S3Region> GetS3BucketLocation(string bucketName) => throw new NotImplementedException();
        public Task<Amazon.S3.Model.WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName) => throw new NotImplementedException();
        public Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn) => throw new NotImplementedException();
        public Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName) => throw new NotImplementedException();
        Task<string> IAWSResourceQueryer.GetS3BucketLocation(string bucketName) => throw new NotImplementedException();
        public Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType) => throw new NotImplementedException();
        public Task<Distribution> GetCloudFrontDistribution(string distributionId) => throw new NotImplementedException();
        public Task<List<string>> ListOfDyanmoDBTables() => throw new NotImplementedException();
        public Task<List<string>> ListOfSQSQueuesUrls() => throw new NotImplementedException();
        public Task<List<string>> ListOfSNSTopicArns() => throw new NotImplementedException();
        public Task<List<Amazon.S3.Model.S3Bucket>> ListOfS3Buckets() => throw new NotImplementedException();
        public Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes() => throw new NotImplementedException();
        public Task<InstanceTypeInfo> DescribeInstanceType(string instanceType) => throw new NotImplementedException();
        public Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName) => throw new NotImplementedException();
        public Task<List<Amazon.ElasticBeanstalk.Model.Tag>> ListElasticBeanstalkResourceTags(string resourceArn) => throw new NotImplementedException();
        public Task<List<ConfigurationOptionSetting>> GetBeanstalkEnvironmentConfigurationSettings(string environmentId) => throw new NotImplementedException();
        public Task<GetCallerIdentityResponse> GetCallerIdentity(string awsRegion) => throw new NotImplementedException();
        public Task<Repository> DescribeECRRepository(string respositoryName) => throw new NotImplementedException();
        public Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors() => throw new NotImplementedException();
        public Task<List<Subnet>> DescribeSubnets(string vpcID = null) => throw new NotImplementedException();
        public Task<List<SecurityGroup>> DescribeSecurityGroups(string vpcID = null) => throw new NotImplementedException();
        public Task<string> GetParameterStoreTextValue(string parameterName) => throw new NotImplementedException();
        public Task<ResourceDescription> GetCloudControlApiResource(string type, string identifier) => throw new NotImplementedException();
    }
}
