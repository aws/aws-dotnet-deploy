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
using Amazon.CloudControlApi;
using Amazon.CloudControlApi.Model;
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
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Orchestration.Data
{
    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public async Task<ResourceDescription> GetCloudControlApiResource(string type, string identifier)
        {
            var cloudControlApiClient = _awsClientFactory.GetAWSClient<IAmazonCloudControlApi>();
            var request = new GetResourceRequest
            {
                TypeName = type,
                Identifier = identifier
            };

            return await HandleException(async () => {
                    var resource = await cloudControlApiClient.GetResourceAsync(request);
                    return resource.ResourceDescription;
                },
                $"Error attempting to retrieve Cloud Control API resource '{identifier}'");
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
            {
                if (string.IsNullOrEmpty(vpcID))
                    return new List<Subnet>();

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
            }

            return await HandleException(async () => await ec2Client.Paginators
                .DescribeSubnets(request)
                .Subnets
                .ToListAsync(),
                "Error attempting to describe available subnets");
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
            {
                if (string.IsNullOrEmpty(vpcID))
                    return new List<SecurityGroup>();

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
            }

            return await HandleException(async () => await ec2Client.Paginators
                .DescribeSecurityGroups(request)
                .SecurityGroups
                .ToListAsync(),
                "Error attempting to describe available security groups");
        }

        public async Task<List<StackEvent>> GetCloudFormationStackEvents(string stackName)
        {
            var cfClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            var request = new DescribeStackEventsRequest
            {
                StackName = stackName
            };

            return await HandleException(async () => await cfClient.Paginators
                .DescribeStackEvents(request)
                .StackEvents
                .ToListAsync(),
                $"Error attempting to describe available CloudFormation stack events of '{stackName}'");
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
            },
            "Error attempting to describe available instance types");
        }

        public async Task<InstanceTypeInfo?> DescribeInstanceType(string instanceType)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            var request = new DescribeInstanceTypesRequest
            {
                InstanceTypes = new List<string> { instanceType }
            };

            return await HandleException(async () =>
            {
                var response = await ec2Client.DescribeInstanceTypesAsync(request);
                return response.InstanceTypes.FirstOrDefault();
            },
            $"Error attempting to describe instance type '{instanceType}'");
        }

        public async Task<List<VpcConnector>> DescribeAppRunnerVpcConnectors()
        {
            var appRunnerClient = _awsClientFactory.GetAWSClient<Amazon.AppRunner.IAmazonAppRunner>();
            return await HandleException(async () =>
            {
                var connections = await appRunnerClient.ListVpcConnectorsAsync(new ListVpcConnectorsRequest());
                return connections.VpcConnectors;
            },
            "Error attempting to describe available App Runner VPC connectors");
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
            },
            $"Error attempting to describe App Runner service '{serviceArn}'");
        }

        public async Task<List<StackResource>> DescribeCloudFormationResources(string stackName)
        {
            var cfClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            return await HandleException(async () =>
            {
                var resources = await cfClient.DescribeStackResourcesAsync(new DescribeStackResourcesRequest { StackName = stackName });

                return resources.StackResources;
            },
            $"Error attempting to describe CloudFormation resources of '{stackName}'");
        }

        public async Task<EnvironmentDescription> DescribeElasticBeanstalkEnvironment(string environmentName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            return await HandleException(async () =>
            {
                var environment = await beanstalkClient.DescribeEnvironmentsAsync(new DescribeEnvironmentsRequest
                {
                    EnvironmentNames = new List<string> { environmentName }
                });

                if (!environment.Environments.Any())
                {
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.BeanstalkEnvironmentDoesNotExist, $"The elastic beanstalk environment '{environmentName}' does not exist.");
                }

                return environment.Environments.First();
            },
            $"Error attempting to describe Elastic Beanstalk environment '{environmentName}'");
        }

        public async Task<List<ConfigurationSettingsDescription>> DescribeElasticBeanstalkConfigurationSettings(string applicationName, string environmentName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            return await HandleException(async () =>
            {
                var environment = await beanstalkClient.DescribeConfigurationSettingsAsync(new DescribeConfigurationSettingsRequest
                {
                    ApplicationName = applicationName,
                    EnvironmentName = environmentName
                });

                return environment.ConfigurationSettings;
            },
            $"Error attempting to describe Elastic Beanstalk environment '{environmentName}'");
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
            },
            $"Error attempting to describe Elastic Load Balancer '{loadBalancerArn}'");
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
            },
            $"Error attempting to describe Elastic Load Balancer listeners of '{loadBalancerArn}'");
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
            },
            $"Error attempting to describe CloudWatch rule '{ruleName}'");
        }

        public async Task<string> GetS3BucketLocation(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException($"The bucket name is null or empty.");
            }

            var s3Client = _awsClientFactory.GetAWSClient<IAmazonS3>();

            var location = await HandleException(async () => await s3Client.GetBucketLocationAsync(bucketName),
            $"Error attempting to retrieve S3 bucket location of '{bucketName}'");

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

            var response = await HandleException(async () => await s3Client.GetBucketWebsiteAsync(bucketName),
            $"Error attempting to retrieve S3 bucket website configuration of '{bucketName}'");

            return response.WebsiteConfiguration;
        }

        public async Task<List<Cluster>> ListOfECSClusters(string? ecsClusterName = null)
        {
            var ecsClient = _awsClientFactory.GetAWSClient<IAmazonECS>();

            var clusters = await HandleException(async () =>
            {
                var request = new DescribeClustersRequest();
                if (string.IsNullOrEmpty(ecsClusterName))
                {
                    var clusterArns = await ecsClient.Paginators
                        .ListClusters(new ListClustersRequest())
                        .ClusterArns
                        .ToListAsync();

                    request.Clusters = clusterArns;
                }
                else
                {
                    request.Clusters = new List<string> { ecsClusterName };
                }


                return await ecsClient.DescribeClustersAsync(request);
            },
            "Error attempting to list available ECS clusters");

            return clusters.Clusters;
        }

        public async Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(string? applicationName = null)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var request = new DescribeApplicationsRequest();
            if (!string.IsNullOrEmpty(applicationName))
                request.ApplicationNames = new List<string> { applicationName };

            var applications = await HandleException(async () => await beanstalkClient.DescribeApplicationsAsync(request),
            "Error attempting to list available Elastic Beanstalk applications");
            return applications.Applications;
        }

        public async Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName = null, string? environmentName = null)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            var request = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName
            };

            if (!string.IsNullOrEmpty(environmentName))
                request.EnvironmentNames = new List<string> { environmentName };

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
            },
            "Error attempting to list available Elastic Beanstalk environments");
        }

        public async Task<List<Amazon.ElasticBeanstalk.Model.Tag>> ListElasticBeanstalkResourceTags(string resourceArn)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var response = await HandleException(async () => await beanstalkClient.ListTagsForResourceAsync(new Amazon.ElasticBeanstalk.Model.ListTagsForResourceRequest
            {
                ResourceArn = resourceArn
            }),
            $"Error attempting to list available Elastic Beanstalk resource tags of '{resourceArn}'");

            return response.ResourceTags;
        }

        public async Task<List<KeyPairInfo>> ListOfEC2KeyPairs()
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();
            var response = await HandleException(async () => await ec2Client.DescribeKeyPairsAsync(),
            "Error attempting to list available EC2 key pairs");

            return response.KeyPairs;
        }

        public async Task<string> CreateEC2KeyPair(string keyName, string saveLocation)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            var request = new CreateKeyPairRequest { KeyName = keyName };

            var response = await HandleException(async () => await ec2Client.CreateKeyPairAsync(request),
            "Error attempting to create EC2 key pair");

            // We're creating the key pair at a user-defined location, and want to support relative paths
            // nosemgrep: csharp.lang.security.filesystem.unsafe-path-combine.unsafe-path-combine
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
            },
            "Error attempting to list available IAM roles");
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
                .ToListAsync(),
                "Error attempting to describe available VPCs");
        }

        public async Task<Vpc?> GetDefaultVpc()
        {
            var vpcClient = _awsClientFactory.GetAWSClient<IAmazonEC2>();

            return await HandleException(async () => await vpcClient.Paginators
                .DescribeVpcs(
                    new DescribeVpcsRequest
                    {
                        Filters = new List<Filter> {
                            new Filter {
                                Name = "is-default",
                                Values = new List<string> { "true" }
                            }
                        }
                    })
                .Vpcs.FirstOrDefaultAsync(),
                "Error attempting to retrieve the default VPC");
        }

        public async Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(string? targetFramework, params BeanstalkPlatformType[]? platformTypes)
        {
            if(platformTypes == null || platformTypes.Length == 0)
            {
                platformTypes = new BeanstalkPlatformType[] { BeanstalkPlatformType.Linux, BeanstalkPlatformType.Windows };
            }
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            Func<string, Task<List<PlatformSummary>>> fetchPlatforms = async (platformName) =>
            {
                var request = new ListPlatformVersionsRequest
                {
                    Filters = new List<PlatformFilter>
                    {
                        new PlatformFilter
                        {
                            Operator = "=",
                            Type = "PlatformStatus",
                            Values = { "Ready" }
                            },
                            new PlatformFilter
                            {
                                Operator = "contains",
                                Type = "PlatformName",
                                Values = { platformName }
                        }
                    }
                };

                var platforms = await HandleException(async () => (await beanstalkClient.Paginators.ListPlatformVersions(request).PlatformSummaryList.ToListAsync()),
                "Error attempting to list available Elastic Beanstalk platform versions");

                // Filter out old test platforms that only internal accounts would be able to see.
                platforms = platforms.Where(x => !string.IsNullOrEmpty(x.PlatformBranchName)).ToList();

                return platforms;
            };

            var allPlatformSummaries = new List<PlatformSummary>();
            if (platformTypes.Contains(BeanstalkPlatformType.Linux))
            {
                var linuxPlatforms = await fetchPlatforms(Constants.ElasticBeanstalk.LinuxPlatformType);
                linuxPlatforms = SortElasticBeanstalkLinuxPlatforms(targetFramework, linuxPlatforms);
                allPlatformSummaries.AddRange(linuxPlatforms);
            }
            if (platformTypes.Contains(BeanstalkPlatformType.Windows))
            {
                var windowsPlatforms = await fetchPlatforms(Constants.ElasticBeanstalk.WindowsPlatformType);
                SortElasticBeanstalkWindowsPlatforms(windowsPlatforms);
                allPlatformSummaries.AddRange(windowsPlatforms);
            }

            var platformVersions = new List<PlatformSummary>();
            foreach (var version in allPlatformSummaries)
            {
                if (string.IsNullOrEmpty(version.PlatformCategory) || string.IsNullOrEmpty(version.PlatformBranchLifecycleState))
                    continue;

                if (!(version.PlatformBranchLifecycleState.Equals("Supported") || version.PlatformBranchLifecycleState.Equals("Deprecated")))
                    continue;

                platformVersions.Add(version);
            }

            return platformVersions;
        }

        public async Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(string? targetFramework, BeanstalkPlatformType platformType)
        {
            var platforms = await GetElasticBeanstalkPlatformArns(targetFramework, platformType);

            if (!platforms.Any())
            {
                throw new FailedToFindElasticBeanstalkSolutionStackException(DeployToolErrorCode.FailedToFindElasticBeanstalkSolutionStack,
                    "Cannot use Elastic Beanstalk deployments because we cannot find a .NET Core Solution Stack to use. " +
                    "Possible reasons could be that Elastic Beanstalk is not enabled in your region if you are using a non-default region, " +
                    "or that the configured credentials lack permission to call ListPlatformVersions.");
            }

            return platforms.First();
        }

        /// <summary>
        /// For Linux beanstalk platforms the describe calls return a collection of .NET x and .NET Core based platforms.
        /// The order returned will be sorted by .NET version in increasing order then by platform versions. So for example we could get a result like the following
        ///
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.2
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.6
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.5
        /// .NET 8 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET Core running on 64bit Amazon Linux 2 v2.8.0
        /// .NET Core running on 64bit Amazon Linux 2 v2.7.3
        /// .NET Core running on 64bit Amazon Linux 2 v2.6.0
        ///
        /// We want the user to see the .NET version corresponding to their application on the latest Beanstalk platform first.
        /// If the user is trying to deploy a .NET 6 application, the above example will be sorted into the following.
        ///
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET 8 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.2
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.6
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.5
        /// .NET Core running on 64bit Amazon Linux 2 v2.8.0
        /// .NET Core running on 64bit Amazon Linux 2 v2.7.3
        /// .NET Core running on 64bit Amazon Linux 2 v2.6.0
        ///
        /// In case the target framework is not known in advance, the platforms will be sorted by Beanstalk Platform followed by the .NET version, with .NET Core coming in last.
        /// The above example will be sorted into the following.
        ///
        /// .NET 8 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.3
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.1.2
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.6
        /// .NET 6 running on 64bit Amazon Linux 2023 v3.0.5
        /// .NET Core running on 64bit Amazon Linux 2 v2.8.0
        /// .NET Core running on 64bit Amazon Linux 2 v2.7.3
        /// .NET Core running on 64bit Amazon Linux 2 v2.6.0
        /// </summary>
        /// <param name="platforms"></param>
        /// <param name="targetFramework"></param>
        public static List<PlatformSummary> SortElasticBeanstalkLinuxPlatforms(string? targetFramework, List<PlatformSummary> platforms)
        {
            var dotnetVersionMap = new System.Collections.Generic.Dictionary<string, decimal>();
            foreach (var platform in platforms)
            {
                var runningIndexOf = platform.PlatformBranchName.IndexOf("running", StringComparison.InvariantCultureIgnoreCase);
                if (runningIndexOf == -1)
                {
                    dotnetVersionMap[platform.PlatformArn] = 0;
                    continue;
                }

                var framework = platform.PlatformBranchName.Substring(0, runningIndexOf).Trim();
                var frameworkSplit = framework.Split(" ");
                if (frameworkSplit.Length != 2)
                {
                    dotnetVersionMap[platform.PlatformArn] = 0;
                    continue;
                }

                if (!decimal.TryParse(frameworkSplit[1], out var dotnetVersion))
                {
                    dotnetVersionMap[platform.PlatformArn] = 0;
                    continue;
                }

                if (decimal.TryParse(targetFramework?.Replace("net", ""), out var currentTargetFramework))
                {
                    if (currentTargetFramework.Equals(dotnetVersion))
                    {
                        dotnetVersionMap[platform.PlatformArn] = 1000;
                        continue;
                    }
                }

                dotnetVersionMap[platform.PlatformArn] = dotnetVersion;
            }

            return platforms.OrderByDescending(x => new Version(x.PlatformVersion)).ThenByDescending(x => dotnetVersionMap[x.PlatformArn]).ToList();
        }

        /// <summary>
        /// For Windows beanstalk platforms the describe calls return a collection of Windows Server Code and Windows Server based platforms.
        /// The order return will be sorted by platform versions but not OS. So for example we could get a result like the following
        ///
        /// IIS 10.0 running on 64bit Windows Server 2016 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server 2016 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2016 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2016 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server 2019 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server 2019 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2019 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2019 (1.0.0)
        ///
        /// We want the user to use the latest version of each OS first as well as the latest version of Windows first. Also Windows Server should come before Windows Server Core.
        /// This matches the behavior of the existing VS toolkit picker. The above example will be sorted into the following.
        ///
        /// IIS 10.0 running on 64bit Windows Server 2019 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2019 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server 2016 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2016 (1.1.0)
        /// IIS 10.0 running on 64bit Windows Server 2019 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2019 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server 2016 (1.0.0)
        /// IIS 10.0 running on 64bit Windows Server Core 2016 (1.0.0)
        /// </summary>
        /// <param name="windowsPlatforms"></param>
        public static void SortElasticBeanstalkWindowsPlatforms(List<PlatformSummary> windowsPlatforms)
        {
            var parseYear = (string name) =>
            {
                var tokens = name.Split(' ');
                int year;
                if (int.TryParse(tokens[tokens.Length - 1], out year))
                    return year;
                if (int.TryParse(tokens[tokens.Length - 2], out year))
                    return year;

                return 0;
            };
            var parseOSLevel = (string name) =>
            {
                if (name.Contains("Windows Server Core"))
                    return 1;
                if (name.Contains("Windows Server"))
                    return 2;

                return 10;
            };

            windowsPlatforms.Sort((x, y) =>
            {
                if (!Version.TryParse(x.PlatformVersion, out var xVersion))
                    xVersion = Version.Parse("0.0.0");

                if (!Version.TryParse(y.PlatformVersion, out var yVersion))
                    yVersion = Version.Parse("0.0.0");

                if (yVersion != xVersion)
                {
                    return yVersion.CompareTo(xVersion);
                }

                var xYear = parseYear(x.PlatformBranchName);
                var yYear = parseYear(y.PlatformBranchName);
                var xOSLevel = parseOSLevel(x.PlatformBranchName);
                var yOSLevel = parseOSLevel(y.PlatformBranchName);

                if (yYear == xYear)
                {
                    if (yOSLevel == xOSLevel)
                    {
                        return 0;
                    }

                    return yOSLevel < xOSLevel ? -1 : 1;
                }

                return yYear < xYear ? -1 : 1;
            });
        }

        public async Task<List<AuthorizationData>> GetECRAuthorizationToken()
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var response = await HandleException(async () => await ecrClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest()),
            "Error attempting to retrieve ECR authorization token");

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
            },
            "Error attempting to list available ECR repositories");
        }

        public async Task<Repository> CreateECRRepository(string repositoryName, string recipeId)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>();

            var request = new CreateRepositoryRequest
            {
                RepositoryName = repositoryName
            };

            // If a recipe ID was provided, set it as the tag value like we do for the CloudFormation stack
            if (!string.IsNullOrEmpty(recipeId))
            {
                // Ensure it fits in the maximum length for ECR
                var tagValue = recipeId.Length > 256 ? recipeId.Substring(0,256) : recipeId;

                request.Tags = new List<Amazon.ECR.Model.Tag>
                {
                    new Amazon.ECR.Model.Tag
                    {
                        Key = Constants.CloudFormationIdentifier.STACK_TAG,
                        Value = tagValue
                   }
                };
            }

            var response = await HandleException(async () => await ecrClient.CreateRepositoryAsync(request),
            "Error attempting to create an ECR repository");

            return response.Repository;
        }

        public async Task<List<Stack>> GetCloudFormationStacks()
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            return await HandleException(async () => await cloudFormationClient.Paginators
                .DescribeStacks(new DescribeStacksRequest())
                .Stacks.ToListAsync(),
                "Error attempting to describe available CloudFormation stacks");
        }

        public async Task<Stack?> GetCloudFormationStack(string stackName)
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<IAmazonCloudFormation>();
            return await HandleException(async () =>
            {
                try
                {
                    var request = new DescribeStacksRequest { StackName = stackName };
                    var response = await cloudFormationClient.DescribeStacksAsync(request);
                    return response.Stacks.FirstOrDefault();
                }
                // CloudFormation throws a BadRequest exception if the stack does not exist
                catch (AmazonCloudFormationException e) when (e.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return null;
                }
            },
            $"Error attempting to retrieve the CloudFormation stack '{stackName}'");
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
            },
            "Error attempting to retrieve the STS caller identity");
        }

        public async Task<List<Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>> ListOfLoadBalancers(Amazon.ElasticLoadBalancingV2.LoadBalancerTypeEnum loadBalancerType)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonElasticLoadBalancingV2>();

            return await HandleException(async () =>
            {
                return await client.Paginators.DescribeLoadBalancers(new DescribeLoadBalancersRequest())
                    .LoadBalancers.Where(loadBalancer => loadBalancer.Type == loadBalancerType)
                    .ToListAsync();
            },
            "Error attempting to list available Elastic load balancers");
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
            },
            $"Error attempting to retrieve the CloudFront distribution '{distributionId}'");
        }

        public async Task<List<string>> ListOfDyanmoDBTables()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonDynamoDB>();
            return await HandleException(async () => await client.Paginators.ListTables(new ListTablesRequest()).TableNames.ToListAsync(),
            "Error attempting to list available DynamoDB tables");
        }

        public async Task<List<string>> ListOfSQSQueuesUrls()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonSQS>();
            return await HandleException(async () => await client.Paginators.ListQueues(new ListQueuesRequest()).QueueUrls.ToListAsync(),
            "Error attempting to list available SQS queue URLs");
        }

        public async Task<List<string>> ListOfSNSTopicArns()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonSimpleNotificationService>();
            return await HandleException(async () =>
            {
                return await client.Paginators.ListTopics(new ListTopicsRequest()).Topics.Select(topic => topic.TopicArn).ToListAsync();
            },
            "Error attempting to list available SNS topics");
        }

        public async Task<List<Amazon.S3.Model.S3Bucket>> ListOfS3Buckets()
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonS3>();
            return await HandleException(async () => (await client.ListBucketsAsync()).Buckets.ToList(),
            "Error attempting to list available S3 buckets");
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
            },
            $"Error attempting to retrieve Elastic Beanstalk environment configuration settings for '{environmentName}'");
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
            },
            $"Error attempting to describe ECR repository '{respositoryName}'");
        }

        public async Task<string?> GetParameterStoreTextValue(string parameterName)
        {
            var client = _awsClientFactory.GetAWSClient<IAmazonSimpleSystemsManagement>();
            return await HandleException(async () =>
            {
                try
                {
                    var request = new GetParameterRequest { Name = parameterName };
                    var response = await client.GetParameterAsync(request);
                    return response.Parameter.Value;
                }
                catch (ParameterNotFoundException)
                {
                    return null;
                }
            },
            $"Error attempting to retrieve SSM parameter store value of '{parameterName}'");
        }

        private async Task<T> HandleException<T>(Func<Task<T>> action, string exceptionMessage)
        {
            try
            {
                return await action();
            }
            catch (AmazonServiceException e)
            {
                var messageBuilder = new StringBuilder();

                var userMessage = string.Empty;
                if (!string.IsNullOrEmpty(exceptionMessage))
                {
                    if (!string.IsNullOrEmpty(e.ErrorCode))
                    {
                        userMessage += $"{exceptionMessage} ({e.ErrorCode}).";
                    }
                    else
                    {
                        userMessage += $"{exceptionMessage}.";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(e.ErrorCode))
                    {
                        userMessage += e.ErrorCode;
                    }
                }

                if (!string.IsNullOrEmpty(userMessage))
                {
                    messageBuilder.AppendLine(userMessage);
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
