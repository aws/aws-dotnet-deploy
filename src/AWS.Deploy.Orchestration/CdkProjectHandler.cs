using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkProjectHandler
    {
        Task<string> ConfigureCdkProject(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation);
        string CreateCdkProject(Recommendation recommendation, OrchestratorSession session, string? saveDirectoryPath = null);
        Task DeployCdkProject(OrchestratorSession session, CloudApplication cloudApplication, string cdkProjectPath, Recommendation recommendation);
        void DeleteTemporaryCdkProject(string cdkProjectPath);
        Task<string> PerformCdkDiff(string cdkProjectPath, CloudApplication cloudApplication);
    }

    public class CdkProjectHandler : ICdkProjectHandler
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly CdkAppSettingsSerializer _appSettingsBuilder;
        private readonly IDirectoryManager _directoryManager;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IFileManager _fileManager;

        public CdkProjectHandler(
            IOrchestratorInteractiveService interactiveService,
            ICommandLineWrapper commandLineWrapper,
            IAWSResourceQueryer awsResourceQueryer,
            IFileManager fileManager)
        {
            _interactiveService = interactiveService;
            _commandLineWrapper = commandLineWrapper;
            _awsResourceQueryer = awsResourceQueryer;
            _appSettingsBuilder = new CdkAppSettingsSerializer();
            _directoryManager = new DirectoryManager();
            _fileManager = fileManager;
        }

        public async Task<string> ConfigureCdkProject(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation)
        {
            string? cdkProjectPath;
            if (recommendation.Recipe.PersistedDeploymentProject)
            {
                if (string.IsNullOrEmpty(recommendation.Recipe.RecipePath))
                    throw new InvalidOperationException($"{nameof(recommendation.Recipe.RecipePath)} cannot be null");

                // The CDK deployment project is already saved in the same directory.
                cdkProjectPath = _directoryManager.GetDirectoryInfo(recommendation.Recipe.RecipePath).Parent.FullName;
            }
            else
            {
                // Create a new temporary CDK project for a new deployment
                _interactiveService.LogMessageLine("Generating AWS Cloud Development Kit (AWS CDK) deployment project");
                cdkProjectPath = CreateCdkProject(recommendation, session);
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
                throw new FailedToRunCDKDiffException(DeployToolErrorCode.FailedToRunCDKDiff, "The CDK Diff command encountered an error and failed.");

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

            _interactiveService.LogMessageLine("Deploying AWS CDK project");

            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");

            // Ensure region is bootstrapped
            var cdkBootstrap = await _commandLineWrapper.TryRunWithResult($"npx cdk bootstrap aws://{session.AWSAccountId}/{session.AWSRegion} -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\" --template \"{Constants.CDK.CDKBootstrapTemplatePath}\"",
                workingDirectory: cdkProjectPath,
                needAwsCredentials: true,
                redirectIO: true,
                streamOutputToInteractiveService: true);

            if (cdkBootstrap.ExitCode != 0)
                throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToRunCDKBootstrap, "The AWS CDK Bootstrap, which is the process of provisioning initial resources for the deployment environment, has failed. Please review the output above for additional details [and check out our troubleshooting guide for the most common failure reasons]. You can learn more about CDK bootstrapping at https://docs.aws.amazon.com/cdk/v2/guide/bootstrapping.html.");


            var deploymentStartDate = DateTime.Now;
            // Handover to CDK command line tool
            // Use a CDK Context parameter to specify the settings file that has been serialized.
            var cdkDeploy = await _commandLineWrapper.TryRunWithResult( $"npx cdk deploy --require-approval never -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"",
                workingDirectory: cdkProjectPath,
                environmentVariables: environmentVariables,
                needAwsCredentials: true,
                redirectIO: true,
                streamOutputToInteractiveService: true);

            await CheckCdkDeploymentFailure(cloudApplication, deploymentStartDate);

            if (cdkDeploy.ExitCode != 0)
                throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToDeployCdkApplication, "We had an issue deploying your application to AWS. Check the deployment output for more details.");
        }

        private async Task CheckCdkDeploymentFailure(CloudApplication cloudApplication, DateTime deploymentStartDate)
        {
            try
            {
                var stackEvents = await _awsResourceQueryer.GetCloudFormationStackEvents(cloudApplication.Name);
                
                var failedEvents = stackEvents
                    .Where(x => x.Timestamp >= deploymentStartDate)
                    .Where(x =>
                        x.ResourceStatus.Equals(ResourceStatus.CREATE_FAILED) ||
                        x.ResourceStatus.Equals(ResourceStatus.UPDATE_FAILED)
                    );
                if (failedEvents.Any())
                {
                    var errors = string.Join(". ", failedEvents.Reverse().Select(x => x.ResourceStatusReason));
                    throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToDeployCdkApplication, errors);
                }
            }
            catch (AmazonCloudFormationException exception) when (exception.ErrorCode.Equals("ValidationError") && exception.Message.Equals($"Stack [{cloudApplication.Name}] does not exist"))
            {
                throw new FailedToDeployCDKAppException(DeployToolErrorCode.FailedToCreateCdkStack, "A CloudFormation stack was not created. Check the deployment output for more details.");
            }
        }

        public string CreateCdkProject(Recommendation recommendation, OrchestratorSession session, string? saveCdkDirectoryPath = null)
        {
            string? assemblyName;
            if (string.IsNullOrEmpty(saveCdkDirectoryPath))
            {
                saveCdkDirectoryPath =
                Path.Combine(
                    Constants.CDK.ProjectsDirectory,
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
            templateEngine.GenerateCDKProjectFromTemplate(recommendation, session, saveCdkDirectoryPath, assemblyName);

            _interactiveService.LogDebugLine($"Saving AWS CDK deployment project to: {saveCdkDirectoryPath}");
            return saveCdkDirectoryPath;
        }

        public void DeleteTemporaryCdkProject(string cdkProjectPath)
        {
            var parentPath = Path.GetFullPath(Constants.CDK.ProjectsDirectory);
            cdkProjectPath = Path.GetFullPath(cdkProjectPath);

            if (!cdkProjectPath.StartsWith(parentPath))
                return;

            try
            {
                _directoryManager.Delete(cdkProjectPath, true);
            }
            catch (Exception exception)
            {
                _interactiveService.LogDebugLine(exception.PrettyPrint());
                _interactiveService.LogErrorMessageLine($"We were unable to delete the temporary project that was created for this deployment. Please manually delete it at this location: {cdkProjectPath}");
            }
        }
    }
}
