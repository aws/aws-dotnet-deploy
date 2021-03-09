// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Amazon.CloudFormation.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ECR;
using AWS.Deploy.Common;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.ECR.Model;

namespace AWS.Deploy.Orchestrator.Data
{
    public interface IAWSResourceQueryer
    {
        Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(OrchestratorSession session);
        Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName);
        Task<List<KeyPairInfo>> ListOfEC2KeyPairs(OrchestratorSession session);
        Task<string> CreateEC2KeyPair(OrchestratorSession session, string keyName, string saveLocation);
        Task<List<Role>> ListOfIAMRoles(OrchestratorSession session, string servicePrincipal);
        Task<List<Vpc>> GetListOfVpcs(OrchestratorSession session);
        Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(OrchestratorSession session);
        Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(OrchestratorSession session);
        Task<List<AuthorizationData>> GetECRAuthorizationToken(OrchestratorSession session);
        Task<List<Repository>> GetECRRepositories(OrchestratorSession session, List<string> repositoryNames);
        Task<Repository> CreateECRRepository(OrchestratorSession session, string repositoryName);
        Task<List<Stack>> GetCloudFormationStacks(OrchestratorSession session);
    }

    public class AWSResourceQueryer : IAWSResourceQueryer
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public AWSResourceQueryer(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public async Task<List<ApplicationDescription>> ListOfElasticBeanstalkApplications(OrchestratorSession session)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(session.AWSCredentials, session.AWSRegion);
            var applications = await beanstalkClient.DescribeApplicationsAsync();
            return applications.Applications;
        }

        public async Task<List<EnvironmentDescription>> ListOfElasticBeanstalkEnvironments(OrchestratorSession session, string applicationName)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(session.AWSCredentials, session.AWSRegion);
            var environments = new List<EnvironmentDescription>();
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

        public async Task<List<KeyPairInfo>> ListOfEC2KeyPairs(OrchestratorSession session)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);
            var response = await ec2Client.DescribeKeyPairsAsync();

            return response.KeyPairs;
        }

        public async Task<string> CreateEC2KeyPair(OrchestratorSession session, string keyName, string saveLocation)
        {
            var ec2Client = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);

            var request = new CreateKeyPairRequest() { KeyName = keyName };

            var response = await ec2Client.CreateKeyPairAsync(request);

            File.WriteAllText(Path.Combine(saveLocation, $"{keyName}.pem"), response.KeyPair.KeyMaterial);

            return response.KeyPair.KeyName;
        }

        public async Task<List<Role>> ListOfIAMRoles(OrchestratorSession session, string servicePrincipal)
        {
            var identityManagementServiceClient = _awsClientFactory.GetAWSClient<IAmazonIdentityManagementService>(session.AWSCredentials, session.AWSRegion);

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

        private static bool AssumeRoleServicePrincipalSelector(Role role, string servicePrincipal)
        {
            return !string.IsNullOrEmpty(role.AssumeRolePolicyDocument) && role.AssumeRolePolicyDocument.Contains(servicePrincipal);
        }

        public async Task<List<Vpc>> GetListOfVpcs(OrchestratorSession session)
        {
            var vpcClient = _awsClientFactory.GetAWSClient<IAmazonEC2>(session.AWSCredentials, session.AWSRegion);

            return await vpcClient.Paginators
                .DescribeVpcs(new DescribeVpcsRequest())
                .Vpcs
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.VpcId)
                .ToListAsync();
        }

        public async Task<List<PlatformSummary>> GetElasticBeanstalkPlatformArns(OrchestratorSession session)
        {
            var beanstalkClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>(session.AWSCredentials, session.AWSRegion);
            
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

        public async Task<PlatformSummary> GetLatestElasticBeanstalkPlatformArn(OrchestratorSession session)
        {
            var platforms = await GetElasticBeanstalkPlatformArns(session);

            if (!platforms.Any())
            {
                throw new AmazonElasticBeanstalkException(".NET Core Solution Stack doesn't exist.");
            }

            return platforms.First();
        }

        public async Task<List<AuthorizationData>> GetECRAuthorizationToken(OrchestratorSession session)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>(session.AWSCredentials, session.AWSRegion);

            var response = await ecrClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());

            return response.AuthorizationData;
        }

        public async Task<List<Repository>> GetECRRepositories(OrchestratorSession session, List<string> repositoryNames)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>(session.AWSCredentials, session.AWSRegion);

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

        public async Task<Repository> CreateECRRepository(OrchestratorSession session, string repositoryName)
        {
            var ecrClient = _awsClientFactory.GetAWSClient<IAmazonECR>(session.AWSCredentials, session.AWSRegion);

            var request = new CreateRepositoryRequest
            {
                RepositoryName = repositoryName
            };

            var response = await ecrClient.CreateRepositoryAsync(request);

            return response.Repository;
        }

        public async Task<List<Stack>> GetCloudFormationStacks(OrchestratorSession session)
        {
            using var cloudFormationClient = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>(session.AWSCredentials, session.AWSRegion);
            return await cloudFormationClient.Paginators.DescribeStacks(new DescribeStacksRequest()).Stacks.ToListAsync();
        }
    }
}
