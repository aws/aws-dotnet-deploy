// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.AppRunner.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName);
        Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes();
        Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn);
        Task<List<StackResource>> DescribeCloudFormationResources(string stackName);
        Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName);
        Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn);
        Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn);
        Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName);
        Task<string> GetS3BucketLocation(string bucketName);
        Task<Amazon.S3.Model.WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName);
        Task<List<Cluster>> ListOfECSClusters();
        Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications();
        Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName = null);
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
        Task<GetCallerIdentityResponse> GetCallerIdentity(string awsRegion);
        Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(LoadBalancerTypeEnum loadBalancerType);
        Task<Distribution> GetCloudFrontDistribution(string distributionId);
        Task<List<string>> ListOfDyanmoDBTables();
        Task<List<string>> ListOfSQSQueuesUrls();
        Task<List<string>> ListOfSNSTopicArns();
        Task<List<Amazon.S3.Model.S3Bucket>> ListOfS3Buckets();
        Task<List<ConfigurationOptionSetting>> GetBeanstalkEnvironmentConfigurationSettings(string environmentName);
        Task<Repository> DescribeECRRepository(string respositoryName);
        Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors();
        Task<List<Subnet>> DescribeSubnets(string? vpcID = null);
        Task<List<SecurityGroup>> DescribeSecurityGroups(string? vpcID = null);
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        /// <summary>
        /// List the available subnets
        /// If <see cref="vpcID"/> is specified, the list of subnets is filtered by the VPC.
        /// </summary>
        public async Task<List<Subnet>> DescribeSubnets(string? vpcID = null)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            var request = new DescribeSubnetsRequest();
            if (vpcID != null)
                request.Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Name = "vpc-id",
                        Values = new List<string>
                        {
                            vpcID
                        }
                    }
                };

            if (string.IsNullOrEmpty(vpcID))
                return new List<Subnet>();

            return await HandleException(async () => await ec2Client.Paginators
                .DescribeSubnets(request)
                .Subnets
                .ToListAsync());
        }

        /// <summary>
        /// List the available security groups
        /// If <see cref="vpcID"/> is specified, the list of security groups is filtered by the VPC.
        /// </summary>
        public async Task<List<SecurityGroup>> DescribeSecurityGroups(string? vpcID = null)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();
            var request = new DescribeSecurityGroupsRequest();
            // If a subnets IDs list is not specified, all security groups wil be returned.
            if (vpcID != null)
                request.Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Name = "vpc-id",
                        Values = new List<string>
                        {
                            vpcID
                        }
                    }
                };

            if (string.IsNullOrEmpty(vpcID))
                return new List<SecurityGroup>();

            return await HandleException(async () => await ec2Client.Paginators
                .DescribeSecurityGroups(request)
                .SecurityGroups
                .ToListAsync());
        }

        public async Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName)
        {
            var cfClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            var listInstanceTypesPaginator = cfClient.Paginators.DescribeStackEvents(new DescribeStackEventsRequest
            {
                StackName = stackName
            });

            return await HandleException(async () => await listInstanceTypesPaginator.StackEvents.ToListAsync());
        }

        public async Task<List<InstanceTypeInfo>> ListOfAvailableInstanceTypes()
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();
            var instanceTypes = new List<InstanceTypeInfo>();
            var listInstanceTypesPaginator = ec2Client.Paginators.DescribeInstanceTypes(new DescribeInstanceTypesRequest());

            return await HandleException(async () =>
            {
                await foreach (var response in listInstanceTypesPaginator.Responses)
                {
                    instanceTypes.AddRange(response.InstanceTypes);
                }

                return instanceTypes;
            });
        }

        public async Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors()
        {
            var appRunnerClient = _awsClientFactory.GetAWSClient<Amazon.AppRunner.IAmazonAppRunner>();
            return await HandleException(async () =>
            {
                var connections = await appRunnerClient.ListVpcConnectorsAsync(new ListVpcConnectorsRequest());
                return connections.VpcConnectors;
            });
        }

        public async Task<Amazon.AppRunner.Model.Service> DescribeAppRunnerService(string serviceArn)
        {
            var appRunnerClient = _awsClientFactory.GetAWSClient<Amazon.AppRunner.IAmazonAppRunner>();

            return await HandleException(async () =>
            {
                var service = (await appRunnerClient.DescribeServiceAsync(new DescribeServiceRequest
                {
                    ServiceArn = serviceArn
                })).Service;

                if (service == null)
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.AppRunnerServiceDoesNotExist, $"The AppRunner service '{serviceArn}' does not exist.");
                }

                return service;
            });
        }

        public async Task<List<StackResource>> DescribeCloudFormationResources(string stackName)
        {
            var cfClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            return await HandleException(async () =>
            {
                var resources = await cfClient.DescribeStackResourcesAsync(new DescribeStackResourcesRequest { StackName = stackName });

                return resources.StackResources;
            });
        }

        public async Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            return await HandleException(async () =>
            {
                var environment = await beanstalkClient.DescribeEnvironmentsAsync(new DescribeEnvironmentsRequest {
                    EnvironmentNames = new List<string> { environmentName }
                });

                if (!environment.Environments.Any())
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.BeanstalkEnvironmentDoesNotExist, $"The elastic beanstalk environment '{environmentName}' does not exist.");
                }

                return environment.Environments.First();
            });
        }

        public async Task<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> DescribeElasticLoadBalancer(string loadBalancerArn)
        {
            var elasticLoadBalancingClient = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            return await HandleException(async () => {

                var loadBalancers = await elasticLoadBalancingClient.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest
                {
                    LoadBalancerArns = new List<string>
                    {
                        loadBalancerArn
                    }
                });

                if (!loadBalancers.LoadBalancers.Any())
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.LoadBalancerDoesNotExist, $"The load balancer '{loadBalancerArn}' does not exist.");
                }

                return loadBalancers.LoadBalancers.First();
            });
        }

        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.Listener>> DescribeElasticLoadBalancerListeners(string loadBalancerArn)
        {
            var elasticLoadBalancingClient = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();
            return await HandleException(async () =>
            {
                var listeners = await elasticLoadBalancingClient.DescribeListenersAsync(new DescribeListenersRequest
                {
                    LoadBalancerArn = loadBalancerArn
                });

                if (!listeners.Listeners.Any())
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.LoadBalancerListenerDoesNotExist, $"The load balancer '{loadBalancerArn}' does not have any listeners.");
                }

                return listeners.Listeners;
            });
        }

        public async Task<DescribeRuleResponse> DescribeCloudWatchRule(string ruleName)
        {
            var cloudWatchEventsClient = _awsClientFactory.GetAWSClient<IAmazonCloudWatchEvents>();
            return await HandleException(async () =>
            {
                var rule = await cloudWatchEventsClient.DescribeRuleAsync(new DescribeRuleRequest
                {
                    Name = ruleName
                });

                if (rule == null)
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.CloudWatchRuleDoesNotExist, $"The CloudWatch rule'{ruleName}' does not exist.");
                }

                return rule;
            });
        }

        public async Task<string> GetS3BucketLocation(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException($"The bucket name is null or empty.");
            }

            var s3Client = _awsClientFactory.GetAWSClient<IAmazonS3>();

            var location = await HandleException(async () => await s3Client.GetBucketLocationAsync(bucketName));

            var region = "";
            if (location.Location.Equals(S3Region.USEast1))
                region = "us-east-1";
            else if (location.Location.Equals(S3Region.EUWest1))
                region = "eu-west-1";
            else
                region = location.Location;

            return region;
        }

        public async Task<Amazon.S3.Model.WebsiteConfiguration> GetS3BucketWebSiteConfiguration(string bucketName)
        {
            var s3Client = _awsClientFactory.GetAWSClient<IAmazonS3>();

            var response = await HandleException(async () => await s3Client.GetBucketWebsiteAsync(bucketName));

            return response.WebsiteConfiguration;
        }

        public async Task<List<Cluster>> ListOfECSClusters()
        {
            var ecsClient = _awsClientFactory.GetAWSClient<IAmazonECS>();

            var clusters = await HandleException(async () =>
            {
                var clusterArns = await ecsClient.Paginators
                    .ListClusters(new ListClustersRequest())
                    .ClusterArns
                    .ToListAsync();

                return await ecsClient.DescribeClustersAsync(new DescribeClustersRequest
                {
                    Clusters = clusterArns
                });
            });

            return clusters.Clusters;
        }

        public async Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications()
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var applications = await HandleException(async () => await beanstalkClient.DescribeApplicationsAsync());
            return applications.Applications;
        }

        public async Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName = null)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            var request = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName
            };

            return await HandleException(async () =>
            {
                var environments = new List<EnvironmentDescription>();
                do
                {
                    var response = await beanstalkClient.DescribeEnvironmentsAsync(request);
                    request.NextToken = response.NextToken;

                    environments.AddRange(response.Environments);

                } while (!string.IsNullOrEmpty(request.NextToken));

                return environments;
            });
        }

        public async Task<List<Amazon.ElasticBeanstalk.Model.Tag>> ListElasticBeanstalkResourceTags(string resourceArn)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var response = await HandleException(async () => await beanstalkClient.ListTagsForResourceAsync(new Amazon.ElasticBeanstalk.Model.ListTagsForResourceRequest
            {
                ResourceArn = resourceArn
            }));

            return response.ResourceTags;
        }

        public async Task<List<KeyPairInfo>> ListOfEC2KeyPairs()
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();
            var response = await HandleException(async () => await ec2Client.DescribeKeyPairsAsync());

            return response.KeyPairs;
        }

        public async Task<string> CreateEC2KeyPair(string keyName, string saveLocation)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            var request = new CreateKeyPairRequest { KeyName = keyName };

            var response = await HandleException(async () => await ec2Client.CreateKeyPairAsync(request));

            await File.WriteAllTextAsync(Path.Combine(saveLocation, $"{keyName}.pem"), response.KeyPair.KeyMaterial);

            return response.KeyPair.KeyName;
        }

        public async Task<List<Role>> ListOfIAMRoles(string? servicePrincipal)
        {
            var identityManagementServiceClient = _awsClientFactory.GetAWSClient<IAmazonIdentityManagementService>();

            var listRolesRequest = new ListRolesRequest();

            var listStacksPaginator = identityManagementServiceClient.Paginators.ListRoles(listRolesRequest);
            return await HandleException(async () =>
            {
                var roles = new List<Role>();
                await foreach (var response in listStacksPaginator.Responses)
                {
                    var filteredRoles = response.Roles.Where(role => AssumeRoleServicePrincipalSelector(role, servicePrincipal));
                    roles.AddRange(filteredRoles);
                }

                return roles;
            });
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

            return await HandleException(async () => await vpcClient.Paginators
                .DescribeVpcs(new DescribeVpcsRequest())
                .Vpcs
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.VpcId)
                .ToListAsync());
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
            var response = await HandleException(async () => await beanstalkClient.ListPlatformVersionsAsync(request));

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
                throw new FailedToFindElasticBeanstalkSolutionStackException(DeployToolErrorCode.FailedToFindElasticBeanstalkSolutionStack, "Cannot use Elastic Beanstalk deployments because we cannot find a .NET Core Solution Stack to use. One possible reason could be that Elastic Beanstalk is not enabled in your region if you are using a non-default region.");
            }

            return platforms.First();
        }

        public async Task<List<AuthorizationData>> GetECRAuthorizationToken()
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var response = await HandleException(async () => await ecrClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest()));

            return response.AuthorizationData;
        }

        public async Task<List<Repository>> GetECRRepositories(List<string>? repositoryNames = null)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var request = new DescribeRepositoriesRequest
            {
                RepositoryNames = repositoryNames
            };

            return await HandleException(async () =>
            {
                try
                {
                    return (await ecrClient.Paginators.DescribeRepositories(request).Repositories.ToListAsync())
                        .OrderByDescending(x => x.CreatedAt)
                        .ToList();
                }
                catch (RepositoryNotFoundException)
                {
                    return new List<Repository>();
                }
            });
        }

        public async Task<Repository> CreateECRRepository(string repositoryName)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var request = new CreateRepositoryRequest
            {
                RepositoryName = repositoryName
            };

            var response = await HandleException(async () => await ecrClient.CreateRepositoryAsync(request));

            return response.Repository;
        }

        public async Task<List<Stack>> GetCloudFormationStacks()
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            return await HandleException(async () => await cloudFormationClient.Paginators
                .DescribeStacks(new DescribeStacksRequest())
                .Stacks.ToListAsync());
        }

        public async Task<GetCallerIdentityResponse> GetCallerIdentity(string awsRegion)
        {
            var request = new GetCallerIdentityRequest();
            using var stsClient = _awsClientFactory.GetAWSClient<IAmazonSecurityTokenService>(awsRegion);

            return await HandleException(async () =>
            {
                try
                {
                    return await stsClient.GetCallerIdentityAsync(request);
                }
                catch (Exception ex)
                {
                    var regionEndpointPartition = RegionEndpoint.GetBySystemName(awsRegion).PartitionName ?? String.Empty;
                    if (regionEndpointPartition.Equals("aws") && !awsRegion.Equals(Constants.CLI.DEFAULT_STS_AWS_REGION))
                    {
                        try
                        {
                            using var defaultRegionStsClient = _awsClientFactory.GetAWSClient<IAmazonSecurityTokenService>(Constants.CLI.DEFAULT_STS_AWS_REGION);
                            await defaultRegionStsClient.GetCallerIdentityAsync(request);
                        }
                        catch (Exception e)
                        {
                            throw new UnableToAccessAWSRegionException(
                                DeployToolErrorCode.UnableToAccessAWSRegion,
                                $"We were unable to access the AWS region '{awsRegion}'. Make sure you have correct permissions for that region and the region is accessible.",
                                e);
                        }

                        throw new UnableToAccessAWSRegionException(
                            DeployToolErrorCode.OptInRegionDisabled,
                            $"We were unable to access the Opt-In region '{awsRegion}'. Please enable the AWS Region '{awsRegion}' and try again. Additional details could be found at https://docs.aws.amazon.com/general/latest/gr/rande-manage.html",
                            ex);
                    }

                    throw new UnableToAccessAWSRegionException(
                        DeployToolErrorCode.UnableToAccessAWSRegion,
                        $"We were unable to access the AWS region '{awsRegion}'. Make sure you have correct permissions for that region and the region is accessible.",
                        ex);
                }
            });
        }

        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(Amazon.ElasticLoadBalancingV2.LoadBalancerTypeEnum loadBalancerType)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            return await HandleException(async () =>
            {
                return await client.Paginators.DescribeLoadBalancers(new DescribeLoadBalancersRequest())
                    .LoadBalancers.Where(loadBalancer => loadBalancer.Type == loadBalancerType)
                    .ToListAsync();
            });
        }

        public async Task<Distribution> GetCloudFrontDistribution(string distributionId)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonCloudFront>();

            return await HandleException(async () =>
            {
                var response = await client.GetDistributionAsync(new GetDistributionRequest
                {
                    Id = distributionId
                });

                return response.Distribution;
            });
        }

        public async Task<List<string>> ListOfDyanmoDBTables()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonDynamoDB>();
            return await HandleException(async () => await client.Paginators.ListTables(new ListTablesRequest()).TableNames.ToListAsync());
        }

        public async Task<List<string>> ListOfSQSQueuesUrls()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonSQS>();
            return await HandleException(async () => await client.Paginators.ListQueues(new ListQueuesRequest()).QueueUrls.ToListAsync());
        }

        public async Task<List<string>> ListOfSNSTopicArns()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonSimpleNotificationService>();
            return await HandleException(async () =>
            {
                return await client.Paginators.ListTopics(new ListTopicsRequest()).Topics.Select(topic => topic.TopicArn).ToListAsync();
            });
        }

        public async Task<List<Amazon.S3.Model.S3Bucket>> ListOfS3Buckets()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonS3>();
            return await HandleException(async () => (await client.ListBucketsAsync()).Buckets.ToList());
        }

        public async Task<List<ConfigurationOptionSetting>> GetBeanstalkEnvironmentConfigurationSettings(string environmentName)
        {
            var optionSetting = new List<ConfigurationOptionSetting>();
            var environmentDescription = await DescribeElasticBeanstalkEnvironment(environmentName);
            var client = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            return await HandleException(async () =>
            {
                var response = await client.DescribeConfigurationSettingsAsync(new DescribeConfigurationSettingsRequest
                {
                    ApplicationName = environmentDescription.ApplicationName,
                    EnvironmentName = environmentName
                });

                optionSetting.AddRange(response.ConfigurationSettings.SelectMany(settingDescription => settingDescription.OptionSettings));

                return optionSetting;
            });
        }

        public async Task<Repository> DescribeECRRepository(string respositoryName)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonECR>();
            return await HandleException(async () =>
            {

                DescribeRepositoriesResponse response;
                try
                {
                    response = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { respositoryName }
                    });
                }
                catch (RepositoryNotFoundException ex)
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.ECRRepositoryDoesNotExist, $"The ECR repository {respositoryName} does not exist.", ex);
                }

                return response.Repositories.First();
            });
        }

        private async Task<T> HandleException<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (AmazonServiceException e)
            {
                var messageBuilder = new StringBuilder();
                if (!string.IsNullOrEmpty(e.ErrorCode))
                {
                    messageBuilder.AppendLine($"{e.ErrorCode}");
                }

                if (!string.IsNullOrEmpty(e.Message))
                {
                    messageBuilder.AppendLine(e.Message);
                }

                if (messageBuilder.Length == 0)
                {
                    messageBuilder.Append($"An unknown error occurred while communicating with AWS.{Environment.NewLine}");
                }

                throw new ResourceQueryException(DeployToolErrorCode.ResourceQuery, messageBuilder.ToString(), e);
            }
        }
    }
}
