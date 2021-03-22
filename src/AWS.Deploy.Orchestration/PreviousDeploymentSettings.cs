// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class PreviousDeploymentSettings
    {
        public const string DEFAULT_FILE_NAME = "aws-netsuite-deployment.json";

        public string Profile { get; set; }
        public string Region { get; set; }

        public static PreviousDeploymentSettings ReadSettings(string projectPath, string configFile)
        {
            var fullPath = GetFullConfigFilePath(projectPath, configFile);
            if (!File.Exists(fullPath))
                return new PreviousDeploymentSettings();

            return ReadSettings(fullPath);
        }

        public static PreviousDeploymentSettings ReadSettings(string filePath)
        {
            return JsonConvert.DeserializeObject<PreviousDeploymentSettings>(File.ReadAllText(filePath));
        }

        public void SaveSettings(string projectPath, string configFile)
        {
            SaveSettings(GetFullConfigFilePath(projectPath, configFile));
        }

        public void SaveSettings(string filePath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static string GetFullConfigFilePath(string projectPath, string configFile)
        {
            var fullPath = string.IsNullOrEmpty(configFile) ? Path.Combine(projectPath, DEFAULT_FILE_NAME) : Path.Combine(projectPath, configFile);
            return fullPath;
        }
    }
}
