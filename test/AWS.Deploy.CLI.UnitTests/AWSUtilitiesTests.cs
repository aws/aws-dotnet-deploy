// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class AWSUtilitiesTests : IDisposable
    {
        private readonly IDirectoryManager _directoryManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Mock<IToolInteractiveService> _mockToolInteractiveService;
        private readonly Mock<IConsoleUtilities> _mockConsoleUtilities;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ICredentialProfileStoreChainFactory> _mockCredentialChainFactory;
        private readonly Mock<ISharedCredentialsFileFactory> _mockSharedCredentialsFileFactory;
        private readonly Mock<IAWSCredentialsFactory> _mockAWSCredentialsFactory;
        private CredentialProfileStoreChain _credentialProfileStoreChain;

        private readonly string _tempCredentialsFile;
        private SharedCredentialsFile _sharedCredentialsFile;

        public AWSUtilitiesTests()
        {
            _directoryManager = new TestDirectoryManager();
            _mockToolInteractiveService = new Mock<IToolInteractiveService>();
            _mockConsoleUtilities = new Mock<IConsoleUtilities>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _optionSettingHandler = new Mock<IOptionSettingHandler>().Object;
            _mockCredentialChainFactory = new Mock<ICredentialProfileStoreChainFactory>();
            _mockSharedCredentialsFileFactory = new Mock<ISharedCredentialsFileFactory>();
            _mockAWSCredentialsFactory = new Mock<IAWSCredentialsFactory>();

            _credentialProfileStoreChain = new CredentialProfileStoreChain();

            _mockCredentialChainFactory
                .Setup(f => f.Create())
                .Returns(_credentialProfileStoreChain);

            // Create a temporary credentials file
            _tempCredentialsFile = Path.GetTempFileName();
            Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", _tempCredentialsFile);

            // Create a real SharedCredentialsFile instance
            _sharedCredentialsFile = new SharedCredentialsFile(_tempCredentialsFile);

            _mockSharedCredentialsFileFactory
                .Setup(f => f.Create())
                .Returns(_sharedCredentialsFile);
        }

        private AWSUtilities CreateAWSUtilities()
        {
            return new AWSUtilities(
                _mockServiceProvider.Object,
                _mockToolInteractiveService.Object,
                _mockConsoleUtilities.Object,
                _directoryManager,
                _optionSettingHandler,
                _mockCredentialChainFactory.Object,
                _mockSharedCredentialsFileFactory.Object,
                _mockAWSCredentialsFactory.Object
            );
        }

        public void Dispose()
        {
            // Clean up the temporary file
            if (File.Exists(_tempCredentialsFile))
            {
                File.Delete(_tempCredentialsFile);
            }
            Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", null);
        }

        private void SetupCredentialsFile(params string[] profileNames)
        {
            var contents = new StringBuilder();
            foreach (var profileName in profileNames)
            {
                contents.AppendLine($"[{profileName}]");
                contents.AppendLine("aws_access_key_id = 123");
                contents.AppendLine("aws_secret_access_key = abc");
                contents.AppendLine();
            }
            File.WriteAllText(_tempCredentialsFile, contents.ToString());

            // Re-create SharedCredentialsFile to pick up the changes
            _sharedCredentialsFile = new SharedCredentialsFile(_tempCredentialsFile);
            _mockSharedCredentialsFileFactory
                .Setup(f => f.Create())
                .Returns(_sharedCredentialsFile);
        }


        [Fact]
        public async Task ResolveAWSCredentials_WithValidProfileName_ReturnsCredentials()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var profileName = "valid-profile";
            var options = new CredentialProfileOptions
            {
                AccessKey = "abc",
                SecretKey = "123"
            };
            var mockProfile = new CredentialProfile(profileName, options)
            {
                Region = RegionEndpoint.USEast1
            };

            var store = new CredentialProfileStoreChain();
            store.RegisterProfile(mockProfile);
            _credentialProfileStoreChain = store;

            _mockCredentialChainFactory
                .Setup(f => f.Create())
                .Returns(_credentialProfileStoreChain);

            // Act
            var result = await awsUtilities.ResolveAWSCredentials(profileName);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Item1);
            Assert.Equal("us-east-1", result.Item2);
        }

        [Fact]
        public async Task ResolveAWSCredentials_WithValidProfileNameAndNullRegion_ReturnsCredentialsAndNullRegion()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var profileName = "valid-profile-no-region";
            var options = new CredentialProfileOptions
            {
                AccessKey = "123",
                SecretKey = "abc"
            };
            var mockProfile = new CredentialProfile(profileName, options)
            {
                Region = null
            };

            var store = new CredentialProfileStoreChain();
            store.RegisterProfile(mockProfile);
            _credentialProfileStoreChain = store;

            _mockCredentialChainFactory
                .Setup(f => f.Create())
                .Returns(_credentialProfileStoreChain);

            // Act
            var result = await awsUtilities.ResolveAWSCredentials(profileName);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        public async Task ResolveAWSCredentials_WithInvalidProfileName_ThrowsException()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var profileName = "invalid-profile";

            var store = new CredentialProfileStoreChain();
            _credentialProfileStoreChain = store;

            _mockCredentialChainFactory
                .Setup(f => f.Create())
                .Returns(_credentialProfileStoreChain);

            // Act & Assert
            await Assert.ThrowsAsync<FailedToGetCredentialsForProfile>(() => awsUtilities.ResolveAWSCredentials(profileName));
        }

        [Fact]
        public async Task ResolveAWSCredentials_WithNullProfileName_UsesFallbackCredentials()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var mockAWSCredentials = new Mock<AWSCredentials>().Object;

            _mockAWSCredentialsFactory
                .Setup(f => f.Create())
                .Returns(mockAWSCredentials);

            // Act
            var result = await awsUtilities.ResolveAWSCredentials(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mockAWSCredentials, result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        public async Task ResolveAWSCredentials_WithNullProfileNameAndFallbackException_PromptsUserToChooseProfile()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var profileNames = new List<string> { "profile1", "profile2" };
            var selectedProfileName = "profile1";

            SetupCredentialsFile(profileNames.ToArray());

            _mockAWSCredentialsFactory
                .Setup(f => f.Create())
                .Throws(new AmazonServiceException("No credentials found"));

            _mockConsoleUtilities
                .Setup(c => c.AskUserToChoose(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(selectedProfileName);

            var store = new CredentialProfileStoreChain(_tempCredentialsFile);
            _credentialProfileStoreChain = store;

            _mockCredentialChainFactory
                .Setup(f => f.Create())
                .Returns(_credentialProfileStoreChain);

            // Act
            var result = await awsUtilities.ResolveAWSCredentials(null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Item1);
            Assert.Null(result.Item2);  // Region will be null as we didn't set it in the file
            _mockConsoleUtilities.Verify(c => c.AskUserToChoose(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResolveAWSCredentials_WithNoProfiles_ThrowsNoAWSCredentialsFoundException()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();

            SetupCredentialsFile();  // Empty file, no profiles

            _mockAWSCredentialsFactory
                .Setup(f => f.Create())
                .Throws(new AmazonServiceException("No credentials found"));

            // Act & Assert
            await Assert.ThrowsAsync<NoAWSCredentialsFoundException>(() => awsUtilities.ResolveAWSCredentials(null));
        }

        [Fact]
        public void ResolveAWSRegion_WithNonNullRegion_ReturnsRegion()
        {
            // Arrange
            var awsUtilities = CreateAWSUtilities();
            var region = "us-west-2";

            // Act
            var result = awsUtilities.ResolveAWSRegion(region);

            // Assert
            Assert.Equal(region, result);
        }
    }
}
