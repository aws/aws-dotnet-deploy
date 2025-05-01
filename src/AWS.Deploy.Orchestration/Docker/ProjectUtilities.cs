// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.Orchestration.Docker
{
    public class ProjectUtilities
    {
        private const string DockerFileConfig = "AWS.Deploy.Orchestration.Properties.DockerFileConfig.json";
        private const string DockerfileTemplate = "AWS.Deploy.Orchestration.Docker.Templates.Dockerfile.template";
        private const string DockerfileTemplate_Net6 = "AWS.Deploy.Orchestration.Docker.Templates.Dockerfile.Net6.template";

        /// <summary>
        /// Retrieves the Docker File Config
        /// </summary>
        internal static string ReadDockerFileConfig()
        {
            var template = Assembly.GetExecutingAssembly().ReadEmbeddedFile(DockerFileConfig);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new DockerEngineException(DeployToolErrorCode.UnableToMapProjectToDockerImage, $"The DockerEngine could not find the embedded config file responsible for mapping projects to Docker images.");
            }

            return template;
        }

        /// <summary>
        /// Reads dockerfile template file
        /// </summary>
        internal static string ReadTemplate(string? targetFramework)
        {
            string templateLocation;
            switch (targetFramework)
            {
                case "net6.0":
                case "net5.0":
                case "netcoreapp3.1":
                    templateLocation = DockerfileTemplate_Net6;
                    break;

                default:
                    templateLocation = DockerfileTemplate;
                    break;
            }
            var template = Assembly.GetExecutingAssembly().ReadEmbeddedFile(templateLocation);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new DockerFileTemplateException(DeployToolErrorCode.DockerFileTemplateNotFound, "The Dockerfile template for the project was not found.");
            }

            return template;
        }
    }
}
