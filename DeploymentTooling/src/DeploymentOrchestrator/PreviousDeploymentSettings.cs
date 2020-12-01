using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AWS.DeploymentOrchestrator
{
    public class PreviousDeploymentSettings
    {
        public const string DEFAULT_FILE_NAME = "aws-netsuite-deployment.json";

        public string Profile { get; set; }
        public string Region { get; set; }

        public IList<DeploymentSettings> Deployments { get; set; } = new List<DeploymentSettings>();
        
        public string[] GetDeploymentNames()
        {
            var names = new List<string>();

            foreach(var deployment in Deployments)
            {
                names.Add(deployment.StackName);
            }

            return names.ToArray();
        }

        public class DeploymentSettings
        {
            public string StackName { get; set; }
            public string RecipeId { get; set; }
            public IDictionary<string, object> RecipeOverrideSettings { get; set; } = new Dictionary<string, object>();
        }

        public static PreviousDeploymentSettings ReadSettings(string projectPath, string configFile)
        {
            var fullPath = GetFullConfigFilePath(projectPath, configFile);
            if (!File.Exists(fullPath))
                return new PreviousDeploymentSettings();

            return ReadSettings(fullPath);
        }

        public static PreviousDeploymentSettings ReadSettings(string filePath)
        {
            return JsonSerializer.Deserialize<PreviousDeploymentSettings>(File.ReadAllText(filePath));
        }

        public void SaveSettings(string projectPath, string configFile)
        {
            SaveSettings(GetFullConfigFilePath(projectPath, configFile));
        }

        public void SaveSettings(string filePath)
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true
            };

            var json = JsonSerializer.Serialize<PreviousDeploymentSettings>(this, jsonOptions);
            File.WriteAllText(filePath, json);
        }

        public static string GetFullConfigFilePath(string projectPath, string configFile)
        {
            var fullPath = string.IsNullOrEmpty(configFile) ? Path.Combine(projectPath, DEFAULT_FILE_NAME) : Path.Combine(projectPath, configFile);
            return fullPath;
        }

    }
}
