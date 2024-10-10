// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// The container for the deployment bundle used by an application.
    /// </summary>
    public class DeploymentBundle
    {
        /// <summary>
        /// The directory from which the docker build command will be executed.
        /// </summary>
        public string DockerExecutionDirectory { get; set; } = "";

        /// <summary>
        /// The list of additional dotnet publish args passed to the target application.
        /// </summary>
        public string DockerBuildArgs { get; set; } = "";

        /// <summary>
        /// The path to the Dockerfile. This can either be an absolute path or relative to the project directory.
        /// </summary>
        public string DockerfilePath { get; set; } = "";

        /// <summary>
        /// The HTTP port to expose in the container.
        /// </summary>
        public int DockerfileHttpPort { get; set; } = 8080;

        /// <summary>
        /// The ECR Repository Name where the docker image will be pushed to.
        /// </summary>
        public string ECRRepositoryName { get; set; } = "";

        /// <summary>
        /// The ECR Image Tag of the docker image.
        /// </summary>
        public string ECRImageTag { get; set; } = "";

        /// <summary>
        /// The path of the zip file containing the assemblies produced by the dotnet publish command.
        /// </summary>
        public string DotnetPublishZipPath { get; set; } = "";

        /// <summary>
        /// The directory containing the assemblies produced by the dotnet publish command.
        /// </summary>
        public string DotnetPublishOutputDirectory { get; set; } = "";

        /// <summary>
        /// The build configuration to use for the dotnet build.
        /// </summary>
        public string DotnetPublishBuildConfiguration { get; set; } = "Release";

        /// <summary>
        /// Publishing your app as self-contained produces an application that includes the .NET runtime and libraries.
        /// Users can run it on a machine that doesn't have the .NET runtime installed.
        /// </summary>
        public bool DotnetPublishSelfContainedBuild { get; set; } = false;

        /// <summary>
        /// The list of additional dotnet publish args passed to the target application.
        /// </summary>
        public string DotnetPublishAdditionalBuildArguments { get; set; } = "";

        /// <summary>
        /// The CPU architecture of the environment to create.
        /// </summary>
        public SupportedArchitecture EnvironmentArchitecture { get; set; } = SupportedArchitecture.X86_64;
    }
}
