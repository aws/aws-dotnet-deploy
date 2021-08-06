using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkProjectHandler
    {
        public Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation);
        public Task<string> CreateCdkProjectForDeployment(Recommendation recommendation, OrchestratorSession session, string? saveDirectoryPath = null);
    }

    public class CdkProjectHandler : ICdkProjectHandler
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly CdkAppSettingsSerializer _appSettingsBuilder;
        private readonly IDirectoryManager _directoryManager;

        public CdkProjectHandler(IOrchestratorInteractiveService interactiveService, ICommandLineWrapper commandLineWrapper)
        {
            _interactiveService = interactiveService;
            _commandLineWrapper = commandLineWrapper;
            _appSettingsBuilder = new CdkAppSettingsSerializer();
            _directoryManager = new DirectoryManager();
        }

        public async Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation)
        {
            var recipeInfo = $"{recommendation.Recipe.Id}_{recommendation.Recipe.Version}";
            var environmentVariables = new Dictionary<string, string>
            {
                { EnvironmentVariableKeys.AWS_EXECUTION_ENV, recipeInfo }
            };

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
                _interactiveService.LogMessageLine($"Generating a {recommendation.Recipe.Name} CDK Project");
                cdkProjectPath = await CreateCdkProjectForDeployment(recommendation, session);
            }
            
            // Write required configuration in appsettings.json
            var appSettingsBody = _appSettingsBuilder.Build(cloudApplication, recommendation, session);
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");
            using (var appSettingsFile = new StreamWriter(appSettingsFilePath))
            {
                await appSettingsFile.WriteAsync(appSettingsBody);
            }

            _interactiveService.LogMessageLine("Starting deployment of CDK Project");

            // Ensure region is bootstrapped
            await _commandLineWrapper.Run($"npx cdk bootstrap aws://{session.AWSAccountId}/{session.AWSRegion}",
                needAwsCredentials: true);

            // Handover to CDK command line tool
            // Use a CDK Context parameter to specify the settings file that has been serialized.
            var cdkDeploy = await _commandLineWrapper.TryRunWithResult( $"npx cdk deploy --require-approval never -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"",
                workingDirectory: cdkProjectPath,
                environmentVariables: environmentVariables,
                needAwsCredentials: true,
                streamOutputToInteractiveService: true);

            if (cdkDeploy.ExitCode != 0)
                throw new FailedToDeployCDKAppException("We had an issue deploying your application to AWS. Check the deployment output for more details.");
        }

        public async Task<string> CreateCdkProjectForDeployment(Recommendation recommendation, OrchestratorSession session, string? saveCdkDirectoryPath = null)
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
            await templateEngine.GenerateCDKProjectFromTemplate(recommendation, session, saveCdkDirectoryPath, assemblyName);

            return saveCdkDirectoryPath;
        }
    }
}
