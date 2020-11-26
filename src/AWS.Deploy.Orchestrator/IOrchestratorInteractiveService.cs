namespace AWS.Deploy.Orchestrator
{
    public interface IOrchestratorInteractiveService
    {
        void LogErrorMessageLine(string message);

        void LogMessageLine(string message);
    }
}
