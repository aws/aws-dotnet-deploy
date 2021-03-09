// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestrator.CDK
{
    /// <summary>
    /// Makes sure that a compatible version of CDK CLI is installed either in the global node_modules
    /// or local node_modules.
    /// </summary>
    public interface ICDKManager
    {
        /// <summary>
        /// Detects whether CDK CLI is installed or not in global node_modules.
        /// If global node_modules don't contain, it checks in local node_modules
        /// If local npm package isn't initialized, it initializes a npm package at <see cref="workingDirectory"/>.
        /// If local node_modules don't contain, it installs CDK CLI version <see cref="cdkVersion"/> in local modules.
        /// </summary>
        /// <param name="workingDirectory">Directory used for local node app</param>
        /// <param name="cdkVersion">Version of CDK CLI</param>
        Task EnsureCompatibleCDKExists(string workingDirectory, Version cdkVersion);
    }

    public class CDKManager : ICDKManager
    {
        private readonly ICDKInstaller _cdkInstaller;
        private readonly INPMPackageInitializer _npmPackageInitializer;

        public CDKManager(ICDKInstaller cdkInstaller, INPMPackageInitializer npmPackageInitializer)
        {
            _cdkInstaller = cdkInstaller;
            _npmPackageInitializer = npmPackageInitializer;
        }

        public async Task EnsureCompatibleCDKExists(string workingDirectory, Version cdkVersion)
        {
            var globalCDKVersionResult = await _cdkInstaller.GetGlobalVersion();
            if (globalCDKVersionResult.Success && globalCDKVersionResult.Result?.CompareTo(cdkVersion) >= 0)
            {
                return;
            }

            var isNPMPackageInitialized = _npmPackageInitializer.IsInitialized(workingDirectory);
            if (!isNPMPackageInitialized)
            {
                await _npmPackageInitializer.Initialize(workingDirectory, cdkVersion);
                return; // There is no need to install CDK CLI explicitly, npm install takes care of first time bootstrap.
            }

            var localCDKVersionResult = await _cdkInstaller.GetLocalVersion(workingDirectory);
            if (localCDKVersionResult.Success && localCDKVersionResult.Result?.CompareTo(cdkVersion) >= 0)
            {
                return;
            }

            await _cdkInstaller.Install(workingDirectory, cdkVersion);
        }
    }
}
