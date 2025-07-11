// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration.Utilities;
using InvalidOperationException = System.InvalidOperationException;
using Stack = Amazon.CloudFormation.Model.Stack;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkProjectHandler
    {
        Task<string> ConfigureCdkProject(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation);
        Task<string> CreateCdkProject(Recommendation recommendation, OrchestratorSession session, string? saveDirectoryPath = null);
        Task DeployCdkProject(OrchestratorSession session, CloudApplication cloudApplication, string cdkProjectPath, Recommendation recommendation);
        void DeleteTemporaryCdkProject(string cdkProjectPath);
        Task<string> PerformCdkDiff(string cdkProjectPath, CloudApplication cloudApplication);
    }

    public class CdkProjectHandler : ICdkProjectHandler
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly ICdkAppSettingsSerializer _appSettingsBuilder;
        private readonly IDirectoryManager _directoryManager;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IFileManager _fileManager;
        private readonly IDeployToolWorkspaceMetadata _workspaceMetadata;
        private readonly ICloudFormationTemplateReader _cloudFormationTemplateReader;

        public CdkProjectHandler(
            IOrchestratorInteractiveService interactiveService,
            ICommandLineWrapper commandLineWrapper,
            IAWSResourceQueryer awsResourceQueryer,
            ICdkAppSettingsSerializer cdkAppSettingsSerializer,
            IFileManager fileManager,
            IDirectoryManager directoryManager,
            IOptionSettingHandler optionSettingHandler,
            IDeployToolWorkspaceMetadata workspaceMetadata,
            ICloudFormationTemplateReader cloudFormationTemplateReader)
        {
            _interactiveService = interactiveService;
            _commandLineWrapper = commandLineWrapper;
            _awsResourceQueryer = awsResourceQueryer;
            _appSettingsBuilder = cdkAppSettingsSerializer;
            _directoryManager = directoryManager;
            _fileManager = fileManager;
            _workspaceMetadata = workspaceMetadata;
            _cloudFormationTemplateReader = cloudFormationTemplateReader;
        }

        public async Task<string> ConfigureCdkProject(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation)
        {
            string? cdkProjectPath;
            if (recommendation.Recipe.PersistedDeploymentProject)
            {
                if (string.IsNullOrEmpty(recommendation.Recipe.RecipePath))
                    throw new InvalidOperationException($"{nameof(recommendation.Recipe.RecipePath)} cannot be null");

                // The CDK deployment project is already saved in the same directory.
                cdkProjectPath = _directoryManager.GetDirectoryInfo(recommendation.Recipe.RecipePath).Parent?.FullName;
                if (string.IsNullOrEmpty(cdkProjectPath))
                    throw new InvalidOperationException($"The CDK Project Path cannot be null.");
            }
            else
            {
                // Create a new temporary CDK project for a new deployment
                _interactiveService.LogInfoMessage("Generating AWS Cloud Development Kit (AWS CDK) deployment project");
                cdkProjectPath = await CreateCdkProject(recommendation, session);
            }

            // Write required configuration in appsettings.json
            var appSettingsBody = _appSettingsBuilder.Build(cloudApplication, recommendation, session);
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");
            await using var appSettingsFile = new StreamWriter(appSettingsFilePath);
            await appSettingsFile.WriteAsync(appSettingsBody);

            return cdkProjectPath;
        }

        /// <summary>
        /// Run 'cdk diff' on the deployment project <param name="cdkProjectPath"/> to get the CF template that will be used by CDK to deploy the application.
        /// </summary>
        /// <returns>The CloudFormation template that is created for this deployment.</returns>
        public async Task<string> PerformCdkDiff(string cdkProjectPath, CloudApplication cloudApplication)
        {
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");

            var cdkDiff = await _commandLineWrapper.TryRunWithResult($"npx cdk diff -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"",
                workingDirectory: cdkProjectPath,
                needAwsCredentials: true);

            if (cdkDiff.ExitCode != 0)
                throw new FailedToRunCDKDiffException(DeployToolErrorCode.FailedToRunCDKDiff, "The CDK Diff command encountered an error and failed.", cdkDiff.ExitCode);

            var templateFilePath = Path.Combine(cdkProjectPath, "cdk.out", $"{cloudApplication.Name}.template.json");
            return await _fileManager.ReadAllTextAsync(templateFilePath);
        }

        public async Task DeployCdkProject(OrchestratorSession session, CloudApplication cloudApplication, string cdkProjectPath, Recommendation recommendation)
        {
            var recipeInfo = $"{recommendation.Recipe.Id}_{recommendation.Recipe.Version}";
            var environmentVariables = new Dictionary<string, string>
            {
                { EnvironmentVariableKeys.AWS_EXECUTION_ENV, recipeInfo }
            };

            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");

            if (await DetermineIfCDKBootstrapShouldRun())
            {
                // Ensure region is bootstrapped
                var cdkBootstrap = await _commandLineWrapper.TryRunWithResult($"npx cdk bootstrap aws://{session.AWSAccountId}/{session.AWSRegion} --template \"{_workspaceMetadata.CDKBootstrapTemplatePath}\"",
                    workingDirectory: _workspaceMetadata.DeployToolWorkspaceDirectoryRoot,
                    needAwsCredentials: true,
                    redirectIO: true,
                    streamOutputToInteractiveService: true);

                if (cdkBootstrap.ExitCode != 0)
                    throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToRunCDKBootstrap, "The AWS CDK Bootstrap, which is the process of provisioning initial resources for the deployment environment, has failed. Please review the output above for additional details [and check out our troubleshooting guide for the most common failure reasons]. You can learn more about CDK bootstrapping at https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html.", cdkBootstrap.ExitCode);
            }
            else
            {
                _interactiveService.LogInfoMessage("Confirmed CDK Bootstrap CloudFormation stack already exists.");
            }


            _interactiveService.LogSectionStart("Deploying AWS CDK project",
                "Use the CDK project to create or update the AWS CloudFormation stack and deploy the project to the AWS resources in the stack.");

            // Handover to CDK command line tool
            // Use a CDK Context parameter to specify the settings file that has been serialized.
            var cdkDeployTask = _commandLineWrapper.TryRunWithResult( $"npx cdk deploy --require-approval never -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"",
                workingDirectory: cdkProjectPath,
                environmentVariables: environmentVariables,
                needAwsCredentials: true,
                redirectIO: true,
                streamOutputToInteractiveService: true);

            var cancellationTokenSource = new CancellationTokenSource();
            var retrieveStackIdTask = RetrieveStackId(cloudApplication, cancellationTokenSource.Token);

            var deploymentStartDate = DateTime.UtcNow;
            var firstCompletedTask = await Task.WhenAny(cdkDeployTask, retrieveStackIdTask);
            // Deployment end date is captured at this point after 1 of the 2 running tasks yields.
            var deploymentEndDate = DateTime.UtcNow;

            TryRunResult? cdkDeploy = null;
            if (firstCompletedTask == retrieveStackIdTask)
            {
                // If retrieveStackIdTask completes first, that means a stack was created and exists in CloudFormation.
                // We can proceed with checking for deployment failures.
                var stackId = cloudApplication.Name;
                if (!retrieveStackIdTask.IsFaulted)
                    stackId = retrieveStackIdTask.Result;
                cdkDeploy = await cdkDeployTask;
                // We recapture the deployment end date at this point after the deployment task completes.
                deploymentEndDate = DateTime.UtcNow;
                await CheckCdkDeploymentFailure(stackId, deploymentStartDate, deploymentEndDate, cdkDeploy);
            }
            else
            {
                // If cdkDeployTask completes first, that means 'cdk deploy' failed before creating a stack in CloudFormation.
                // In this case, we skip checking for deployment failures since a stack does not exist.

                cdkDeploy = cdkDeployTask.Result;
                cancellationTokenSource.Cancel();
            }

            var deploymentTotalTime = Math.Round((deploymentEndDate - deploymentStartDate).TotalSeconds, 2);
            if (cdkDeploy.ExitCode != 0)
                throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToDeployCdkApplication, $"We had an issue deploying your application to AWS. Check the deployment output for more details. Deployment took {deploymentTotalTime}s.", cdkDeploy.ExitCode);
        }

        public async Task<bool> DetermineIfCDKBootstrapShouldRun()
        {
            var cdkTemplateVersion = await _cloudFormationTemplateReader.ReadCDKTemplateVersion();
            var stack = await _awsResourceQueryer.GetCloudFormationStack(AWS.Deploy.Constants.CDK.CDKBootstrapStackName);
            if (stack == null)
            {
                _interactiveService.LogDebugMessage("CDK Bootstrap stack not found.");
                return true;
            }

            var qualiferParameter = stack.Parameters.FirstOrDefault(x => string.Equals("Qualifier", x.ParameterKey));
            if (qualiferParameter == null || string.IsNullOrEmpty(qualiferParameter.ParameterValue))
            {
                _interactiveService.LogDebugMessage("CDK Bootstrap SSM parameter store value missing.");
                return true;
            }

            var bootstrapVersionStr = await _awsResourceQueryer.GetParameterStoreTextValue($"/cdk-bootstrap/{qualiferParameter.ParameterValue}/version");
            if (string.IsNullOrEmpty(bootstrapVersionStr) ||
                !int.TryParse(bootstrapVersionStr, out var bootstrapVersion) ||
                bootstrapVersion < cdkTemplateVersion)
            {
                _interactiveService.LogDebugMessage($"CDK Bootstrap version is out of date: \"{cdkTemplateVersion}\" < \"{bootstrapVersionStr}\".");
                return true;
            }

            return false;
        }

        private async Task<string> RetrieveStackId(CloudApplication cloudApplication, CancellationToken cancellationToken)
        {
            Stack? stack = null;
            await Helpers.WaitUntil(async () =>
            {
                try
                {
                    stack = await _awsResourceQueryer.GetCloudFormationStack(cloudApplication.Name);
                    return stack != null;
                }
                catch (ResourceQueryException exception) when (exception.InnerException != null && exception.InnerException.Message.Equals($"Stack with id {cloudApplication.Name} does not exist"))
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(5), cancellationToken);

            return stack?.StackId ?? throw new ResourceQueryException(DeployToolErrorCode.FailedToRetrieveStackId, "We were unable to retrieve the CloudFormation stack identifier.");
        }

        private async Task CheckCdkDeploymentFailure(string stackId, DateTime deploymentStartDate, DateTime deploymentEndDate, TryRunResult cdkDeployResult)
        {
            try
            {
                var stackEvents = await _awsResourceQueryer.GetCloudFormationStackEvents(stackId) ?? new List<StackEvent>();

                var failedEvents = stackEvents
                    .Where(x => x.Timestamp != null && x.Timestamp.Value.ToUniversalTime() >= deploymentStartDate)
                    .Where(x =>
                        x.ResourceStatus.Equals(ResourceStatus.CREATE_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.DELETE_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.UPDATE_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.IMPORT_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.IMPORT_ROLLBACK_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.UPDATE_ROLLBACK_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.ROLLBACK_FAILED)
                    );
                if (failedEvents.Any())
                {
                    var errors = string.Join(". ", failedEvents.Reverse().Select(x => x.ResourceStatusReason));
                    throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToDeployCdkApplication, errors, cdkDeployResult.ExitCode);
                }
            }
            catch (ResourceQueryException exception) when (exception.InnerException != null && exception.InnerException.Message.Equals($"Stack [{stackId}] does not exist"))
            {
                var deploymentTotalTime = Math.Round((deploymentEndDate - deploymentStartDate).TotalSeconds, 2);
                throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToCreateCdkStack, $"A CloudFormation stack was not created. Check the deployment output for more details. Deployment took {deploymentTotalTime}s.", cdkDeployResult.ExitCode);
            }
        }

        public async Task<string> CreateCdkProject(Recommendation recommendation, OrchestratorSession session, string? saveCdkDirectoryPath = null)
        {
            string? assemblyName;
            if (string.IsNullOrEmpty(saveCdkDirectoryPath))
            {
                saveCdkDirectoryPath =
                Path.Combine(
                    _workspaceMetadata.ProjectsDirectory,
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

                assemblyName = recommendation.ProjectDefinition.AssemblyName;
            }
            else
            {
                assemblyName = _directoryManager.GetDirectoryInfo(saveCdkDirectoryPath).Name;
            }

            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException("The assembly name for the CDK deployment project cannot be null");

            _directoryManager.CreateDirectory(saveCdkDirectoryPath);

            var templateEngine = new TemplateEngine();
            await templateEngine.GenerateCdkProjectFromTemplateAsync(recommendation, session, saveCdkDirectoryPath, assemblyName);

            _interactiveService.LogDebugMessage($"Saving AWS CDK deployment project to: {saveCdkDirectoryPath}");
            return saveCdkDirectoryPath;
        }

        public void DeleteTemporaryCdkProject(string cdkProjectPath)
        {
            var parentPath = Path.GetFullPath(_workspaceMetadata.ProjectsDirectory);
            cdkProjectPath = Path.GetFullPath(cdkProjectPath);

            if (!cdkProjectPath.StartsWith(parentPath))
                return;

            try
            {
                _directoryManager.Delete(cdkProjectPath, true);
            }
            catch (Exception exception)
            {
                _interactiveService.LogDebugMessage(exception.PrettyPrint());
                _interactiveService.LogErrorMessage($"We were unable to delete the temporary project that was created for this deployment. Please manually delete it at this location: {cdkProjectPath}");
            }
        }
    }
}
