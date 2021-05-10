// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using AWS.Deploy.Common;

namespace AWS.Deploy.Orchestration.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<Cluster>> ListOfECSClusters();
        Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications();
        Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(string? applicationName);
        Task<List<KeyPairInfo>> ListOfEC2KeyPairs();
        Task<string> CreateEC2KeyPair(string? keyName, string saveLocation);
        Task<List<Role>> ListOfIAMRoles(string? servicePrincipal);
        Task<List<Vpc>> GetListOfVpcs();
        Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns();
        Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn();
        Task<List<AuthorizationData>> GetECRAuthorizationToken();
        Task<List<Repository>> GetECRRepositories(List<string> repositoryNames);
        Task<Repository> CreateECRRepository(string repositoryName);
        Task<List<Stack>> GetCloudFormationStacks();
        Task<GetCallerIdentityResponse> GetCallerIdentity();
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
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

        public async Task<string> CreateEC2KeyPair(string? keyName, string saveLocation)
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
    }
}
