// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Amazon;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Common;
using AWS.Deploy.CLI.ServerMode.Tasks;
using AWS.Deploy.CLI.ServerMode.Models;
using AWS.Deploy.CLI.ServerMode.Services;
using AWS.Deploy.Orchestration;

using Swashbuckle.AspNetCore.Annotations;
using AWS.Deploy.CLI.ServerMode.Hubs;
using Microsoft.AspNetCore.SignalR;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.AspNetCore.Authorization;
using Amazon.Runtime;

namespace AWS.Deploy.CLI.ServerMode.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DeploymentController : ControllerBase
    {
        private readonly IDeploymentSessionStateServer _stateServer;
        private readonly IProjectParserUtility _projectParserUtility;
        private readonly ICloudApplicationNameGenerator _cloudApplicationNameGenerator;
        private readonly IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> _hubContext;

        public DeploymentController(
                        IDeploymentSessionStateServer stateServer,
                        IProjectParserUtility projectParserUtility,
                        ICloudApplicationNameGenerator cloudApplicationNameGenerator,
                        IHubContext<DeploymentCommunicationHub, IDeploymentCommunicationHub> hubContext
                    )
        {
            _stateServer = stateServer;
            _projectParserUtility = projectParserUtility;
            _cloudApplicationNameGenerator = cloudApplicationNameGenerator;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Start a deployment session. A session id will be generated. This session id needs to be passed in future API calls to configure and execute deployment.
        /// </summary>
        [HttpPost("session")]
        [SwaggerOperation(OperationId = "StartDeploymentSession")]
        [SwaggerResponse(200, type: typeof(StartDeploymentSessionOutput))]
        [Authorize]
        public async Task<IActionResult> StartDeploymentSession(StartDeploymentSessionInput input)
        {
            var output = new StartDeploymentSessionOutput(
                Guid.NewGuid().ToString()
                );

            var serviceProvider = CreateSessionServiceProvider(output.SessionId, input.AWSRegion);
            var awsResourceQueryer = serviceProvider.GetRequiredService<IAWSResourceQueryer>();

            var state = new SessionState(
                output.SessionId,
                input.ProjectPath,
                input.AWSRegion,
                (await awsResourceQueryer.GetCallerIdentity()).Account,
                await _projectParserUtility.Parse(input.ProjectPath)
                );

            _stateServer.Save(output.SessionId, state);

            output.DefaultDeploymentName = _cloudApplicationNameGenerator.GenerateValidName(state.ProjectDefinition, new List<CloudApplication>());
            return Ok(output);
        }

        /// <summary>
        /// Closes the deployment session. This removes any session state for the session id.
        /// </summary>
        [HttpDelete("session/<sessionId>")]
        [SwaggerOperation(OperationId = "CloseDeploymentSession")]
        [Authorize]
        public IActionResult CloseDeploymentSession(string sessionId)
        {
            _stateServer.Delete(sessionId);
            return Ok();
        }

        /// <summary>
        /// Gets the list of compatible deployments for the session's project. The list is ordered with the first recommendation in the list being the most compatible recommendation.
        /// </summary>
        [HttpGet("session/<sessionId>/recommendations")]
        [SwaggerOperation(OperationId = "GetRecommendations")]
        [SwaggerResponse(200, type: typeof(GetRecommendationsOutput))]
        [Authorize]
        public async Task<IActionResult> GetRecommendations(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var orchestrator = CreateOrchestrator(state);

            var output = new GetRecommendationsOutput();

            state.NewRecommendations = await orchestrator.GenerateDeploymentRecommendations();
            foreach (var recommendation in state.NewRecommendations)
            {
                output.Recommendations.Add(new RecommendationSummary(
                    recommendation.Recipe.Id,
                    recommendation.Name,
                    recommendation.Description
                    ));
            }

            return Ok(output);
        }

        /// <summary>
        /// Gets the list of existing deployments that are compatible with the session's project.
        /// </summary>
        [HttpGet("session/<sessionId>/deployments")]
        [SwaggerOperation(OperationId = "GetExistingDeployments")]
        [SwaggerResponse(200, type: typeof(GetExistingDeploymentsOutput))]
        [Authorize]
        public async Task<IActionResult> GetExistingDeployments(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);

            if(state.NewRecommendations == null)
            {
                await GetRecommendations(sessionId);
            }

            var output = new GetExistingDeploymentsOutput();

            var deployedApplicationQueryer = serviceProvider.GetRequiredService<IDeployedApplicationQueryer>();
            state.ExistingDeployments = await deployedApplicationQueryer.GetExistingDeployedApplications(state.NewRecommendations);

            foreach(var deployment in state.ExistingDeployments)
            {
                output.ExistingDeployments.Add(new ExistingDeploymentSummary(
                    deployment.Name,
                    deployment.RecipeId));
            }

            return Ok(output);
        }

        /// <summary>
        /// Set the target recipe and name for the deployment.
        /// </summary>
        [HttpPost("session/<sessionId>")]
        [SwaggerOperation(OperationId = "SetDeploymentTarget")]
        [Authorize]
        public async Task<IActionResult> SetDeploymentTarget(string sessionId, [FromBody] SetDeploymentTargetInput input)
        {
            var state = _stateServer.Get(sessionId);
            if(state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            if(!string.IsNullOrEmpty(input.NewDeploymentRecipeId) &&
               !string.IsNullOrEmpty(input.NewDeploymentName))
            {
                state.SelectedRecommendation = state.NewRecommendations.FirstOrDefault(x => string.Equals(input.NewDeploymentRecipeId, x.Recipe.Id));
                if(state.SelectedRecommendation == null)
                {
                    return NotFound($"Recommendation {input.NewDeploymentRecipeId} not found.");
                }

                state.ApplicationDetails.Name = input.NewDeploymentName;
                state.ApplicationDetails.RecipeId = input.NewDeploymentRecipeId;
            }
            else if(!string.IsNullOrEmpty(input.ExistingDeploymentName))
            {
                var serviceProvider = CreateSessionServiceProvider(state);
                var templateMetadataReader = serviceProvider.GetRequiredService<ITemplateMetadataReader>();

                var existingDeployment = state.ExistingDeployments.FirstOrDefault(x => string.Equals(input.ExistingDeploymentName, x.Name));
                if (existingDeployment == null)
                {
                    return NotFound($"Existing deployment {input.ExistingDeploymentName} not found.");
                }

                state.SelectedRecommendation = state.NewRecommendations.FirstOrDefault(x => string.Equals(existingDeployment.RecipeId, x.Recipe.Id));
                if (state.SelectedRecommendation == null)
                {
                    return NotFound($"Recommendation {input.NewDeploymentRecipeId} used in existing deployment {existingDeployment.RecipeId} not found.");
                }

                var existingCloudApplicationMetadata = await templateMetadataReader.LoadCloudApplicationMetadata(input.ExistingDeploymentName);
                state.SelectedRecommendation.ApplyPreviousSettings(existingCloudApplicationMetadata.Settings);

                state.ApplicationDetails.Name = input.ExistingDeploymentName;
                state.ApplicationDetails.RecipeId = existingDeployment.RecipeId;
            }

            return Ok();
        }

        /// <summary>
        /// Begin execution of the deployment.
        /// </summary>
        [HttpPost("session/<sessionId>/execute")]
        [SwaggerOperation(OperationId = "StartDeployment")]
        [Authorize]
        public IActionResult StartDeployment(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);

            var orchestrator = CreateOrchestrator(state, serviceProvider);

            if (state.SelectedRecommendation == null)
                throw new SelectedRecommendationIsNullException("The selected recommendation is null or invalid.");

            var task = new DeployRecommendationTask(orchestrator, state.ApplicationDetails, state.SelectedRecommendation);
            state.DeploymentTask = task.Execute();

            return Ok();
        }

        /// <summary>
        /// Gets the status of the deployment.
        /// </summary>
        [HttpGet("session/<sessionId>/execute")]
        [SwaggerOperation(OperationId = "GetDeploymentStatus")]
        [SwaggerResponse(200, type: typeof(GetDeploymentStatusOutput))]
        [Authorize]
        public IActionResult GetDeploymentStatus(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var output = new GetDeploymentStatusOutput();

            if (state.DeploymentTask == null)
                output.Status = DeploymentStatus.NotStarted;
            else if (state.DeploymentTask.IsCompleted && state.DeploymentTask.Status == TaskStatus.RanToCompletion)
                output.Status = DeploymentStatus.Success;
            else if (state.DeploymentTask.IsCompleted && state.DeploymentTask.Status == TaskStatus.Faulted)
                output.Status = DeploymentStatus.Error;
            else
                output.Status = DeploymentStatus.Executing;

            return Ok(output);
        }

        private IServiceProvider CreateSessionServiceProvider(SessionState state)
        {
            return CreateSessionServiceProvider(state.SessionId, state.AWSRegion);
        }

        private IServiceProvider CreateSessionServiceProvider(string sessionId, string awsRegion)
        {
            var interactiveServices = new SessionOrchestratorInteractiveService(sessionId, _hubContext);
            var services = new ServiceCollection();
            services.AddSingleton<IOrchestratorInteractiveService>(interactiveServices);
            services.AddSingleton<ICommandLineWrapper>(services => new CommandLineWrapper(interactiveServices, true));
            services.AddCustomServices();
            var serviceProvider = services.BuildServiceProvider();

            var awsClientFactory = serviceProvider.GetRequiredService<IAWSClientFactory>();
            var awsCredentials = HttpContext.User.ToAWSCredentials();
            awsClientFactory.ConfigureAWSOptions(awsOptions =>
            {
                awsOptions.Credentials = awsCredentials;
                awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);
            });

            return serviceProvider;
        }

        private Orchestrator CreateOrchestrator(SessionState state, IServiceProvider? serviceProvider = null, AWSCredentials? awsCredentials = null)
        {
            if(serviceProvider == null)
            {
                serviceProvider = CreateSessionServiceProvider(state);
            }

            var session = new OrchestratorSession(
                state.ProjectDefinition,
                awsCredentials ?? HttpContext.User.ToAWSCredentials() ??
                    throw new FailedToRetrieveAWSCredentialsException("The tool was not able to retrieve the AWS Credentials."),
                state.AWSRegion,
                state.AWSAccountId);

            return new Orchestrator(
                                    session,
                                    serviceProvider.GetRequiredService<IOrchestratorInteractiveService>(),
                                    serviceProvider.GetRequiredService<ICdkProjectHandler>(),
                                    serviceProvider.GetRequiredService<ICDKManager>(),
                                    serviceProvider.GetRequiredService<IAWSResourceQueryer>(),
                                    serviceProvider.GetRequiredService<IDeploymentBundleHandler>(),
                                    new DockerEngine.DockerEngine(session.ProjectDefinition),
                                    new List<string> { RecipeLocator.FindRecipeDefinitionsPath() }
                                );
        }
    }
}
