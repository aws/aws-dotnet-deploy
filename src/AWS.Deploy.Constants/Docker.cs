// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Constants
{
    internal class Docker
    {
        /// <summary>
        /// Name of the default Dockerfile that the deployment tool attempts to detect in the project directory
        /// </summary>
        public static readonly string DefaultDockerfileName = "Dockerfile";

        /// <summary>
        /// Id for the Docker Execution Directory recipe option
        /// </summary>
        public const string DockerExecutionDirectoryOptionId = "DockerExecutionDirectory";

        /// <summary>
        /// Id for the Dockerfile Path recipe option
        /// </summary>
        public const string DockerfileOptionId = "DockerfilePath";

        /// <summary>
        /// Id for the Docker Build Args recipe option
        /// </summary>
        public const string DockerBuildArgsOptionId = "DockerBuildArgs";

        /// <summary>
        /// Id for the ECR Repository Name recipe option
        /// </summary>
        public const string ECRRepositoryNameOptionId = "ECRRepositoryName";

        /// <summary>
        /// Id for the Docker Image Tag recipe option
        /// </summary>
        public const string ImageTagOptionId = "ImageTag";
    }
}
