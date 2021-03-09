// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Reflection;
using AWS.Deploy.Common.Extensions;

namespace AWS.Deploy.DockerEngine
{
    public class ProjectUtilities
    {
        private const string DockerFileConfig = "AWS.Deploy.DockerEngine.Properties.DockerFileConfig.json";
        private const string DockerfileTemplate = "AWS.Deploy.DockerEngine.Templates.Dockerfile.template";

        /// <summary>
        /// Retrieves the Docker File Config
        /// </summary>
        internal static string ReadDockerFileConfig()
        {
            var template = Assembly.GetExecutingAssembly().ReadEmbeddedFile(DockerFileConfig);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new DockerEngineException($"The DockerEngine could not find the embedded config file responsible for mapping projects to Docker images.");
            }

            return template;
        }

        /// <summary>
        /// Reads dockerfile template file
        /// </summary>
        internal static string ReadTemplate()
        {
            var template = Assembly.GetExecutingAssembly().ReadEmbeddedFile(DockerfileTemplate);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new DockerFileTemplateException("The Dockerfile template for the project was not found.");
            }

            return template;
        }
    }
}
