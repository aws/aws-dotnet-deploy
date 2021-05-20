// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Shell;

namespace AWS.Deploy.Orchestration.CDK
{
    /// <summary>
    /// Orchestrates local node app.
    /// It makes sure that a local node application is initialized in order to be able to
    /// install CDK CLI in local node_modules.
    /// </summary>
    /// <remarks>
    /// When npm package is initialized, a specified version of CDK CLI is installed along with initialization.
    /// </remarks>
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
        /// Initializes npm package at <see cref="workingDirectory"/>
        /// </summary>
        /// <remarks>
        /// When npm package is initialized, a specified version of CDK CLI is installed along with installation.
        /// </remarks>w
        /// <param name="workingDirectory">Directory for local node app.</param>
        /// <param name="cdkVersion">Version of CDK CLI.</param>
        /// <exception cref="PackageJsonFileException">Thrown when package.json IO fails.</exception>
        /// <exception cref="NPMCommandFailedException">Thrown when a npm command fails to execute.</exception>
        Task Initialize(string workingDirectory, Version cdkVersion);
    }

    public class NPMPackageInitializer : INPMPackageInitializer
    {
        private readonly ICommandRunner _commandRunner;
        private readonly IPackageJsonGenerator _packageJsonGenerator;
        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;
        private const string _packageJsonFileName = "package.json";

        public NPMPackageInitializer(ICommandRunner commandRunner, IPackageJsonGenerator packageJsonGenerator, IFileManager fileManager, IDirectoryManager directoryManager)
        {
            _commandRunner = commandRunner;
            _packageJsonGenerator = packageJsonGenerator;
            _fileManager = fileManager;
            _directoryManager = directoryManager;
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
                if (!_directoryManager.Exists(workingDirectory))
                {
                    _directoryManager.CreateDirectory(workingDirectory);
                }

                await _fileManager.WriteAllTextAsync(packageJsonFilePath, packageJsonFileContent);
            }
            catch (Exception exception)
            {
                throw new PackageJsonFileException($"Failed to write {_packageJsonFileName} at {packageJsonFilePath}", exception);
            }

            try
            {
                // Install node packages specified in package.json file
                await _commandRunner.Run("npm install", workingDirectory, false);
            }
            catch (Exception exception)
            {
                throw new NPMCommandFailedException($"Failed to install npm packages at {workingDirectory}", exception);
            }
        }
    }
}
