// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.AppRunner.Model;
using Amazon.CloudControlApi.Model;
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
using AWS.Deploy.Common.Data;
using AWS.Deploy.Orchestration.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    /// <summary>
    /// This implementation of IAWSResourceQueryer wraps the normal AWSResourceQueryer implementation
    /// but caches the responses for each of the AWS service calls.
    ///
    /// The lifetime of this class is a session in server mode. Everytime users restart deployment
    /// a new instance of this class will be created with a new cache.
    /// </summary>
    public class SessionAWSResourceQuery : IAWSResourceQueryer
    {
        private readonly static JsonSerializerOptions _cacheKeyJsonOptions = new JsonSerializerOptions { WriteIndented = false };

        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly ConcurrentDictionary<string, object?> _cachedResponse = new();

        /// <summary>
        /// Construct an instance of SessionAWSResourceQuery. This will create an instance of AWSResourceQueryer
        /// using the services in the IServiceProvider.
        /// </summary>
        /// <param name="services"></param>

        public SessionAWSResourceQuery(IServiceProvider services)
        {
            _awsResourceQueryer = ActivatorUtilities.CreateInstance<AWSResourceQueryer>(services);
        }

        /// <summary>
        /// Construct an instance of SessionAWSResourceQuery wrapping the passed in IAWSResourceQueryer.
        /// This constructor is primary included for unit testing and injecting a Mock version of IAWSResourceQueryer.
        ///
        /// The factory Create method with a private constructor is used to avoid this constructor being attempted
        /// to be used in DI resolution.
        /// </summary>
        /// <param name="awsResourceQueryer"></param>
        private SessionAWSResourceQuery(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        /// <summary>
        /// Construct an instance of SessionAWSResourceQuery wrapping the passed in IAWSResourceQueryer.
        /// This method is primary included for unit testing and injecting a Mock version of IAWSResourceQueryer.
        /// </summary>
        /// <param name="awsResourceQueryer"></param>
        public static IAWSResourceQueryer Create(IAWSResourceQueryer awsResourceQueryer)
        {
            return new SessionAWSResourceQuery(awsResourceQueryer);
        }

        public static string CreateCacheKey(IEnumerable<object?>? args = null, [CallerMemberName] string caller = "")
        {
            var values = new List<object>
            {
                caller
            };
            if (args != null)
            {
                var argString = JsonSerializer.Serialize(args, typeof(IEnumerable<object?>), _cacheKeyJsonOptions);
                values.Add(argString);
            }

            var cacheKey = string.Join(",", values);
            return cacheKey;
        }

        private async Task<T?> GetAndCache<T>(Func<Task<T>> func, IEnumerable<object?>? args = null, [CallerMemberName] string caller = "")
        {
            var cacheKey = CreateCacheKey(args, caller);

            if (_cachedResponse.TryGetValue(cacheKey, out var cacheItem))
            {
                return (T?)cacheItem;
            }

            cacheItem = await func();
            _cachedResponse[cacheKey] = cacheItem;

            return (T?)cacheItem;
        }

        /// <inheritdoc/>
        public Task<string> CreateEC2KeyPair(string keyName, string saveLocation)
        {
            var cacheKey = CreateCacheKey(null, nameof(ListOfEC2KeyPairs));
            _cachedResponse.TryRemove(cacheKey, out _);

            return _awsResourceQueryer.CreateEC2KeyPair(keyName, saveLocation);
        }

        /// <inheritdoc/>
        public Task<Repository> CreateECRRepository(string repositoryName, string recipeId)
        {
            // Since GetECRRepositories takes in an optional list repository names to get we need to clear
            // all cached responses for GetECRRepositories.
            var cacheKeys = _cachedResponse.Keys.Where(x => x.StartsWith(nameof(GetECRRepositories)));
            foreach (var cacheKey in cacheKeys)
            {
                _cachedResponse.TryRemove(cacheKey, out _);
            }

            return _awsResourceQueryer.CreateECRRepository(repositoryName, recipeId);
        }

        /// <inheritdoc/>
        public async Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeAppRunnerService(serviceArn), new object[] { serviceArn }))!;
        }

        /// <inheritdoc/>
        public async Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeAppRunnerVpcConnectors()))!;
        }

        /// <inheritdoc/>
        public async Task<List<StackResource>> DescribeCloudFormationResources(string stackName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeCloudFormationResources(stackName), new object[] { stackName }))!;
        }

        /// <inheritdoc/>
        public async Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeCloudWatchRule(ruleName), new object[] { ruleName }))!;
        }

        /// <inheritdoc/>
        public async Task<Repository> DescribeECRRepository(string respositoryName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeECRRepository(respositoryName), new object[] { respositoryName }))!;
        }

        /// <inheritdoc/>
        public async Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeElasticBeanstalkEnvironment(environmentName), new object[] { environmentName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<ConfigurationSettingsDescription>> DescribeElasticBeanstalkConfigurationSettings(string applicationName, string environmentName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeElasticBeanstalkConfigurationSettings(applicationName, environmentName), new object[] { applicationName, environmentName }))!;
        }

        /// <inheritdoc/>
        public async Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeElasticLoadBalancer(loadBalancerArn), new object[] { loadBalancerArn }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeElasticLoadBalancerListeners(loadBalancerArn), new object[] { loadBalancerArn }))!;
        }

        /// <inheritdoc/>
        public async Task<InstanceTypeInfo?> DescribeInstanceType(string instanceType)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeInstanceType(instanceType), new object[] { instanceType }))!;
        }

        /// <inheritdoc/>
        public async Task<List<SecurityGroup>> DescribeSecurityGroups(string? vpcID = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeSecurityGroups(vpcID), new object?[] { vpcID }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Subnet>> DescribeSubnets(string? vpcID = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeSubnets(vpcID), new object?[] { vpcID }))!;
        }

        /// <inheritdoc/>
        public async Task<List<ConfigurationOptionSetting>> GetBeanstalkEnvironmentConfigurationSettings(string environmentName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetBeanstalkEnvironmentConfigurationSettings(environmentName), new object[] { environmentName }))!;
        }

        /// <inheritdoc/>
        public async Task<GetCallerIdentityResponse> GetCallerIdentity(string awsRegion)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCallerIdentity(awsRegion), new object[] { awsRegion }))!;
        }

        /// <inheritdoc/>
        public async Task<ResourceDescription> GetCloudControlApiResource(string type, string identifier)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCloudControlApiResource(type, identifier), new object[] { type, identifier }))!;
        }

        /// <inheritdoc/>
        public async Task<Stack?> GetCloudFormationStack(string stackName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCloudFormationStack(stackName), new object[] { stackName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCloudFormationStackEvents(stackName), new object[] { stackName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Stack>> GetCloudFormationStacks()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCloudFormationStacks()))!;
        }

        /// <inheritdoc/>
        public async Task<Distribution> GetCloudFrontDistribution(string distributionId)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetCloudFrontDistribution(distributionId), new object[] { distributionId }))!;
        }

        public async Task<List<Stack>> DescribeStacks(string stackName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DescribeStacks(stackName), new object[] { stackName }))!;
        }

        public async Task<DeleteStackResponse> DeleteStack(string stackName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.DeleteStack(stackName), new object[] { stackName }))!;
        }

        /// <inheritdoc/>
        public async Task<Vpc?> GetDefaultVpc()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetDefaultVpc()));
        }

        /// <inheritdoc/>
        public async Task<List<AuthorizationData>> GetECRAuthorizationToken()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetECRAuthorizationToken()))!;
        }

        /// <inheritdoc/>
        public async Task<List<Repository>> GetECRRepositories(List<string>? repositoryNames = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetECRRepositories(repositoryNames), new object?[] { repositoryNames }))!;
        }

        /// <inheritdoc/>
        public async Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(string? targetFramework, params BeanstalkPlatformType[]? platformTypes)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetElasticBeanstalkPlatformArns(targetFramework, platformTypes), new object?[] { platformTypes }))!;
        }

        /// <inheritdoc/>
        public async Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(string? targetFramework, BeanstalkPlatformType platformType)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn(targetFramework, platformType), new object[] { platformType }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Vpc>> GetListOfVpcs()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetListOfVpcs()))!;
        }

        /// <inheritdoc/>
        public async Task<string?> GetParameterStoreTextValue(string parameterName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetParameterStoreTextValue(parameterName), new object[] { parameterName }))!;
        }

        /// <inheritdoc/>
        public async Task<string> GetS3BucketLocation(string bucketName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetS3BucketLocation(bucketName), new object[] { bucketName }))!;
        }

        /// <inheritdoc/>
        public async Task<WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.GetS3BucketWebSiteConfiguration(bucketName), new object[] { bucketName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Amazon.ElasticBeanstalk.Model.Tag>> ListElasticBeanstalkResourceTags(string resourceArn)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListElasticBeanstalkResourceTags(resourceArn), new object[] { resourceArn }))!;
        }

        /// <inheritdoc/>
        public async Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfAvailableInstanceTypes()))!;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListOfDyanmoDBTables()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfDyanmoDBTables()))!;
        }

        /// <inheritdoc/>
        public async Task<List<KeyPairInfo>> ListOfEC2KeyPairs()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfEC2KeyPairs()))!;
        }

        /// <inheritdoc/>
        public async Task<List<Cluster>> ListOfECSClusters(string? ecsClusterName = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfECSClusters(ecsClusterName), new object?[] { ecsClusterName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(string? applicationName = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfElasticBeanstalkApplications(applicationName), new object?[] { applicationName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName = null, string? environmentName = null)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(applicationName, environmentName), new object?[] { applicationName, environmentName }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Role>> ListOfIAMRoles(string? servicePrincipal)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfIAMRoles(servicePrincipal), new object?[] { servicePrincipal }))!;
        }

        /// <inheritdoc/>
        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType)
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfLoadBalancers(loadBalancerType), new object[] { loadBalancerType }))!;
        }

        /// <inheritdoc/>
        public async Task<List<S3Bucket>> ListOfS3Buckets()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfS3Buckets()))!;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListOfSNSTopicArns()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfSNSTopicArns()))!;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListOfSQSQueuesUrls()
        {
            return (await GetAndCache(async () => await _awsResourceQueryer.ListOfSQSQueuesUrls()))!;
        }
    }
}
