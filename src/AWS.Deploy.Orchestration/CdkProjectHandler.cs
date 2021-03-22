using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes.CDK.Common;

namespace AWS.Deploy.Orchestration
{
    public interface ICdkProjectHandler
    {
        public Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation);
    }

    public class CdkProjectHandler : ICdkProjectHandler
    {
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly CdkAppSettingsSerializer _appSettingsBuilder;

        public CdkProjectHandler(IOrchestratorInteractiveService interactiveService, ICommandLineWrapper commandLineWrapper)
        {
            _interactiveService = interactiveService;
            _commandLineWrapper = commandLineWrapper;
            _appSettingsBuilder = new CdkAppSettingsSerializer();
        }

        public async Task CreateCdkDeployment(OrchestratorSession session, CloudApplication cloudApplication, Recommendation recommendation)
        {
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
            await _commandLineWrapper.Run($"npx cdk bootstrap aws://{session.AWSAccountId}/{session.AWSRegion}");

            // Handover to CDK command line tool
            // Use a CDK Context parameter to specify the settings file that has been serialized.
            await _commandLineWrapper.Run( $"npx cdk deploy --require-approval never -c {CloudFormationIdentifierConstants.SETTINGS_PATH_CDK_CONTEXT_PARAMETER}=\"{appSettingsFilePath}\"", cdkProjectPath);
        }

        private async Task<string> CreateCdkProjectForDeployment(Recommendation recommendation, OrchestratorSession session)
        {
            var tempDirectoryPath =
                Path.Combine(
                    CDKConstants.ProjectsDirectory,
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectoryPath);

            var templateEngine = new TemplateEngine();
            await templateEngine.GenerateCDKProjectFromTemplate(recommendation, session, tempDirectoryPath);

            return tempDirectoryPath;
        }
    }
}
