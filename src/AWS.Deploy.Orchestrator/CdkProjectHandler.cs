using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.Utilities;
using AWS.DeploymentCommon;

namespace AWS.Deploy.Orchestrator
{
    public interface ICdkProjectHandler
    {
        public Task CreateCdkDeployment(string cloudApplicationName, Recommendation recommendation);
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

        public async Task CreateCdkDeployment(string cloudApplicationName, Recommendation recommendation)
        {
            // Create a new temporary CDK project for a new deployment
            _interactiveService.LogMessageLine($"Generating a {recommendation.Recipe.Name} CDK Project");
            var cdkProjectPath = await CreateCdkProjectForDeployment(recommendation);

            // Write required configuration in appsettings.json
            var appSettingsBody = _appSettingsBuilder.Build(cloudApplicationName, recommendation);
            var appSettingsFilePath = Path.Combine(cdkProjectPath, "appsettings.json");
            using (StreamWriter appSettingsFile = new StreamWriter(appSettingsFilePath))
            {
                await appSettingsFile.WriteAsync(appSettingsBody);
            }

            // Handover to CDK command line tool
            var commands = new List<string> { "cdk deploy --require-approval never" };
            _commandLineWrapper.Run(commands, cdkProjectPath);
        }

        private async Task<string> CreateCdkProjectForDeployment(Recommendation recommendation)
        {
            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), "AWS.Deploy", "Projects", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectoryPath);

            var templateEngine = new TemplateEngine();
            await templateEngine.GenerateCDKProjectFromTemplate(recommendation, tempDirectoryPath);

            return tempDirectoryPath;
        }
    }
}
