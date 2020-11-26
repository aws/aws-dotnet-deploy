namespace AWS.DeploymentOrchestrator
{
    public interface IOrchestratorInteractiveService
    {
        void LogErrorMessageLine(string message);

        void LogMessageLine(string message);
    }
}
