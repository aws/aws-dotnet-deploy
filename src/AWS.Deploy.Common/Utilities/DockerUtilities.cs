// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common.Utilities
{
    /// <summary>
    ///  Utility methods for working with a recommendation's Docker configuration
    /// </summary>
    public static class DockerUtilities
    {
        /// <summary>
        /// Gets the path of a Dockerfile if it exists at the default location: "{ProjectPath}/Dockerfile"
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="fileManager">File manager, used for validating that the Dockerfile exists</param>
        /// <param name="dockerfilePath">Path to the Dockerfile, relative to the recommendation's project directory</param>
        /// <returns>True if the Dockerfile exists at the default location, false otherwise</returns>
        public static bool TryGetDefaultDockerfile(Recommendation recommendation, IFileManager? fileManager, out string dockerfilePath)
        {
            if (fileManager == null)
            {
                fileManager = new FileManager();
            }

            if (fileManager.Exists(Constants.Docker.DefaultDockerfileName, recommendation.GetProjectDirectory()))
            {
                // Set the default value to the OS-specific ".\Dockerfile"
                dockerfilePath = Path.Combine(".", Constants.Docker.DefaultDockerfileName);
                return true;
            }
            else
            {
                dockerfilePath = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Gets the path of a the project's Dockerfile if it exists, from either a user-specified or the default location
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="fileManager">File manager, used for validating that the Dockerfile exists</param>
        /// <param name="dockerfilePath">Path to a Dockerfile,which may be absolute or relative</param>
        /// <returns>True if a Dockerfile is specified for this deployment, false otherwise</returns>
        public static bool TryGetDockerfile(Recommendation recommendation, IFileManager fileManager, out string dockerfilePath)
        {
            dockerfilePath = recommendation.DeploymentBundle.DockerfilePath;

            if (!string.IsNullOrEmpty(dockerfilePath))
            {
                // Double-check that it still exists in case it was move/deleted after being specified.
                if (fileManager.Exists(dockerfilePath, recommendation.GetProjectDirectory()))
                {
                    return true;
                }
                else
                {
                    throw new InvalidFilePath(DeployToolErrorCode.InvalidFilePath, $"A dockerfile was specified at {dockerfilePath} but does not exist.");
                }
            }
            else
            {
                // Check the default location again, for the case where a file was NOT specified
                // in the option but we generated one in the default location right before calling docker build.
                var defaultExists = TryGetDefaultDockerfile(recommendation, fileManager, out dockerfilePath);
                return defaultExists;
            }
        }

        /// <summary>
        /// Gets the path of a the project's Dockerfile if it exists, from either a user-specified or the default location
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="fileManager">File manager, used for validating that the Dockerfile exists</param>
        /// <param name="absoluteDockerfilePath">Absolute path to the Dockerfile</param>
        /// <returns>True if a Dockerfile is specified for this deployment, false otherwise</returns>
        public static bool TryGetAbsoluteDockerfile(Recommendation recommendation, IFileManager fileManager, IDirectoryManager directoryManager, out string absoluteDockerfilePath)
        {
            var dockerfileExists = TryGetDockerfile(recommendation, fileManager, out var dockerfilePath);

            if (dockerfileExists)
            {
                absoluteDockerfilePath = Path.IsPathRooted(dockerfilePath)
                ? dockerfilePath
                : directoryManager.GetAbsolutePath(recommendation.GetProjectDirectory(), dockerfilePath);
            }
            else
            {
                absoluteDockerfilePath = string.Empty;
            }

            return dockerfileExists;
        }
    }
}
