// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using Amazon.CloudFormation.Model;
using Amazon.CloudFront.Model;
using Amazon.CloudWatchEvents.Model;
using Amazon.EC2.Model;
using Amazon.ECR.Model;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.IdentityManagement.Model;
using Amazon.S3.Model;
using Amazon.SecurityToken.Model;
using LoadBalancer = Amazon.ElasticLoadBalancingV2.Model.LoadBalancer;
using Listener = Amazon.ElasticLoadBalancingV2.Model.Listener;
using Amazon.CloudControlApi.Model;

namespace AWS.Deploy.Common.Data
{
    public interface IAWSResourceQueryer
    {
        Task<ResourceDescription> GetCloudControlApiResource(string type, string identifier);
        Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName);
        Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes();
        Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn);
        Task<List<StackResource>> DescribeCloudFormationResources(string stackName);
        Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName);
        Task<LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn);
        Task<List<Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn);
        Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName);
        Task<string> GetS3BucketLocation(string bucketName);
        Task<Amazon.S3.Model.WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName);
        Task<List<Cluster>> ListOfECSClusters(string? ecsClusterName = null);
        Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(string? applicationName = null);
        Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName = null, string? environmentName = null);
        Task<List<Amazon.ElasticBeanstalk.Model.Tag>> ListElasticBeanstalkResourceTags(string resourceArn);
        Task<List<KeyPairInfo>> ListOfEC2KeyPairs();
        Task<string> CreateEC2KeyPair(string keyName, string saveLocation);
        Task<List<Role>> ListOfIAMRoles(string? servicePrincipal);
        Task<List<Vpc>> GetListOfVpcs();
        Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns();
        Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn();
        Task<List<AuthorizationData>> GetECRAuthorizationToken();
        Task<List<Repository>> GetECRRepositories(List<string>? repositoryNames = null);
        Task<Repository> CreateECRRepository(string repositoryName);
        Task<List<Stack>> GetCloudFormationStacks();
        Task<Stack?> GetCloudFormationStack(string stackName);
        Task<GetCallerIdentityResponse> GetCallerIdentity(string awsRegion);
        Task<List<LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType);
        Task<Distribution> GetCloudFrontDistribution(string distributionId);
        Task<List<string>> ListOfDyanmoDBTables();
        Task<List<string>> ListOfSQSQueuesUrls();
        Task<List<string>> ListOfSNSTopicArns();
        Task<List<S3Bucket>> ListOfS3Buckets();
        Task<List<ConfigurationOptionSetting>> GetBeanstalkEnvironmentConfigurationSettings(string environmentName);
        Task<Repository> DescribeECRRepository(string respositoryName);
        Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors();
        Task<List<Subnet>> DescribeSubnets(string? vpcID = null);
        Task<List<SecurityGroup>> DescribeSecurityGroups(string? vpcID = null);
        Task<string?> GetParameterStoreTextValue(string parameterName);
    }
}
