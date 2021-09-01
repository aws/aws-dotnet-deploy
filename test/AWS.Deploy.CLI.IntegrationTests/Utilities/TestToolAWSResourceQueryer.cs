// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using Amazon.CloudFront.Model;
using Amazon.CloudWatchEvents.Model;
using Amazon.EC2.Model;
using Amazon.ECR.Model;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken.Model;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.IntegrationTests.Utilities
{
    public class TestToolAWSResourceQueryer : IAWSResourceQueryer
    {
        public Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn()
        {
            return System.Threading.Tasks.Task.FromResult(new PlatformSummary() { PlatformArn = string.Empty });
        }

        public Task<string> CreateEC2KeyPair(string keyName, string saveLocation) => throw new NotImplementedException();
        public Task<Repository> CreateECRRepository(string repositoryName) => throw new NotImplementedException();
        public Task<List<StackResource>> DescribeCloudFormationResources(string stackName) => throw new NotImplementedException();
        public Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName) => throw new NotImplementedException();
        public Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentId) => throw new NotImplementedException();
        public Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn) => throw new NotImplementedException();
        public Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn) => throw new NotImplementedException();
        public Task<GetCallerIdentityResponse> GetCallerIdentity() => throw new NotImplementedException();
        public Task<List<Stack>> GetCloudFormationStacks() => throw new NotImplementedException();
        public Task<List<AuthorizationData>> GetECRAuthorizationToken() => throw new NotImplementedException();
        public Task<List<Repository>> GetECRRepositories(List<string> repositoryNames) => throw new NotImplementedException();
        public Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns() => throw new NotImplementedException();
        public Task<List<Vpc>> GetListOfVpcs() => throw new NotImplementedException();
        public Task<string> GetS3BucketLocation(string bucketName) => throw new NotImplementedException();
        public Task<Amazon.S3.Model.WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName) => throw new NotImplementedException();
        public Task<List<KeyPairInfo>> ListOfEC2KeyPairs() => throw new NotImplementedException();
        public Task<List<Cluster>> ListOfECSClusters() => throw new NotImplementedException();
        public Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications() => throw new NotImplementedException();
        public Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string applicationName) => throw new NotImplementedException();
        public Task<List<Role>> ListOfIAMRoles(string servicePrincipal) => throw new NotImplementedException();
        public Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn) => throw new NotImplementedException();
        public Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType) => throw new NotImplementedException();
        public Task<Distribution> GetCloudFrontDistribution(string distributionId) => throw new NotImplementedException();
    }
}
