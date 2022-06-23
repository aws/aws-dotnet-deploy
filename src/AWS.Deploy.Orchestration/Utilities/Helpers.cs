// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Orchestration.Utilities
{
    /// <summary>
    /// This class holds static helper methods
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Wait for <see cref="timeout"/> TimeSpan until <see cref="Predicate{T}"/> isn't satisfied
        /// </summary>
        /// <param name="predicate">Termination condition for breaking the wait loop</param>
        /// <param name="frequency">Interval between the two executions of the task</param>
        /// <param name="timeout">Interval for timeout, if timeout passes, methods throws <see cref="TimeoutException"/></param>
        /// <exception cref="TimeoutException">Throws when timeout passes</exception>
        public static async Task WaitUntil(Func<Task<bool>> predicate, TimeSpan frequency, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && !await predicate())
                {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// This method returns the deployment tool workspace directory to create the CDK app during deployment.
        /// It will first look for AWS_DOTNET_DEPLOYTOOL_WORKSPACE environment variable set by the user. It will be used as the deploy tool workspace if it points to a valid directory whithout whitespace characters in its path.
        /// If the environment variable is set, it will also create a temp directory inside the workspace and set process scoped TEMP and TMP environment variables that point to the temp directory.
        /// This additional configuration is required due to a known issue in the CDK - https://github.com/aws/aws-cdk/issues/2532
        /// If the override is not present, then it defaults to USERPROFILE/.aws-dotnet-deploy.
        /// It will throw an exception if the USERPROFILE contains a whitespace character.
        /// </summary>
        public static string GetDeployToolWorkspaceDirectoryRoot(string userProfilePath, IDirectoryManager directoryManager, IEnvironmentVariableManager environmentVariableManager)
        {
            var overridenWorkspace = environmentVariableManager.GetEnvironmentVariable(Constants.CLI.WORKSPACE_ENV_VARIABLE);

            var defaultWorkSpace = Path.Combine(userProfilePath, ".aws-dotnet-deploy");

            if (overridenWorkspace == null)
            {
                return !defaultWorkSpace.Contains(" ")
                    ? defaultWorkSpace
                    : throw new InvalidDeployToolWorkspaceException(DeployToolErrorCode.InvalidDeployToolWorkspace, $"The USERPROFILE path ({userProfilePath}) contains a whitespace character and cannot be used as a workspace by the deployment tool. " +
                    $"Please refer to the troubleshooting guide for setting the {Constants.CLI.WORKSPACE_ENV_VARIABLE} that specifies an alternative workspace directory.");
            }

            if (!directoryManager.Exists(overridenWorkspace))
            {
                throw new InvalidDeployToolWorkspaceException(DeployToolErrorCode.InvalidDeployToolWorkspace,
                    $"The {Constants.CLI.WORKSPACE_ENV_VARIABLE} environment variable has been set to \"{overridenWorkspace}\" but it does not point to a valid directory.");
            }

            if (overridenWorkspace.Contains(" "))
            {
                throw new InvalidDeployToolWorkspaceException(DeployToolErrorCode.InvalidDeployToolWorkspace,
                    $"The {Constants.CLI.WORKSPACE_ENV_VARIABLE} environment variable ({overridenWorkspace}) contains a whitespace character and cannot be used as a workspace by the deployment tool.");
            }

            var tempDir = Path.Combine(overridenWorkspace, "temp");
            if (!directoryManager.Exists(tempDir))
            {
                directoryManager.CreateDirectory(tempDir);
            }

            // This is done to override the C:\Users\<username>\AppData\Local\Temp directory.
            // There is a known issue with CDK where it cannot access the Temp directory if the username contains a whitespace.
            environmentVariableManager.SetEnvironmentVariable("TMP", tempDir);
            environmentVariableManager.SetEnvironmentVariable("TEMP", tempDir);

            return overridenWorkspace;
        }
    }
}
