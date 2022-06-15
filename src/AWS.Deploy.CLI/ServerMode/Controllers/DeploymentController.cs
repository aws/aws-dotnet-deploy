// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancingV2;
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
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.DisplayedResources;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.LocalUserSettings;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.ServiceHandlers;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes.Validation;

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
                (await awsResourceQueryer.GetCallerIdentity(input.AWSRegion)).Account,
                await _projectParserUtility.Parse(input.ProjectPath)
                );

            _stateServer.Save(output.SessionId, state);

            var deployedApplicationQueryer = serviceProvider.GetRequiredService<IDeployedApplicationQueryer>();
            var session = CreateOrchestratorSession(state);
            var orchestrator = CreateOrchestrator(state);

            // Determine what recommendations are possible for the project.
            var recommendations = await orchestrator.GenerateDeploymentRecommendations();
            state.NewRecommendations = recommendations;

            // Get all existing CloudApplications based on the deploymentTypes filter
            var allDeployedApplications = await deployedApplicationQueryer.GetExistingDeployedApplications(recommendations.Select(x => x.Recipe.DeploymentType).ToList());

            var existingApplications = await deployedApplicationQueryer.GetCompatibleApplications(recommendations, allDeployedApplications, session);
            state.ExistingDeployments = existingApplications;

            output.DefaultDeploymentName = _cloudApplicationNameGenerator.GenerateValidName(state.ProjectDefinition, existingApplications);
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
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
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

            //NewRecommendations is set during StartDeploymentSession API. It is only updated here if NewRecommendations was null.
            state.NewRecommendations ??= await orchestrator.GenerateDeploymentRecommendations();
            foreach (var recommendation in state.NewRecommendations)
            {
                if (recommendation.Recipe.DisableNewDeployments)
                    continue;

                output.Recommendations.Add(new RecommendationSummary(
                    baseRecipeId: recommendation.Recipe.BaseRecipeId,
                    recipeId: recommendation.Recipe.Id,
                    name: recommendation.Name,
                    settingsCategories: CategorySummary.FromCategories(recommendation.GetConfigurableOptionSettingCategories()),
                    isPersistedDeploymentProject: recommendation.Recipe.PersistedDeploymentProject,
                    shortDescription: recommendation.ShortDescription,
                    description: recommendation.Description,
                    targetService: recommendation.Recipe.TargetService,
                    deploymentType: recommendation.Recipe.DeploymentType
                    ));
            }

            return Ok(output);
        }

        /// <summary>
        /// Gets the list of updatable option setting items for the selected recommendation.
        /// </summary>
        [HttpGet("session/<sessionId>/settings")]
        [SwaggerOperation(OperationId = "GetConfigSettings")]
        [SwaggerResponse(200, type: typeof(GetOptionSettingsOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [Authorize]
        public IActionResult GetConfigSettings(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            if (state.SelectedRecommendation == null)
            {
                return NotFound($"A deployment target is not set for Session ID {sessionId}.");
            }

            var orchestrator = CreateOrchestrator(state);
            var serviceProvider = CreateSessionServiceProvider(state);
            var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();

            var configurableOptionSettings = state.SelectedRecommendation.GetConfigurableOptionSettingItems();

            var output = new GetOptionSettingsOutput();
            output.OptionSettings = ListOptionSettingSummary(optionSettingHandler, state.SelectedRecommendation, configurableOptionSettings);

            return Ok(output);
        }

        private List<OptionSettingItemSummary> ListOptionSettingSummary(IOptionSettingHandler optionSettingHandler, Recommendation recommendation, IEnumerable<OptionSettingItem> configurableOptionSettings)
        {
            var optionSettingItems = new List<OptionSettingItemSummary>();

            foreach (var setting in configurableOptionSettings)
            {
                var settingSummary = new OptionSettingItemSummary(setting.Id, setting.FullyQualifiedId, setting.Name, setting.Description, setting.Type.ToString())
                {
                    Category = setting.Category,
                    TypeHint = setting.TypeHint?.ToString(),
                    TypeHintData = setting.TypeHintData,
                    Value = optionSettingHandler.GetOptionSettingValue(recommendation, setting),
                    Advanced = setting.AdvancedSetting,
                    ReadOnly = recommendation.IsExistingCloudApplication && !setting.Updatable,
                    Visible = optionSettingHandler.IsOptionSettingDisplayable(recommendation, setting),
                    SummaryDisplayable = optionSettingHandler.IsSummaryDisplayable(recommendation, setting),
                    AllowedValues = setting.AllowedValues,
                    ValueMapping = setting.ValueMapping,
                    Validation = setting.Validation,
                    ChildOptionSettings = ListOptionSettingSummary(optionSettingHandler, recommendation, setting.ChildOptionSettings)
                };

                optionSettingItems.Add(settingSummary);
            }

            return optionSettingItems;
        }

        /// <summary>
        /// Applies a value for a list of option setting items on the selected recommendation.
        /// Option setting updates are provided as Key Value pairs with the Key being the JSON path to the leaf node.
        /// Only primitive data types are supported for Value updates. The Value is a string value which will be parsed as its corresponding data type.
        /// </summary>
        [HttpPut("session/<sessionId>/settings")]
        [SwaggerOperation(OperationId = "ApplyConfigSettings")]
        [SwaggerResponse(200, type: typeof(ApplyConfigSettingsOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
        [Authorize]
        public async Task<IActionResult> ApplyConfigSettings(string sessionId, [FromBody] ApplyConfigSettingsInput input)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            if (state.SelectedRecommendation == null)
            {
                return NotFound($"A deployment target is not set for Session ID {sessionId}.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);
            var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();

            var output = new ApplyConfigSettingsOutput();

            var optionSettingItems = input.UpdatedSettings
                .Select(x => optionSettingHandler.GetOptionSetting(state.SelectedRecommendation, x.Key));

            var readonlySettings = optionSettingItems
                .Where(x => state.SelectedRecommendation.IsExistingCloudApplication && !x.Updatable);
            if (readonlySettings.Any())
                return BadRequest($"The following settings are read only and cannot be updated: {string.Join(", ", readonlySettings)}");

            foreach (var updatedSetting in optionSettingItems)
            {
                try
                {
                    await optionSettingHandler.SetOptionSettingValue(state.SelectedRecommendation, updatedSetting, input.UpdatedSettings[updatedSetting.FullyQualifiedId]);
                }
                catch (Exception ex)
                {
                    output.FailedConfigUpdates.Add(updatedSetting.FullyQualifiedId, ex.Message);
                }
            }

            return Ok(output);
        }

        [HttpGet("session/<sessionId>/settings/<configSettingId>/resources")]
        [SwaggerOperation(OperationId = "GetConfigSettingResources")]
        [SwaggerResponse(200, type: typeof(GetConfigSettingResourcesOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> GetConfigSettingResources(string sessionId, string configSettingId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }
            if (state.SelectedRecommendation == null)
            {
                return NotFound($"A deployment target is not set for Session ID {sessionId}.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);
            var typeHintCommandFactory = serviceProvider.GetRequiredService<ITypeHintCommandFactory>();
            var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();

            var configSetting = optionSettingHandler.GetOptionSetting(state.SelectedRecommendation, configSettingId);

            if (configSetting.TypeHint.HasValue && typeHintCommandFactory.GetCommand(configSetting.TypeHint.Value) is var typeHintCommand && typeHintCommand != null)
            {
                var output = new GetConfigSettingResourcesOutput();
                var resources = await typeHintCommand.GetResources(state.SelectedRecommendation, configSetting);

                if (resources == null)
                {
                    return NotFound("The Config Setting type hint is not recognized.");
                }

                output.Resources = resources.Select(x => new TypeHintResourceSummary(x.SystemName, x.DisplayName)).ToList();
                return Ok(output);
            }

            return NotFound("The Config Setting type hint is not recognized.");
        }

        /// <summary>
        /// Gets the list of existing deployments that are compatible with the session's project.
        /// </summary>
        [HttpGet("session/<sessionId>/deployments")]
        [SwaggerOperation(OperationId = "GetExistingDeployments")]
        [SwaggerResponse(200, type: typeof(GetExistingDeploymentsOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
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
            if(state.NewRecommendations == null)
            {
                return Ok(output);
            }

            var deployedApplicationQueryer = serviceProvider.GetRequiredService<IDeployedApplicationQueryer>();
            var session = CreateOrchestratorSession(state);

            //ExistingDeployments is set during StartDeploymentSession API. It is only updated here if ExistingDeployments was null.
            state.ExistingDeployments ??= await deployedApplicationQueryer.GetCompatibleApplications(state.NewRecommendations.ToList(), session: session);

            foreach(var deployment in state.ExistingDeployments)
            {
                var recommendation = state.NewRecommendations.First(x => string.Equals(x.Recipe.Id, deployment.RecipeId));

                output.ExistingDeployments.Add(new ExistingDeploymentSummary(
                    name: deployment.Name,
                    baseRecipeId: recommendation.Recipe.BaseRecipeId,
                    recipeId: deployment.RecipeId,
                    recipeName: recommendation.Name,
                    settingsCategories: CategorySummary.FromCategories(recommendation.GetConfigurableOptionSettingCategories()),
                    isPersistedDeploymentProject: recommendation.Recipe.PersistedDeploymentProject,
                    shortDescription: recommendation.ShortDescription,
                    description: recommendation.Description,
                    targetService: recommendation.Recipe.TargetService,
                    lastUpdatedTime: deployment.LastUpdatedTime,
                    updatedByCurrentUser: deployment.UpdatedByCurrentUser,
                    resourceType: deployment.ResourceType,
                    uniqueIdentifier: deployment.UniqueIdentifier));
            }

            return Ok(output);
        }

        /// <summary>
        /// Set the target recipe and name for the deployment.
        /// </summary>
        [HttpPost("session/<sessionId>")]
        [SwaggerOperation(OperationId = "SetDeploymentTarget")]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
        [Authorize]
        public async Task<IActionResult> SetDeploymentTarget(string sessionId, [FromBody] SetDeploymentTargetInput input)
        {
            var state = _stateServer.Get(sessionId);
            if(state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);
            var orchestrator = CreateOrchestrator(state, serviceProvider);
            var cloudApplicationNameGenerator = serviceProvider.GetRequiredService<ICloudApplicationNameGenerator>();

            if (!string.IsNullOrEmpty(input.NewDeploymentRecipeId))
            {
                var newDeploymentName = input.NewDeploymentName ?? string.Empty;

                state.SelectedRecommendation = state.NewRecommendations?.FirstOrDefault(x => string.Equals(input.NewDeploymentRecipeId, x.Recipe.Id));
                if (state.SelectedRecommendation == null)
                {
                    return NotFound($"Recommendation {input.NewDeploymentRecipeId} not found.");
                }

                // We only validate the name when the recipe deployment type is not ElasticContainerRegistryImage.
                // This is because pushing images to ECR does not need a cloud application name.
                if (state.SelectedRecommendation.Recipe.DeploymentType != Common.Recipes.DeploymentTypes.ElasticContainerRegistryImage)
                {
                    var validationResult = cloudApplicationNameGenerator.IsValidName(newDeploymentName, state.ExistingDeployments ?? new List<CloudApplication>(), state.SelectedRecommendation.Recipe.DeploymentType);
                    if (!validationResult.IsValid)
                        return ValidationProblem(validationResult.ErrorMessage);
                }

                state.ApplicationDetails.Name = newDeploymentName;
                state.ApplicationDetails.UniqueIdentifier = string.Empty;
                state.ApplicationDetails.ResourceType = orchestrator.GetCloudApplicationResourceType(state.SelectedRecommendation.Recipe.DeploymentType);
                state.ApplicationDetails.RecipeId = input.NewDeploymentRecipeId;
                await orchestrator.ApplyAllReplacementTokens(state.SelectedRecommendation, newDeploymentName);
            }
            else if(!string.IsNullOrEmpty(input.ExistingDeploymentId))
            {
                var templateMetadataReader = serviceProvider.GetRequiredService<ITemplateMetadataReader>();
                var deployedApplicationQueryer = serviceProvider.GetRequiredService<IDeployedApplicationQueryer>();
                var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();

                var existingDeployment = state.ExistingDeployments?.FirstOrDefault(x => string.Equals(input.ExistingDeploymentId, x.UniqueIdentifier));
                if (existingDeployment == null)
                {
                    return NotFound($"Existing deployment {input.ExistingDeploymentId} not found.");
                }

                state.SelectedRecommendation = state.NewRecommendations?.FirstOrDefault(x => string.Equals(existingDeployment.RecipeId, x.Recipe.Id));
                if (state.SelectedRecommendation == null)
                {
                    return NotFound($"Recommendation {input.NewDeploymentRecipeId} used in existing deployment {existingDeployment.RecipeId} not found.");
                }

                IDictionary<string, object> previousSettings;
                if (existingDeployment.ResourceType == CloudApplicationResourceType.CloudFormationStack)
                    previousSettings = (await templateMetadataReader.LoadCloudApplicationMetadata(existingDeployment.Name)).Settings;
                else
                    previousSettings = await deployedApplicationQueryer.GetPreviousSettings(existingDeployment);

                state.SelectedRecommendation = await orchestrator.ApplyRecommendationPreviousSettings(state.SelectedRecommendation, previousSettings);

                state.ApplicationDetails.Name = existingDeployment.Name;
                state.ApplicationDetails.UniqueIdentifier = existingDeployment.UniqueIdentifier;
                state.ApplicationDetails.RecipeId = existingDeployment.RecipeId;
                state.ApplicationDetails.ResourceType = existingDeployment.ResourceType;
                await orchestrator.ApplyAllReplacementTokens(state.SelectedRecommendation, existingDeployment.Name);
            }

            return Ok();
        }

        /// <summary>
        /// Checks the missing System Capabilities for a given session.
        /// </summary>
        [HttpPost("session/<sessionId>/compatiblity")]
        [SwaggerOperation(OperationId = "GetCompatibility")]
        [SwaggerResponse(200, type: typeof(GetCompatibilityOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> GetCompatibility(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            if (state.SelectedRecommendation == null)
            {
                return NotFound($"A deployment target is not set for Session ID {sessionId}.");
            }

            var output = new GetCompatibilityOutput();
            var serviceProvider = CreateSessionServiceProvider(state);
            var systemCapabilityEvaluator = serviceProvider.GetRequiredService<ISystemCapabilityEvaluator>();

            var capabilities = await systemCapabilityEvaluator.EvaluateSystemCapabilities(state.SelectedRecommendation);

            output.Capabilities = capabilities.Select(x => new SystemCapabilitySummary(x.Name, x.Message, x.InstallationUrl));

            return Ok(output);
        }

        /// <summary>
        /// Creates the CloudFormation template that will be used by CDK for the deployment.
        /// This operation returns the CloudFormation template that is created for this deployment.
        /// </summary>
        [HttpGet("session/<sessionId>/cftemplate")]
        [SwaggerOperation(OperationId = "GenerateCloudFormationTemplate")]
        [SwaggerResponse(200, type: typeof(GenerateCloudFormationTemplateOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> GenerateCloudFormationTemplate(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);

            var orchestratorSession = CreateOrchestratorSession(state);

            var orchestrator = CreateOrchestrator(state, serviceProvider);

            var cdkProjectHandler = CreateCdkProjectHandler(state, serviceProvider);

            if (state.SelectedRecommendation == null)
                throw new SelectedRecommendationIsNullException("The selected recommendation is null or invalid.");

            if (!state.SelectedRecommendation.Recipe.DeploymentType.Equals(Common.Recipes.DeploymentTypes.CdkProject))
                throw new SelectedRecommendationIsIncompatibleException($"We cannot generate a CloudFormation template for the selected recommendation as it is not of type '{nameof(Models.DeploymentTypes.CloudFormationStack)}'.");

            var task = new DeployRecommendationTask(orchestratorSession, orchestrator, state.ApplicationDetails, state.SelectedRecommendation);
            var cloudFormationTemplate = await task.GenerateCloudFormationTemplate(cdkProjectHandler);
            var output = new GenerateCloudFormationTemplateOutput(cloudFormationTemplate);

            return Ok(output);
        }

        /// <summary>
        /// Begin execution of the deployment.
        /// </summary>
        [HttpPost("session/<sessionId>/execute")]
        [SwaggerOperation(OperationId = "StartDeployment")]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status424FailedDependency)]
        [Authorize]
        public async Task<IActionResult> StartDeployment(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);

            var orchestratorSession = CreateOrchestratorSession(state);

            var orchestrator = CreateOrchestrator(state, serviceProvider);

            if (state.SelectedRecommendation == null)
                throw new SelectedRecommendationIsNullException("The selected recommendation is null or invalid.");

            var optionSettingHandler = serviceProvider.GetRequiredService<IOptionSettingHandler>();
            var validatorFactory = serviceProvider.GetRequiredService<IValidatorFactory>();
            var settingValidatorFailedResults = optionSettingHandler.RunOptionSettingValidators(state.SelectedRecommendation);
            var recipeValidatorFailedResults =
                        validatorFactory.BuildValidators(state.SelectedRecommendation.Recipe)
                            .Select(async validator => await validator.Validate(state.SelectedRecommendation, orchestratorSession))
                            .Select(x => x.Result)
                            .Where(x => !x.IsValid)
                            .ToList();
            if (settingValidatorFailedResults.Any() || recipeValidatorFailedResults.Any())
            {
                var settingValidationErrorMessage = $"The deployment configuration needs to be adjusted before it can be deployed:{Environment.NewLine}";
                foreach (var result in settingValidatorFailedResults)
                    settingValidationErrorMessage += $" - {result.ValidationFailedMessage}{Environment.NewLine}{Environment.NewLine}";
                foreach (var result in recipeValidatorFailedResults)
                    settingValidationErrorMessage += $" - {result.ValidationFailedMessage}{Environment.NewLine}{Environment.NewLine}";
                settingValidationErrorMessage += $"{Environment.NewLine}Please adjust your settings";
                return Problem(settingValidationErrorMessage);
            }

            var systemCapabilityEvaluator = serviceProvider.GetRequiredService<ISystemCapabilityEvaluator>();

            var capabilities = await systemCapabilityEvaluator.EvaluateSystemCapabilities(state.SelectedRecommendation);

            var missingCapabilitiesMessage = "";
            foreach (var capability in capabilities)
            {
                missingCapabilitiesMessage = $"{missingCapabilitiesMessage}{capability.GetMessage()}{Environment.NewLine}";
            }

            if (capabilities.Any())
                return Problem($"Unable to start deployment due to missing system capabilities.{Environment.NewLine}{missingCapabilitiesMessage}", statusCode: Microsoft.AspNetCore.Http.StatusCodes.Status424FailedDependency);

            var task = new DeployRecommendationTask(orchestratorSession, orchestrator, state.ApplicationDetails, state.SelectedRecommendation);
            state.DeploymentTask = task.Execute();

            return Ok();
        }

        /// <summary>
        /// Gets the status of the deployment.
        /// </summary>
        [HttpGet("session/<sessionId>/execute")]
        [SwaggerOperation(OperationId = "GetDeploymentStatus")]
        [SwaggerResponse(200, type: typeof(GetDeploymentStatusOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
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
            {
                output.Status = DeploymentStatus.Error;
                if (state.DeploymentTask.Exception != null)
                {
                    var innerException = state.DeploymentTask.Exception.InnerException;
                    if (innerException is DeployToolException deployToolException)
                    {
                        output.Exception = new DeployToolExceptionSummary(deployToolException.ErrorCode.ToString(), deployToolException.Message, deployToolException.ProcessExitCode);
                    }
                    else
                    {
                        output.Exception = new DeployToolExceptionSummary(DeployToolErrorCode.UnexpectedError.ToString(), innerException?.Message ?? string.Empty);
                    }
                }
            }
            else
                output.Status = DeploymentStatus.Executing;

            return Ok(output);
        }

        /// <summary>
        /// Gets information about the displayed resources defined in the recipe definition.
        /// </summary>
        [HttpGet("session/<sessionId>/details")]
        [SwaggerOperation(OperationId = "GetDeploymentDetails")]
        [SwaggerResponse(200, type: typeof(GetDeploymentDetailsOutput))]
        [ProducesResponseType(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> GetDeploymentDetails(string sessionId)
        {
            var state = _stateServer.Get(sessionId);
            if (state == null)
            {
                return NotFound($"Session ID {sessionId} not found.");
            }

            var serviceProvider = CreateSessionServiceProvider(state);
            var displayedResourcesHandler = serviceProvider.GetRequiredService<IDisplayedResourcesHandler>();

            if (state.SelectedRecommendation == null)
            {
                return NotFound($"A deployment target is not set for Session ID {sessionId}.");
            }

            var displayedResources = await displayedResourcesHandler.GetDeploymentOutputs(state.ApplicationDetails, state.SelectedRecommendation);

            var output = new GetDeploymentDetailsOutput(
                state.ApplicationDetails.Name,
                displayedResources
                    .Select(x => new DisplayedResourceSummary(x.Id, x.Description, x.Type, x.Data))
                    .ToList());

            return Ok(output);
        }

        private IServiceProvider CreateSessionServiceProvider(SessionState state)
        {
            return CreateSessionServiceProvider(state.SessionId, state.AWSRegion);
        }

        private IServiceProvider CreateSessionServiceProvider(string sessionId, string awsRegion)
        {
            var awsCredentials = HttpContext.User.ToAWSCredentials();
            if(awsCredentials == null)
            {
                throw new FailedToRetrieveAWSCredentialsException("AWS credentials are missing for the current session.");
            }

            var interactiveServices = new SessionOrchestratorInteractiveService(sessionId, _hubContext);
            var services = new ServiceCollection();
            services.AddSingleton<IOrchestratorInteractiveService>(interactiveServices);
            services.AddSingleton<ICommandLineWrapper>(services =>
            {
                var wrapper = new CommandLineWrapper(interactiveServices, true);
                wrapper.RegisterAWSContext(awsCredentials, awsRegion);
                return wrapper;
            });

            services.AddCustomServices();
            var serviceProvider = services.BuildServiceProvider();

            var awsClientFactory = serviceProvider.GetRequiredService<IAWSClientFactory>();

            awsClientFactory.ConfigureAWSOptions(awsOptions =>
            {
                awsOptions.Credentials = awsCredentials;
                awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);
            });

            return serviceProvider;
        }

        private OrchestratorSession CreateOrchestratorSession(SessionState state, AWSCredentials? awsCredentials = null)
        {
            return new OrchestratorSession(
                state.ProjectDefinition,
                awsCredentials ?? HttpContext.User.ToAWSCredentials() ??
                    throw new FailedToRetrieveAWSCredentialsException("The tool was not able to retrieve the AWS Credentials."),
                state.AWSRegion,
                state.AWSAccountId);
        }

        private CdkProjectHandler CreateCdkProjectHandler(SessionState state, IServiceProvider? serviceProvider = null)
        {
            if (serviceProvider == null)
            {
                serviceProvider = CreateSessionServiceProvider(state);
            }

            return new CdkProjectHandler(
                serviceProvider.GetRequiredService<IOrchestratorInteractiveService>(),
                serviceProvider.GetRequiredService<ICommandLineWrapper>(),
                serviceProvider.GetRequiredService<IAWSResourceQueryer>(),
                serviceProvider.GetRequiredService<IFileManager>(),
                serviceProvider.GetRequiredService<IOptionSettingHandler>()
                );
        }

        private Orchestrator CreateOrchestrator(SessionState state, IServiceProvider? serviceProvider = null, AWSCredentials? awsCredentials = null)
        {
            if(serviceProvider == null)
            {
                serviceProvider = CreateSessionServiceProvider(state);
            }

            var session = CreateOrchestratorSession(state, awsCredentials);

            return new Orchestrator(
                                    session,
                                    serviceProvider.GetRequiredService<IOrchestratorInteractiveService>(),
                                    serviceProvider.GetRequiredService<ICdkProjectHandler>(),
                                    serviceProvider.GetRequiredService<ICDKManager>(),
                                    serviceProvider.GetRequiredService<ICDKVersionDetector>(),
                                    serviceProvider.GetRequiredService<IAWSResourceQueryer>(),
                                    serviceProvider.GetRequiredService<IDeploymentBundleHandler>(),
                                    serviceProvider.GetRequiredService<ILocalUserSettingsEngine>(),
                                    new DockerEngine.DockerEngine(
                                        session.ProjectDefinition,
                                        serviceProvider.GetRequiredService<IFileManager>(),
                                        serviceProvider.GetRequiredService<IDirectoryManager>()),
                                    serviceProvider.GetRequiredService<IRecipeHandler>(),
                                    serviceProvider.GetRequiredService<IFileManager>(),
                                    serviceProvider.GetRequiredService<IDirectoryManager>(),
                                    serviceProvider.GetRequiredService<IAWSServiceHandler>(),
                                    serviceProvider.GetRequiredService<IOptionSettingHandler>()
                                );
        }
    }
}
