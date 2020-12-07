// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Reflection;

namespace AWS.Deploy.DockerEngine
{
    public class ProjectUtilities
    {
        private const string DockerFileConfig = "AWS.Deploy.DockerEngine.Properties.DockerFileConfig.json";
        private const string DockerfileTemplate = "AWS.Deploy.DockerEngine.Templates.Dockerfile.template";

        private static string GetEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var result = "";
            using (Stream resource = assembly.GetManifestResourceStream(resourceName))
            {
                if (resource == null)
                {
                    throw new FileNotFoundException($"The resource {resourceName} was not found in the project.");
                }

                using (StreamReader reader = new StreamReader(resource))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the Docker File Config
        /// </summary>
        internal static string ReadDockerFileConfig()
        {
            var template = GetEmbeddedResource(DockerFileConfig);

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
            var template = GetEmbeddedResource(DockerfileTemplate);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new DockerFileTemplateException("The Dockerfile template for the project was not found.");
            }

            return template;
        }
    }
}
