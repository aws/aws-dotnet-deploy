// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using Amazon.Runtime;

namespace AWS.Deploy.Orchestrator
{
    public class OrchestratorSession
    {
        public string ProjectPath { get; set; }
        public string ConfigFile { get; set; } = PreviousDeploymentSettings.DEFAULT_FILE_NAME;
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
