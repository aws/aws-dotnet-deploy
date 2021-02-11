// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.Common;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestrator
{
    public class CdkAppSettingsSerializer
    {
        public string Build(CloudApplication cloudApplication, Recommendation recommendation)
        {
            // General Settings
            var settings = new Dictionary<string, object>
            {
                { nameof(recommendation.ProjectPath), recommendation.ProjectPath },
                { "StackName", cloudApplication.StackName },
                { "DockerfileDirectory",  new FileInfo(recommendation.ProjectPath).Directory.FullName }
            };

            string solutionFilePath = GetProjectSolutionFile(recommendation.ProjectPath);
            if (!string.IsNullOrEmpty(solutionFilePath))
            {
                settings["ProjectSolutionPath"] = solutionFilePath;
            }

            // Option Settings
            foreach (var optionSetting in recommendation.Recipe.OptionSettings)
            {
                settings[optionSetting.Id] = recommendation.GetOptionSettingValue(optionSetting);
            }

            return JsonConvert.SerializeObject(settings, Formatting.Indented);
        }

        private string GetProjectSolutionFile(string projectPath)
        {
            var projectDirectory = Directory.GetParent(projectPath);
            var solutionExists = false;
            while (solutionExists == false && projectDirectory != null)
            {
                var files = projectDirectory.GetFiles("*.sln");
                if (files.Length > 0)
                {
                    foreach (var solutionFile in files)
                    {
                        if (ValidateProjectInSolution(projectPath, solutionFile.FullName))
                        {
                            return solutionFile.FullName;
                        }
                    }
                }
                projectDirectory = projectDirectory.Parent;
            }
            return string.Empty;
        }

        private bool ValidateProjectInSolution(string projectPath, string solutionFile)
        {
            var projectFileName = Path.GetFileName(projectPath);
            if (string.IsNullOrWhiteSpace(solutionFile) ||
                string.IsNullOrWhiteSpace(projectFileName))
            {
                return false;
            }
            List<string> lines = File.ReadAllLines(solutionFile).ToList();
            var projectLines = lines.Where(x => x.StartsWith("Project"));
            var projectPaths = projectLines.Select(x => x.Split(',')[1].Replace('\"', ' ').Trim()).ToList();

            //Validate project exists in solution
            return projectPaths.Select(x => Path.GetFileName(x)).Where(x => x.Equals(projectFileName)).Any();
        }
    }
}
