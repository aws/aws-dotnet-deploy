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
    public interface INPMPackageInitializer
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
        /// <remarks>
        /// Installs CDK CLI along with initialization.
        /// </remarks>w
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="cdkVersion">Version of CDK CLI.</param>
        /// <exception cref="PackageJsonFileException">Thrown when package.json IO fails.</exception>
        /// <exception cref="NPMCommandFailedException">Thrown when a npm command fails to execute.</exception>
        Task Initialize(string workingDirectory, Version cdkVersion);
    }

    /// <summary>
    /// Orchestrates local node app.
    /// It makes sure that a local node application is initialized in order to be able to
    /// install CDK CLI in local node_modules.
    /// </summary>
    public class NPMPackageInitializer : INPMPackageInitializer
    {
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IPackageJsonGenerator _packageJsonGenerator;
        private readonly IFileManager _fileManager;
        private const string _packageJsonFileName = "package.json";

        public NPMPackageInitializer(ICommandLineWrapper commandLineWrapper, IPackageJsonGenerator packageJsonGenerator, IFileManager fileManager)
        {
            _commandLineWrapper = commandLineWrapper;
            _packageJsonGenerator = packageJsonGenerator;
            _fileManager = fileManager;
        }

        public bool IsInitialized(string workingDirectory)
        {
            var packageFilePath = Path.Combine(workingDirectory, _packageJsonFileName);
            return _fileManager.Exists(packageFilePath);
        }

        public async Task Initialize(string workingDirectory, Version cdkVersion)
        {
            var packageJsonFileContent = _packageJsonGenerator.Generate(cdkVersion);
            var packageJsonFilePath = Path.Combine(workingDirectory, _packageJsonFileName);

            try
            {
                await _fileManager.WriteAllTextAsync(packageJsonFilePath, packageJsonFileContent);
            }
            catch (Exception exception)
            {
                throw new PackageJsonFileException($"Failed to write {_packageJsonFileName} at {packageJsonFilePath}", exception);
            }

            try
            {
                // Install node packages specified in package.json file
                await _commandLineWrapper.Run("npm install", workingDirectory, false);
            }
            catch (Exception exception)
            {
                throw new NPMCommandFailedException($"Failed to install npm packages at {workingDirectory}", exception);
            }
        }
    }
}
