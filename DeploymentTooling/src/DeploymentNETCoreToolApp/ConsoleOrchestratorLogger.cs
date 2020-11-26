using AWS.DeploymentOrchestrator;

namespace AWS.DeploymentNETCoreToolApp
{
    public class ConsoleOrchestratorLogger : IOrchestratorInteractiveService
    {
        private readonly IToolInteractiveService _interactiveService;

        public ConsoleOrchestratorLogger(IToolInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public void LogErrorMessageLine(string message)
        {
            _interactiveService.WriteErrorLine(message);
        }

        public void LogMessageLine(string message)
        {
            _interactiveService.WriteLine(message);
        }
    }
}
