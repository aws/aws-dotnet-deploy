// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
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
        /// Gets CDK CLI version installed <see cref="workingDirectory"/> using npx command.
        /// It checks for local as well as global version
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <returns><see cref="Version"/> object wrapped in <see cref="TryGetResult{TResult}"/></returns>
        Task<TryGetResult<Version>> GetVersion(string workingDirectory);

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

        public async Task<TryGetResult<Version>> GetVersion(string workingDirectory)
        {
            const string command = "npx --no-install cdk --version";

            TryRunResult result;

            try
            {
                result = await _commandLineWrapper.TryRunWithResult(command, workingDirectory, false);
            }
            catch (Exception exception)
            {
                throw new NPMCommandFailedException($"Failed to execute {command}", exception);
            }

            var standardOut = result.StandardOut ?? "";
            var lines = standardOut.Split(Environment.NewLine).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            if (lines.Length < 1)
            {
                return TryGetResult.Failure<Version>();
            }

            var versionLine = lines.Last();

            /*
             * Split the last line which has the version
             * typical version line: 1.127.0 (build 0ea309a)
             *
             * part 0: 1.127.0
             * part 1: build
             * part 2: 0ea309a
             *
             * It could be possible we have more than 3 parts with more information but they can be ignored.
             */
            var parts = versionLine.Split(' ', '(', ')').Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
            if (parts.Length < 3)
            {
                return TryGetResult.Failure<Version>();
            }

            if (Version.TryParse(parts[0], out var version))
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
