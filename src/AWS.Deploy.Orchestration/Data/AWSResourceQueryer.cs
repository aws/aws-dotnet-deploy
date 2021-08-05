// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration.Data
{
    public interface IAWSResourceQueryer
    {
        Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn);
        Task<List<StackResource>> DescribeCloudFormationResources(string stackName);
        Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentId);
        Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn);
        Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn);
        Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName);
        Task<string> GetS3BucketLocation(string bucketName);
        Task<List<Cluster>> ListOfECSClusters();
        Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications();
        Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName);
        Task<List<KeyPairInfo>> ListOfEC2KeyPairs();
        Task<string> CreateEC2KeyPair(string keyName, string saveLocation);
        Task<List<Role>> ListOfIAMRoles(string? servicePrincipal);
        Task<List<Vpc>> GetListOfVpcs();
        Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns();
        Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn();
        Task<List<AuthorizationData>> GetECRAuthorizationToken();
        Task<List<Repository>> GetECRRepositories(List<string> repositoryNames);
        Task<Repository> CreateECRRepository(string repositoryName);
        Task<List<Stack>> GetCloudFormationStacks();
        Task<GetCallerIdentityResponse> GetCallerIdentity();
        Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType);
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public async Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn)
        {
            var appRunnerClient = _awsClientFactory.GetAWSClient<Amazon.AppRunner.IAmazonAppRunner>();

            var service = (await appRunnerClient.DescribeServiceAsync(new Amazon.AppRunner.Model.DescribeServiceRequest
            {
                ServiceArn = serviceArn
            })).Service;

            if (service == null)
            {
                throw new AWSResourceNotFoundException($"The AppRunner service '{serviceArn}' does not exist.");
            }

            return service;
        }

        public async Task<List<StackResource>> DescribeCloudFormationResources(string stackName)
        {
            var cfClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            var resources = await cfClient.DescribeStackResourcesAsync(new DescribeStackResourcesRequest { StackName = stackName });

            return resources.StackResources;
        }

        public async Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentId)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            var environment = await beanstalkClient.DescribeEnvironmentsAsync(new DescribeEnvironmentsRequest {
                EnvironmentNames = new List<string> { environmentId }
            });

            if (!environment.Environments.Any())
            {
                throw new AWSResourceNotFoundException($"The elastic beanstalk environment '{environmentId}' does not exist.");
            }

            return environment.Environments.First();
        }

        public async Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn)
        {
            var elasticLoadBalancingClient = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            var loadBalancers = await elasticLoadBalancingClient.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest
            {
                LoadBalancerArns = new List<string> { loadBalancerArn }
            });

            if (!loadBalancers.LoadBalancers.Any())
            {
                throw new AWSResourceNotFoundException($"The load balancer '{loadBalancerArn}' does not exist.");
            }

            return loadBalancers.LoadBalancers.First();
        }

        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn)
        {
            var elasticLoadBalancingClient = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            var listeners = await elasticLoadBalancingClient.DescribeListenersAsync(new DescribeListenersRequest
            {
                LoadBalancerArn = loadBalancerArn
            });

            if (!listeners.Listeners.Any())
            {
                throw new AWSResourceNotFoundException($"The load balancer '{loadBalancerArn}' does not have any listeners.");
            }

            return listeners.Listeners;
        }

        public async Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName)
        {
            var cloudWatchEventsClient = _awsClientFactory.GetAWSClient<IAmazonCloudWatchEvents>();

            var rule = await cloudWatchEventsClient.DescribeRuleAsync(new DescribeRuleRequest
            {
                Name = ruleName
            });

            if (rule == null)
            {
                throw new AWSResourceNotFoundException($"The CloudWatch rule'{ruleName}' does not exist.");
            }

            return rule;
        }

        public async Task<string> GetS3BucketLocation(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException($"The bucket name is null or empty.");
            }

            var s3Client = _awsClientFactory.GetAWSClient<IAmazonS3>();

            var location = await s3Client.GetBucketLocationAsync(bucketName);

            var region = "";
            if (location.Location.Equals(S3Region.USEast1))
                region = "us-east-1";
            else if (location.Location.Equals(S3Region.EUWest1))
                region = "eu-west-1";
            else
                region = location.Location;

            return region;
        }

        public async Task<List<Cluster>> ListOfECSClusters()
        {
            var ecsClient = _awsClientFactory.GetAWSClient<IAmazonECS>();

            var clusterArns = await ecsClient.Paginators
                .ListClusters(new ListClustersRequest())
                .ClusterArns
                .ToListAsync();

            var clusters = await ecsClient.DescribeClustersAsync(new DescribeClustersRequest
            {
                Clusters = clusterArns
            });

            return clusters.Clusters;
        }

        public async Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications()
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var applications = await beanstalkClient.DescribeApplicationsAsync();
            return applications.Applications;
        }

        public async Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var environments = new List<EnvironmentDescription>();

            if (string.IsNullOrEmpty(applicationName))
                return environments;

            var request = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName
            };

            do
            {
                var response = await beanstalkClient.DescribeEnvironmentsAsync(request);
                request.NextToken = response.NextToken;

                environments.AddRange(response.Environments);

            } while (!string.IsNullOrEmpty(request.NextToken));

            return environments;
        }

        public async Task<List<KeyPairInfo>> ListOfEC2KeyPairs()
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();
            var response = await ec2Client.DescribeKeyPairsAsync();

            return response.KeyPairs;
        }

        public async Task<string> CreateEC2KeyPair(string keyName, string saveLocation)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            var request = new CreateKeyPairRequest { KeyName = keyName };

            var response = await ec2Client.CreateKeyPairAsync(request);

            File.WriteAllText(Path.Combine(saveLocation, $"{keyName}.pem"), response.KeyPair.KeyMaterial);

            return response.KeyPair.KeyName;
        }

        public async Task<List<Role>> ListOfIAMRoles(string? servicePrincipal)
        {
            var identityManagementServiceClient = _awsClientFactory.GetAWSClient<IAmazonIdentityManagementService>();

            var listRolesRequest = new ListRolesRequest();
            var roles = new List<Role>();

            var listStacksPaginator = identityManagementServiceClient.Paginators.ListRoles(listRolesRequest);
            await foreach (var response in listStacksPaginator.Responses)
            {
                var filteredRoles = response.Roles.Where(role => AssumeRoleServicePrincipalSelector(role, servicePrincipal));
                roles.AddRange(filteredRoles);
            }

            return roles;
        }

        private static bool AssumeRoleServicePrincipalSelector(Role role, string? servicePrincipal)
        {
            return !string.IsNullOrEmpty(role.AssumeRolePolicyDocument) &&
                   !string.IsNullOrEmpty(servicePrincipal) &&
                   role.AssumeRolePolicyDocument.Contains(servicePrincipal);
        }

        public async Task<List<Vpc>> GetListOfVpcs()
        {
            var vpcClient = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            return await vpcClient.Paginators
                .DescribeVpcs(new DescribeVpcsRequest())
                .Vpcs
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.VpcId)
                .ToListAsync();
        }

        public async Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns()
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            var request = new ListPlatformVersionsRequest
            {
                Filters = new List<PlatformFilter>
                {
                    new PlatformFilter
                    {
                        Operator = "=",
                        Type = "PlatformStatus",
                        Values = { "Ready" }
                    }
                }
            };
            var response = await beanstalkClient.ListPlatformVersionsAsync(request);

            var platformVersions = new List<PlatformSummary>();
            foreach (var version in response.PlatformSummaryList)
            {
                if (string.IsNullOrEmpty(version.PlatformCategory) || string.IsNullOrEmpty(version.PlatformBranchLifecycleState))
                    continue;

                if (!version.PlatformBranchLifecycleState.Equals("Supported"))
                    continue;

                if (!version.PlatformCategory.Equals(".NET Core"))
                    continue;

                platformVersions.Add(version);
            }

            return platformVersions;
        }

        public async Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn()
        {
            var platforms = await GetElasticBeanstalkPlatformArns();

            if (!platforms.Any())
            {
                throw new AmazonElasticBeanstalkException(".NET Core Solution Stack doesn't exist.");
            }

            return platforms.First();
        }

        public async Task<List<AuthorizationData>> GetECRAuthorizationToken()
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var response = await ecrClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());

            return response.AuthorizationData;
        }

        public async Task<List<Repository>> GetECRRepositories(List<string> repositoryNames)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var request = new DescribeRepositoriesRequest
            {
                RepositoryNames = repositoryNames
            };

            try
            {
                return await ecrClient.Paginators
                    .DescribeRepositories(request)
                    .Repositories
                    .ToListAsync();
            }
            catch (RepositoryNotFoundException)
            {
                return new List<Repository>();
            }
        }

        public async Task<Repository> CreateECRRepository(string repositoryName)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var request = new CreateRepositoryRequest
            {
                RepositoryName = repositoryName
            };

            var response = await ecrClient.CreateRepositoryAsync(request);

            return response.Repository;
        }

        public async Task<List<Stack>> GetCloudFormationStacks()
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>();
            return await cloudFormationClient.Paginators.DescribeStacks(new DescribeStacksRequest()).Stacks.ToListAsync();
        }

        public async Task<GetCallerIdentityResponse> GetCallerIdentity()
        {
            using var stsClient = _awsClientFactory.GetAWSClient<IAmazonSecurityTokenService>();
            return await stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
        }

        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(Amazon.ElasticLoadBalancingV2.LoadBalancerTypeEnum loadBalancerType)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            var loadBalancers = new List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();

            await foreach(var loadBalancer in client.Paginators.DescribeLoadBalancers(new DescribeLoadBalancersRequest()).LoadBalancers)
            {
                if(loadBalancer.Type == loadBalancerType)
                {
                    loadBalancers.Add(loadBalancer);
                }
            }

            return loadBalancers;
        }
    }
}
