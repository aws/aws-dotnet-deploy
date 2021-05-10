// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration.CDK
{
    /// <summary>
    /// Abstracts low level node package manager commands to list and install CDK CLI
    /// in high level APIs.
    /// </summary>
    public interface ICDKInstaller
    {
        /// <summary>
        /// Gets CDK CLI version installed in global node_modules using npm command.
        /// </summary>
        /// <returns><see cref="Version"/> object wrapped in <see cref="TryGetResult{TResult}"/></returns>
        Task<TryGetResult<Version>> GetGlobalVersion();

        /// <summary>
        /// Gets CDK CLI version installed <see cref="workingDirectory"/> using npm command.
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <returns><see cref="Version"/> object wrapped in <see cref="TryGetResult{TResult}"/></returns>
        Task<TryGetResult<Version>> GetLocalVersion(string workingDirectory);

        /// <summary>
        /// Installs local version of the AWS SDK CLI in the given working directory
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="version">CDK CLI version to update</param>
        Task Install(string workingDirectory, Version version);
    }

    public class CDKInstaller : ICDKInstaller
    {
        private readonly ICommandLineWrapper _commandLineWrapper;

        public CDKInstaller(ICommandLineWrapper commandLineWrapper)
        {
            _commandLineWrapper = commandLineWrapper;
        }

        public Task<TryGetResult<Version>> GetGlobalVersion()
        {
            return GetVersion(string.Empty, true);
        }

        public Task<TryGetResult<Version>> GetLocalVersion(string workingDirectory)
        {
            return GetVersion(workingDirectory, false);
        }

        private async Task<TryGetResult<Version>> GetVersion(string workingDirectory, bool checkGlobal)
        {
            var command = new StringBuilder("npm list aws-cdk");
            if (checkGlobal)
            {
                command.Append(" --global");
            }

            TryRunResult result;

            try
            {
                result = await _commandLineWrapper.TryRunWithResult(command.ToString(), workingDirectory, false);
            }
            catch (Exception exception)
            {
                throw new NPMCommandFailedException($"Failed to execute {command}", exception);
            }

            /*
             * A typical Standard out looks like with version information in line 2
             *
             * > npm list aws-cdk --global
             * C:\Users\user\AppData\Roaming\npm
             * `-- aws-cdk@0.0.0
             */
            var standardOut = result.StandardOut ?? "";
            var lines = standardOut.Split('\n'); // Environment.NewLine doesn't work here.
            if (lines.Length < 2)
            {
                return TryGetResult.Failure<Version>();
            }

            var versionLine = lines[1];

            /*
             * Split line 2 in parts so that we have package name and version in separate parts
             *
             * part 0: `--
             * part 1: aws-cdk
             * part 2: 0.0.0
             *
             * It could be possible we have more than 3 parts with more information but they can be ignored.
             */
            var parts = versionLine.Split(' ', '@');
            if (parts.Length < 3)
            {
                return TryGetResult.Failure<Version>();
            }

            /*
             * Make sure that we are checking aws-cdk only
             * If a customer has plugin which depends on aws-cdk and then customer removes aws-cdk
             * Plugin version information is shown which can lead to a false positive.
             *
             * > npm list aws-cdk --global
             * C:\Users\user\AppData\Roaming\npm
             * `-- cdk-assume-role-credential-plugin@1.0.0 (git+https://github.com/aws-samples/cdk-assume-role-credential-plugin.git#5167c798a50bc9c96a9d660b28306428be4e99fb)
             */
            if (!parts[1].Equals("aws-cdk"))
            {
                return TryGetResult.Failure<Version>();
            }

            if (Version.TryParse(parts[2], out var version))
            {
                return TryGetResult.FromResult(version);
            }

            return TryGetResult.Failure<Version>();
        }

        public async Task Install(string workingDirectory, Version version)
        {
            await _commandLineWrapper.Run($"npm install aws-cdk@{version}", workingDirectory, false);
        }
    }
}
