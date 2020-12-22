using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.DeploymentCommon;

namespace AWS.Deploy.Orchestrator
{
    public interface ICdkProjectHandler
    {
        public Task CreateCdkDeployment(OrchestratorSession session, string cloudApplicationName, Recommendation recommendation);
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

        public async Task CreateCdkDeployment(OrchestratorSession session, string cloudApplicationName, Recommendation recommendation)
        {
            // Create a new temporary CDK project for a new deployment
            _interactiveService.LogMessageLine($"Generating a {recommendation.Recipe.Name} CDK Project");
            var cdkProjectPath = await CreateCdkProjectForDeployment(recommendation, session);

            // Write required configuration in appsettings.json
            var appSettingsBody = _appSettingsBuilder.Build(cloudApplicationName, recommendation);
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");
            using (var appSettingsFile = new StreamWriter(appSettingsFilePath))
            {
                await appSettingsFile.WriteAsync(appSettingsBody);
            }

            _interactiveService.LogMessageLine("Starting deployment of CDK Project");

            // install cdk locally if needed
            if (!session.SystemCapabilities.CdkNpmModuleInstalledGlobally)
            {
                await _commandLineWrapper.Run("npm install aws-cdk", cdkProjectPath, streamOutputToInteractiveService: false);
            }

            // Handover to CDK command line tool
            await _commandLineWrapper.Run( "npx cdk deploy --require-approval never", cdkProjectPath);
        }

        private async Task<string> CreateCdkProjectForDeployment(Recommendation recommendation, OrchestratorSession session)
        {
            var tempDirectoryPath =
                Path.Combine(
                    Path.GetTempPath(),
                    "AWS.Deploy",
                    "Projects",
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectoryPath);

            var templateEngine = new TemplateEngine();
            await templateEngine.GenerateCDKProjectFromTemplate(recommendation, session, tempDirectoryPath);

            return tempDirectoryPath;
        }
    }
}
