// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.BedrockAgentCore;
using Amazon.BedrockAgentCore.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.CLI.Common.UnitTests.IO;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Helpers;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace AWS.Deploy.CLI.IntegrationTests
{
    /// <summary>
    /// Integration test that deploys an AgentCore agent using the deploy tool,
    /// invokes it via the AgentCore SDK, and tears down the stack.
    /// Requires AWS credentials with Bedrock and AgentCore permissions.
    /// </summary>
    public class AgentCoreDeploymentTests : IDisposable
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly CloudFormationHelper _cloudFormationHelper;
        private readonly ITestOutputHelper _output;
        private bool _isDisposed;
        private string? _stackName;
        private readonly TestAppManager _testAppManager;

        public AgentCoreDeploymentTests(ITestOutputHelper output)
        {
            _output = output;
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();

            var cloudFormationClient = new AmazonCloudFormationClient();
            _cloudFormationHelper = new CloudFormationHelper(cloudFormationClient);

            _testAppManager = new TestAppManager();
        }

        [Fact]
        public async Task DeployAndInvokeAgentCoreRuntime()
        {
            _stackName = $"AgentCore{Guid.NewGuid().ToString().Split('-').Last()}";
            var projectPath = _testAppManager.GetProjectPath(Path.Combine("testapps", "AgentCoreWebApp", "AgentCoreWebApp.csproj"));
            _output.WriteLine($"Stack name: {_stackName}");
            _output.WriteLine($"Project path: {projectPath}");

            InMemoryInteractiveService interactiveService = null!;
            try
            {
                // Deploy
                _output.WriteLine("Starting deploy...");
                var deployArgs = new[] { "deploy", "--project-path", projectPath, "--application-name", _stackName, "--diagnostics" };
                var exitCode = await _serviceCollection.RunDeployToolAsync(deployArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();

                        interactiveService.StdInWriter.Write(Environment.NewLine); // Select default recommendation
                        interactiveService.StdInWriter.Write(Environment.NewLine); // Accept default settings
                        interactiveService.StdInWriter.Flush();
                    });

                var deployStdOut = interactiveService.StdOutReader.ReadAllLines();
                _output.WriteLine($"Deploy exit code: {exitCode}");
                _output.WriteLine($"Deploy output (last 20 lines):");
                foreach (var line in deployStdOut.TakeLast(20))
                    _output.WriteLine($"  {line}");

                Assert.Equal(CommandReturnCodes.SUCCESS, exitCode);

                // Verify stack
                var stackStatus = await _cloudFormationHelper.GetStackStatus(_stackName);
                _output.WriteLine($"Stack status: {stackStatus}");
                Assert.Equal(StackStatus.CREATE_COMPLETE, stackStatus);

                // Extract ARN
                var arnLine = deployStdOut.FirstOrDefault(line => line.Trim().StartsWith("ARN:"));
                _output.WriteLine($"ARN line: {arnLine}");
                Assert.NotNull(arnLine);
                var runtimeArn = arnLine!.Split(":", 2)[1].Trim();
                _output.WriteLine($"Runtime ARN: {runtimeArn}");
                Assert.StartsWith("arn:", runtimeArn);

                // Invoke
                _output.WriteLine("Invoking agent...");
                var agentCoreClient = new AmazonBedrockAgentCoreClient(Amazon.RegionEndpoint.USWest2);
                var payload = JsonSerializer.Serialize(new { prompt = "What is 2+2? Reply with just the number." });
                using var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

                var invokeResponse = await agentCoreClient.InvokeAgentRuntimeAsync(new InvokeAgentRuntimeRequest
                {
                    AgentRuntimeArn = runtimeArn,
                    Payload = payloadStream,
                    ContentType = "application/json"
                });

                using var reader = new StreamReader(invokeResponse.Response);
                var responseBody = await reader.ReadToEndAsync();
                _output.WriteLine($"Agent response: {responseBody}");

                Assert.NotEmpty(responseBody);
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }

            try
            {
                // Delete
                _output.WriteLine("Deleting stack...");
                var deleteArgs = new[] { "delete-deployment", _stackName, "--diagnostics" };
                Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync(deleteArgs,
                    provider =>
                    {
                        interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                        interactiveService.StdInWriter.Write("y");
                        interactiveService.StdInWriter.Flush();
                    }));

                Assert.True(await _cloudFormationHelper.IsStackDeleted(_stackName), $"{_stackName} still exists.");
                _output.WriteLine("Stack deleted.");
            }
            finally
            {
                interactiveService?.ReadStdOutStartToEnd();
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing && !string.IsNullOrEmpty(_stackName))
            {
                var isStackDeleted = _cloudFormationHelper.IsStackDeleted(_stackName).GetAwaiter().GetResult();
                if (!isStackDeleted)
                {
                    _cloudFormationHelper.DeleteStack(_stackName).GetAwaiter().GetResult();
                }
            }

            _isDisposed = true;
        }

        ~AgentCoreDeploymentTests()
        {
            Dispose(false);
        }
    }
}
