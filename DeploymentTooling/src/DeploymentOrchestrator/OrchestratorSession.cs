using System.IO;
using Amazon.Runtime;

namespace AWS.DeploymentOrchestrator
{
    public class OrchestratorSession
    {
        public OrchestratorSession(string projectPath, string configFile)
        {
            ProjectPath = projectPath;
            ConfigFile = configFile ?? PreviousDeploymentSettings.DEFAULT_FILE_NAME;
        }


        public string ProjectPath { get; }
        public string ConfigFile { get; }


        public string AWSProfileName { get; set; }
        public AWSCredentials AWSCredentials { get; set; }
        public string AWSRegion { get; set; }
        
        public string CloudApplicationName { get; set; }

        private string _projectDirectory;

        public string ProjectDirectory
        {
            get => _projectDirectory;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _projectDirectory = Directory.GetCurrentDirectory();
                }
                else if (File.Exists(value))
                {
                    _projectDirectory = Directory.GetParent(value).FullName;
                }
                else
                {
                    _projectDirectory = value;
                }
            }
        }
    }
}
