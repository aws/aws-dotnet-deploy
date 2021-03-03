// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestrator.CDK
{
    public interface ICDKManager
    {
        Task<bool> InstallIfNeeded(string workingDirectory, Version cdkVersion);
    }

    /// <summary>
    /// Makes sure that a compatible version of AWS CDK CLI is installed either in the global node_modules
    /// or local node_modules.
    /// </summary>
    public class CDKManager : ICDKManager
    {
        private readonly ICDKInstaller _cdkInstaller;
        private readonly INodeInitializer _nodeInitializer;

        public CDKManager(ICDKInstaller cdkInstaller, INodeInitializer nodeInitializer)
        {
            _cdkInstaller = cdkInstaller;
            _nodeInitializer = nodeInitializer;
        }

        /// <summary>
        /// Detects whether CDK CLI is installed or not in global node_modules.
        /// If there does not exist global installation, it installs required CDK CLI in local node_modules.
        /// If there exists a global installation of CDK CLI but isn't compatible with .NET Deploy, it installs CDK CLI in local node_modules.
        /// If there exists a local installation of CDK CLI but isn't compatible with .NET Deploy, it upgrades CDK CLI to supported version in local node_modules.
        /// </summary>
        /// <param name="workingDirectory">Directory used for local node app</param>
        /// <param name="cdkVersion">Version of AWS CDK CLI</param>
        public async Task<bool> InstallIfNeeded(string workingDirectory, Version cdkVersion)
        {
            try
            {
                var globalCDKVersionResult = await _cdkInstaller.GetVersion(workingDirectory, true);
                if (globalCDKVersionResult.Success && globalCDKVersionResult.Result?.CompareTo(cdkVersion) >= 0)
                {
                    return true;
                }

                var isLocalNodeInitialized = _nodeInitializer.IsInitialized(workingDirectory);
                if (!isLocalNodeInitialized)
                {
                    await _nodeInitializer.Initialize(workingDirectory, cdkVersion);
                }

                var localCDKVersionResult = await _cdkInstaller.GetVersion(workingDirectory, false);
                if (localCDKVersionResult.Success && localCDKVersionResult.Result?.CompareTo(cdkVersion) >= 0)
                {
                    return true;
                }

                await _cdkInstaller.Install(workingDirectory, cdkVersion);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
