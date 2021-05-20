using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Constants;
using AWS.Deploy.Shell;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkProjectHandler
    {
        public Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation);
    }

    public class CdkProjectHandler : ICdkProjectHandler
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly ICommandRunner _commandRunner;
        private readonly CdkAppSettingsSerializer _appSettingsBuilder;

        public CdkProjectHandler(IOrchestratorInteractiveService interactiveService, ICommandRunner commandRunner)
        {
            _interactiveService = interactiveService;
            _commandRunner = commandRunner;
            _appSettingsBuilder = new CdkAppSettingsSerializer();
        }

        public async Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation)
        {
            var recipeInfo = $"{recommendation.Recipe.Id}_{recommendation.Recipe.Version}";
            var environmentVariables = new Dictionary<string, string>
            {
                { EnvironmentVariable.Keys.AWS_EXECUTION_ENV, recipeInfo }
            };

            // Create a new temporary CDK project for a new deployment
            _interactiveService.LogMessageLine($"Generating a {recommendation.Recipe.Name} CDK Project");
            var cdkProjectPath = await CreateCdkProjectForDeployment(recommendation, session);

            // Write required configuration in appsettings.json
            var appSettingsBody = _appSettingsBuilder.Build(cloudApplication, recommendation);
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");
            using (var appSettingsFile = new StreamWriter(appSettingsFilePath))
            {
                await appSettingsFile.WriteAsync(appSettingsBody);
            }

            _interactiveService.LogMessageLine("Starting deployment of CDK Project");

            // Ensure region is bootstrapped
            await _commandRunner.Run($"npx cdk bootstrap aws://{session.AWSAccountId}/{session.AWSRegion}");

            // Handover to CDK command line tool
            // Use a CDK Context parameter to specify the settings file that has been serialized.
            await _commandRunner.Run( $"npx cdk deploy --require-approval never -c {Constants.CloudFormationIdentifier.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"",
                workingDirectory: cdkProjectPath,
                environmentVariables: environmentVariables);
        }

        private async Task<string> CreateCdkProjectForDeployment(Recommendation recommendation, OrchestratorSession session)
        {
            var tempDirectoryPath =
                Path.Combine(
                    Constants.CDK.ProjectsDirectory,
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectoryPath);

            var templateEngine = new TemplateEngine();
            await templateEngine.GenerateCDKProjectFromTemplate(recommendation, session, tempDirectoryPath);

            return tempDirectoryPath;
        }
    }
}
