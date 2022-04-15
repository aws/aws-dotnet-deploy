// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Utilities;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This validates that the Dockerfile is within the build context.
    ///
    /// Per https://docs.docker.com/engine/reference/commandline/build/#text-files
    /// "The path must be to a file within the build context."
    /// </summary>
    public class DockerfilePathValidator : IRecipeValidator
    {
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;

        public DockerfilePathValidator(IDirectoryManager directoryManager, IFileManager fileManager)
        {
            _directoryManager = directoryManager;
            _fileManager = fileManager;
        }

        public Task<ValidationResult> Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext)
        {
            DockerUtilities.TryGetAbsoluteDockerfile(recommendation, _fileManager, _directoryManager, out var absoluteDockerfilePath);

            // Docker execution directory has its own typehint, which sets the value here
            var dockerExecutionDirectory = recommendation.DeploymentBundle.DockerExecutionDirectory;

            // We're only checking the interaction here against a user-specified file and execution directory,
            // it's still possible that we generate a dockerfile and/or compute the execution directory later.
            if (absoluteDockerfilePath == string.Empty || dockerExecutionDirectory == string.Empty)
            {
                return ValidationResult.ValidAsync();
            }

            // Convert both to absolute paths in case they were specified relative to the project directory
            var projectPath = recommendation.GetProjectDirectory();

            var absoluteDockerExecutionDirectory = Path.IsPathRooted(dockerExecutionDirectory)
                            ? dockerExecutionDirectory
                            : _directoryManager.GetAbsolutePath(projectPath, dockerExecutionDirectory);

            if (!_directoryManager.ExistsInsideDirectory(absoluteDockerExecutionDirectory, absoluteDockerfilePath))
            {
                return ValidationResult.FailedAsync($"The specified Dockerfile \"{absoluteDockerfilePath}\" is not located within " +
                    $"the specified Docker execution directory \"{dockerExecutionDirectory}\"");
            }

            return ValidationResult.ValidAsync();
        }
    }
}
