// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.Runtime;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.DisplayedResources;
using AWS.Deploy.Orchestration.UnitTests.Utilities;
using AWS.Deploy.Recipes;
using Moq;
using Xunit;
using LoadBalancer = Amazon.ElasticLoadBalancingV2.Model.LoadBalancer;

namespace AWS.Deploy.Orchestration.UnitTests
{
    public class DisplayedResourcesHandlerTests
    {
        private readonly Mock<IAWSResourceQueryer> _mockAWSResourceQueryer;
        private readonly CloudApplication _cloudApplication;
        private readonly DisplayedResourceCommandFactory _displayedResourcesFactory;
        private readonly StackResource _stackResource;
        private readonly List<StackResource> _stackResources;
        private readonly EnvironmentDescription _environmentDescription;
        private readonly LoadBalancer _loadBalancer;
        private OrchestratorSession _session;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly Mock<IOrchestratorInteractiveService> _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public DisplayedResourcesHandlerTests()
        {
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new Mock<IOrchestratorInteractiveService>();
            var serviceProvider = new Mock<IServiceProvider>();
            var validatorFactory = new ValidatorFactory(serviceProvider.Object);
            var optionSettingHandler = new OptionSettingHandler(validatorFactory);
            _recipeHandler = new RecipeHandler(_deploymentManifestEngine, _orchestratorInteractiveService.Object, _directoryManager, _fileManager, optionSettingHandler, validatorFactory);
            _mockAWSResourceQueryer = new Mock<IAWSResourceQueryer>();
            _cloudApplication = new CloudApplication("StackName", "UniqueId", CloudApplicationResourceType.CloudFormationStack, "RecipeId");
            _displayedResourcesFactory = new DisplayedResourceCommandFactory(_mockAWSResourceQueryer.Object);
            _stackResource = new StackResource();
            _stackResources = new List<StackResource>() { _stackResource };
            _environmentDescription = new EnvironmentDescription();
            _loadBalancer = new LoadBalancer();
        }

        private async Task<RecommendationEngine.RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(new FileManager(), new DirectoryManager());
            var awsCredentials = new Mock<AWSCredentials>();
            _session = new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine.RecommendationEngine(_session, _recipeHandler);
        }

        [Fact]
        public async Task GetDeploymentOutputs_ElasticBeanstalkEnvironment()
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("AspNetAppElasticBeanstalkLinux"));

            _stackResource.LogicalResourceId = "RecipeBeanstalkEnvironment83CC12DE";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "AWS::ElasticBeanstalk::Environment";
            _environmentDescription.CNAME = "www.website.com";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            _mockAWSResourceQueryer.Setup(x => x.DescribeElasticBeanstalkEnvironment(It.IsAny<string>())).Returns(Task.FromResult(_environmentDescription));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            Assert.Single(outputs);
            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("AWS::ElasticBeanstalk::Environment", resource.Type);
            Assert.Single(resource.Data);
            Assert.True(resource.Data.ContainsKey("Endpoint"));
            Assert.Equal("http://www.website.com/", resource.Data["Endpoint"]);

        }

        [Fact]
        public async Task GetDeploymentOutputs_ElasticLoadBalancer()
        {
            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("AspNetAppEcsFargate"));

            _stackResource.LogicalResourceId = "RecipeServiceLoadBalancer68534AEF";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "AWS::ElasticLoadBalancingV2::LoadBalancer";
            _loadBalancer.DNSName = "www.website.com";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            _mockAWSResourceQueryer.Setup(x => x.DescribeElasticLoadBalancer(It.IsAny<string>())).Returns(Task.FromResult(_loadBalancer));
            _mockAWSResourceQueryer.Setup(x => x.DescribeElasticLoadBalancerListeners(It.IsAny<string>())).Returns(Task.FromResult(new List<Amazon.ElasticLoadBalancingV2.Model.Listener>()));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            Assert.Single(outputs);
            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("AWS::ElasticLoadBalancingV2::LoadBalancer", resource.Type);
            Assert.Single(resource.Data);
            Assert.True(resource.Data.ContainsKey("Endpoint"));
            Assert.Equal("http://www.website.com/", resource.Data["Endpoint"]);

        }

        [Fact]
        public async Task GetDeploymentOutputs_S3BucketWithWebSiteConfig()
        {
            var engine = await BuildRecommendationEngine("BlazorWasm60");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("BlazorWasm"));

            _stackResource.LogicalResourceId = "RecipeContentS3BucketE74B8362";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "AWS::S3::Bucket";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            _mockAWSResourceQueryer.Setup(x => x.GetS3BucketLocation(It.IsAny<string>())).Returns(Task.FromResult("us-west-2"));
            _mockAWSResourceQueryer.Setup(x => x.GetS3BucketWebSiteConfiguration(It.IsAny<string>())).Returns(Task.FromResult(new Amazon.S3.Model.WebsiteConfiguration { IndexDocumentSuffix = "index.html" }));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("AWS::S3::Bucket", resource.Type);
            Assert.Equal(2, resource.Data.Count);
            Assert.True(resource.Data.ContainsKey("Endpoint"));
            Assert.True(resource.Data.ContainsKey("Bucket Name"));
            Assert.Equal("http://PhysicalResourceId.s3-website-us-west-2.amazonaws.com/", resource.Data["Endpoint"]);
            Assert.Equal("PhysicalResourceId", resource.Data["Bucket Name"]);
        }

        [Fact]
        public async Task GetDeploymentOutputs_S3BucketWithoutWebSiteConfig()
        {
            var engine = await BuildRecommendationEngine("BlazorWasm60");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("BlazorWasm"));

            _stackResource.LogicalResourceId = "RecipeContentS3BucketE74B8362";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "AWS::S3::Bucket";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            _mockAWSResourceQueryer.Setup(x => x.GetS3BucketLocation(It.IsAny<string>())).Returns(Task.FromResult("us-west-2"));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("AWS::S3::Bucket", resource.Type);
            Assert.Single(resource.Data);
            Assert.True(resource.Data.ContainsKey("Bucket Name"));
            Assert.Equal("PhysicalResourceId", resource.Data["Bucket Name"]);
        }

        [Fact]
        public async Task GetDeploymentOutputs_CloudFrontDistribution()
        {
            var engine = await BuildRecommendationEngine("BlazorWasm60");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("BlazorWasm"));

            _stackResource.LogicalResourceId = "RecipeCloudFrontDistribution2BE25932";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "AWS::CloudFront::Distribution";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            _mockAWSResourceQueryer.Setup(x => x.GetCloudFrontDistribution(It.IsAny<string>())).Returns(Task.FromResult(new Amazon.CloudFront.Model.Distribution
            {
                Id = "PhysicalResourceId",
                DomainName = "id.cloudfront.net"
            }));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("AWS::CloudFront::Distribution", resource.Type);
            Assert.Single(resource.Data);
            Assert.True(resource.Data.ContainsKey("Endpoint"));
            Assert.Equal("https://id.cloudfront.net/", resource.Data["Endpoint"]);
        }

        [Fact]
        public async Task GetDeploymentOutputs_UnknownType()
        {
            var engine = await BuildRecommendationEngine("ConsoleAppService");
            var recommendations = await engine.ComputeRecommendations();
            var recommendation = recommendations.First(r => r.Recipe.Id.Equals("ConsoleAppEcsFargateService"));

            _stackResource.LogicalResourceId = "RecipeEcsClusterB4EDBB7E";
            _stackResource.PhysicalResourceId = "PhysicalResourceId";
            _stackResource.ResourceType = "UnknownType";
            _mockAWSResourceQueryer.Setup(x => x.DescribeCloudFormationResources(It.IsAny<string>())).Returns(Task.FromResult(_stackResources));
            var disaplayedResourcesHandler = new DisplayedResourcesHandler(_mockAWSResourceQueryer.Object, _displayedResourcesFactory);

            var outputs = await disaplayedResourcesHandler.GetDeploymentOutputs(_cloudApplication, recommendation);

            Assert.Single(outputs);
            var resource = outputs.First();
            Assert.Equal("PhysicalResourceId", resource.Id);
            Assert.Equal("UnknownType", resource.Type);
            Assert.Empty(resource.Data);
        }
    }
}
