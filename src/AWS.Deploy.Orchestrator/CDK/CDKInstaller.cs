// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.Orchestrator.CDK
{
    public interface ICDKInstaller
    {
        Task<TryGetResult<Version>> GetVersion(string workingDirectory, bool checkGlobal);
        Task Install(string workingDirectory, Version version);
    }

    /// <summary>
    /// Abstracts low level node package manager commands to list and install AWS CDK CLI
    /// in high level APIs.
    /// </summary>
    public class CDKInstaller : ICDKInstaller
    {
        private readonly ICommandLineWrapper _commandLineWrapper;

        public CDKInstaller(ICommandLineWrapper commandLineWrapper)
        {
            _commandLineWrapper = commandLineWrapper;
        }

        /// <summary>
        /// Gets AWS CDK CLI version using npm command.
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="checkGlobal">If true, global installation of AWS CDK CLI is checked.</param>
        /// <returns></returns>
        public async Task<TryGetResult<Version>> GetVersion(string workingDirectory, bool checkGlobal)
        {
            var command = new StringBuilder("npm list aws-cdk");
            if (checkGlobal)
            {
                command.Append(" --global");
            }

            var result = await _commandLineWrapper.TryRunWithResult(command.ToString(), workingDirectory, false);
            var standardOut = result.StandardOut;
            var lines = standardOut.Split(Environment.NewLine);
            if (lines.Length < 2)
            {
                return TryGetResult.Failure<Version>();
            }

            var versionLine = lines[1];
            var parts = versionLine.Split(' ', '@');
            if (parts.Length < 3)
            {
                return TryGetResult.Failure<Version>();
            }

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

        /// <summary>
        /// Installs local version of the AWS SDK CLI in the given working directory
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="version">AWS CDK CLI version to update</param>
        /// <returns></returns>
        public async Task Install(string workingDirectory, Version version)
        {
            await _commandLineWrapper.Run($"npm install aws-cdk@{version}", workingDirectory, false);
        }
    }
}
