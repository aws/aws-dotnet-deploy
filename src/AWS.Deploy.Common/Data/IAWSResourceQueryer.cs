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
    /// <summary>
    /// Enum for filtering the type of Elastic Beanstalk platform to deploy to.
    /// </summary>
    public enum BeanstalkPlatformType { Linux, Windows }


    /// <summary>
    /// Retrieves AWS resources
    /// </summary>
    /// <remarks>
    /// This is meant to be a lightweight wrapper around the SDK,
    /// business logic should generally be implemented in the caller.
    /// </remarks>
    public interface IAWSResourceQueryer
    {
        Task<List<Stack>> DescribeStacks(string stackName);
        Task<DeleteStackResponse> DeleteStack(string stackName);
        Task<Vpc?> GetDefaultVpc();
        Task<ResourceDescription> GetCloudControlApiResource(string type, string identifier);
        Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName);

        /// <summary>
        /// Lists all of the EC2 instance types available in the deployment region without any filtering
        /// </summary>
        /// <returns>List of <see cref="InstanceTypeInfo"/></returns>
        Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes();

        /// <summary>
        /// Describes a single EC2 instance type
        /// </summary>
        /// <param name="instanceType">Instance type (for example, "t2.micro")</param>
        /// <returns>The first <see cref="InstanceTypeInfo"/> if the specified type exists</returns>
        Task<InstanceTypeInfo?> DescribeInstanceType(string instanceType);

        Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn);
        Task<List<StackResource>> DescribeCloudFormationResources(string stackName);

        /// <summary>
        /// Describes the compute environment of an Elastic Beanstalk application
        /// </summary>
        /// <param name="environmentName">Environment name</param>
        /// <returns>The Elastic Beanstalk environment</returns>
        Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName);

        /// <summary>
        /// Describes the configuration settings of an Elastic Beanstalk environment
        /// </summary>
        /// <param name="applicationName">Application name</param>
        /// <param name="environmentName">Environment name</param>
        /// <returns>The configuration settings of an Elastic Beanstalk environment</returns>
        Task<List<ConfigurationSettingsDescription>> DescribeElasticBeanstalkConfigurationSettings(string applicationName, string environmentName);

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
        Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(string? targetFramework, params BeanstalkPlatformType[]? platformTypes);
        Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(string? targetFramework, BeanstalkPlatformType platformType);
        Task<List<AuthorizationData>> GetECRAuthorizationToken();
        Task<List<Repository>> GetECRRepositories(List<string>? repositoryNames = null);

        /// <summary>
        /// Creates a new ECR repository
        /// </summary>
        /// <param name="repositoryName">The name to use for the repository</param>
        /// <param name="recipeId">The id of the recipe that's being deployd, set as the value of the "aws-dotnet-deploy" tag</param>
        /// <returns>The repository that was created</returns>
        Task<Repository> CreateECRRepository(string repositoryName, string recipeId);

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
