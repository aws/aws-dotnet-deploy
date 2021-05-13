// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.ECR;
using Amazon.ECR.Model;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class ECRHelper
    {
        private readonly IAmazonECR _ecrClient;

        public ECRHelper(IAmazonECR ecrClient)
        {
            _ecrClient = ecrClient;
        }

        public async Task<bool> IsRepositoryDeleted(string repositoryName)
        {
            var repositories = await GetRepositories(new List<string>
            {
                repositoryName
            });

            foreach (var repository in repositories)
            {
                var tags = await ListTagsForResource(repository.RepositoryArn);
                if (tags.Any(tag => tag.Key.Equals(CloudFormationIdentifierConstants.STACK_TAG)))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<List<Repository>> GetRepositories(List<string> repositoryNames)
        {
            var request = new DescribeRepositoriesRequest
            {
                RepositoryNames = repositoryNames
            };

            try
            {
                return await _ecrClient.Paginators
                    .DescribeRepositories(request)
                    .Repositories
                    .ToListAsync();
            }
            catch (RepositoryNotFoundException)
            {
                return new List<Repository>();
            }
        }

        public async Task<List<Tag>> ListTagsForResource(string resourceArn)
        {
            var request = new ListTagsForResourceRequest
            {
                ResourceArn = resourceArn
            };

            var response = await _ecrClient.ListTagsForResourceAsync(request);

            return response.Tags;
        }

        public async Task DeleteRepository(string repositoryName)
        {
            var request = new DeleteRepositoryRequest
            {
                RepositoryName = repositoryName,
                Force = true
            };

            try
            {
                await _ecrClient.DeleteRepositoryAsync(request);
            }
            catch (RepositoryNotFoundException)
            {
                // Repository does not exit, no action required.
            }
        }
    }
}
