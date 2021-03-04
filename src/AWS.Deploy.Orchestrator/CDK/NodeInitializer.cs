// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestrator.Utilities;

namespace AWS.Deploy.Orchestrator.CDK
{
    public interface INodeInitializer
    {
        /// <summary>
        /// Checks whether package.json file exists at given working directory or not.
        /// If there exists a package.json file, it is assumed to have node initialized.
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <returns>True, if package.json exists at <see cref="workingDirectory"/></returns>
        bool IsInitialized(string workingDirectory);

        /// <summary>
        /// Initializes node app at <see cref="workingDirectory"/>
        /// </summary>
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="cdkVersion">Version of AWS CDK CLI</param>
        Task Initialize(string workingDirectory, Version cdkVersion);
    }

    /// <summary>
    /// Orchestrates local node app.
    /// It makes sure that a local node application is initialized in order to be able to
    /// install AWS CDK CLI in local node_modules.
    /// </summary>
    public class NodeInitializer : INodeInitializer
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly ITemplateWriter _templateWriter;
        private readonly IFileManager _fileManager;

        public NodeInitializer(ICommandLineWrapper commandLineWrapper, ITemplateWriter templateWriter, IFileManager fileManager)
        {
            _commandLineWrapper = commandLineWrapper;
            _templateWriter = templateWriter;
            _fileManager = fileManager;
        }

        public bool IsInitialized(string workingDirectory)
        {
            var packageFilePath = Path.Combine(workingDirectory, "package.json");
            return _fileManager.Exists(packageFilePath);
        }

        public async Task Initialize(string workingDirectory, Version cdkVersion)
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var replacementTokens = new Dictionary<string, string>
            {
                { "{aws-cdk-version}", cdkVersion.ToString() },
                { "{version}", $"{assemblyVersion?.Major}.{assemblyVersion?.Minor}.{assemblyVersion?.Build}" }
            };
            var packageFilePath = Path.Combine(workingDirectory, "package.json");
            await _templateWriter.Write(packageFilePath, replacementTokens);

            await _commandLineWrapper.Run("npm install", workingDirectory, false);
        }
    }
}