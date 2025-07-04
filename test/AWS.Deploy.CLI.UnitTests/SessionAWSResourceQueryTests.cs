// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using Amazon.ECR.Model;
using AWS.Deploy.CLI.ServerMode.Services;
using AWS.Deploy.Common.Data;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class SessionAWSResourceQueryTests
    {
        [Fact]
        public async Task MakeSureKeyPairCacheIsCleared()
        {
            var cachedKeys = new List<KeyPairInfo>();
            var awsResourceQueryMock = new Mock<IAWSResourceQueryer>();

            awsResourceQueryMock.Setup(x => x.ListOfEC2KeyPairs())
                                .ReturnsAsync(cachedKeys);

            awsResourceQueryMock.Setup(x => x.CreateEC2KeyPair(It.IsAny<string>(), It.IsAny<string>()))
                                .Callback<string, string>((keyPairName, location) =>
                                {
                                    cachedKeys.Add(new KeyPairInfo { KeyName = keyPairName });
                                });

            var sessionAwsResourceQueryMock = SessionAWSResourceQuery.Create(awsResourceQueryMock.Object);

            var keyPairs = await sessionAwsResourceQueryMock.ListOfEC2KeyPairs();
            Assert.Empty(keyPairs);

            await sessionAwsResourceQueryMock.CreateEC2KeyPair("test-keypair", "location");

            keyPairs = await sessionAwsResourceQueryMock.ListOfEC2KeyPairs();
            Assert.Single(keyPairs);
        }

        [Fact]
        public async Task MakeSureECRRepositoryCacheIsCleared()
        {
            var cachedECRRepositories = new List<Repository>();
            var awsResourceQueryMock = new Mock<IAWSResourceQueryer>();

            awsResourceQueryMock.Setup(x => x.GetECRRepositories(It.IsAny<List<string>>()))
                                .ReturnsAsync(cachedECRRepositories);

            awsResourceQueryMock.Setup(x => x.CreateECRRepository(It.IsAny<string>(), It.IsAny<string>()))
                                .Callback<string, string>((respositoryName, recipeId) =>
                                {
                                    cachedECRRepositories.Add(new Repository { RepositoryName = respositoryName });
                                });

            var sessionAwsResourceQueryMock = SessionAWSResourceQuery.Create(awsResourceQueryMock.Object);

            var repositories = await sessionAwsResourceQueryMock.GetECRRepositories();
            Assert.Empty(repositories);

            repositories = await sessionAwsResourceQueryMock.GetECRRepositories(new List<string> { "repo1" });
            Assert.Empty(repositories);

            repositories = await sessionAwsResourceQueryMock.GetECRRepositories(new List<string> { "repo1", "repo2" });
            Assert.Empty(repositories);

            await sessionAwsResourceQueryMock.CreateECRRepository("repo1", "recpieId");

            repositories = await sessionAwsResourceQueryMock.GetECRRepositories();
            Assert.Single(repositories);

            repositories = await sessionAwsResourceQueryMock.GetECRRepositories(new List<string> { "repo1" });
            Assert.Single(repositories);

            repositories = await sessionAwsResourceQueryMock.GetECRRepositories(new List<string> { "repo1", "repo2" });
            Assert.Single(repositories);
        }

        [Fact]
        public void TestCacheKey()
        {
            Assert.Equal("OperationName,[]", SessionAWSResourceQuery.CreateCacheKey(new object[] { }, "OperationName"));
            Assert.Equal("OperationName,[\"param1\"]", SessionAWSResourceQuery.CreateCacheKey(new object[] { "param1" }, "OperationName"));
            Assert.Equal("OperationName,[null]", SessionAWSResourceQuery.CreateCacheKey(new object[] { null! }, "OperationName"));
            Assert.Equal("OperationName,[[null]]", SessionAWSResourceQuery.CreateCacheKey(new object[] { new object[] { null! } }, "OperationName"));
            Assert.Equal("OperationName,[[\"listValue1\"]]", SessionAWSResourceQuery.CreateCacheKey(new object[] { new object[] { "listValue1" } }, "OperationName"));

            Assert.Equal("OperationName,[\"filterValue\",10]", SessionAWSResourceQuery.CreateCacheKey(new object[] { "filterValue", 10 }, "OperationName"));
        }
    }
}
