// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Utilities;

namespace AWS.Deploy.Orchestration
{
    public interface ICustomRecipeLocator
    {
        Task<HashSet<string>> LocateCustomRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath);
    }

    /// <summary>
    /// This class supports the functionality to fetch custom recipe paths from a deployment-manifest file as well as
    /// other locations that are monitored by the same source control root as the target application that needs to be deployed.
    /// </summary>
    public class CustomRecipeLocator : ICustomRecipeLocator
    {
        private const string GIT_STATUS_COMMAND = "git status";
        private const string SVN_STATUS_COMMAND = "svn status";

        private readonly string _ignorePathSubstring = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IDirectoryManager _directoryManager;
        
        public CustomRecipeLocator(IDeploymentManifestEngine deploymentManifestEngine, IOrchestratorInteractiveService orchestratorInteractiveService,
            ICommandLineWrapper commandLineWrapper, IDirectoryManager directoryManager)
        {
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _commandLineWrapper = commandLineWrapper;
            _deploymentManifestEngine = deploymentManifestEngine;
            _directoryManager = directoryManager;
        }

        /// <summary>
        /// Wrapper method to fetch custom recipe definition paths from a deployment-manifest file as well as
        /// other locations that are monitored by the same source control root as the target application that needs to be deployed.
        /// </summary>
        /// <param name="targetApplicationFullPath">The absolute path to the csproj or fsproj file of the target application</param>
        /// <param name="solutionDirectoryPath">The absolute path of the directory which contains the solution file for the target application</param>
        /// <returns>A <see cref="HashSet{String}"/> containing absolute paths of directories inside which the custom recipe snapshot is stored</returns>
        public async Task<HashSet<string>> LocateCustomRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath)
        {
            var customRecipePaths = new HashSet<string>();

            foreach (var recipePath in await LocateRecipePathsFromManifestFile(targetApplicationFullPath))
            {
                if (ContainsRecipeFile(recipePath))
                    customRecipePaths.Add(recipePath);
            }

            foreach (var recipePath in await LocateAlternateRecipePaths(targetApplicationFullPath, solutionDirectoryPath))
            {
                if (ContainsRecipeFile(recipePath))
                    customRecipePaths.Add(recipePath);
            }

            return customRecipePaths;
        }

        /// <summary>
        /// Fetches recipe definition paths by parsing the deployment-manifest file that is associated with the target application.
        /// </summary>
        /// <param name="targetApplicationFullPath">The absolute path to the target application csproj or fsproj file</param>
        /// <returns>A list containing absolute paths to the saved CDK deployment projects</returns>
        private async Task<List<string>> LocateRecipePathsFromManifestFile(string targetApplicationFullPath)
        {
            try
            {
                return await _deploymentManifestEngine.GetRecipeDefinitionPaths(targetApplicationFullPath);
            }
            catch
            {
                _orchestratorInteractiveService.LogMessageLine(Environment.NewLine);
                _orchestratorInteractiveService.LogErrorMessageLine("Failed to load custom deployment recommendations " +
                   "from the deployment-manifest file due to an error while trying to deserialze the file.");
                return await Task.FromResult(new List<string>());
            }
        }

        /// <summary>
        /// Fetches custom recipe paths from other locations that are monitored by the same source control root
        /// as the target application that needs to be deployed.
        /// If the target application is not under source control then it scans the sub-directories of the solution folder for custom recipes.
        /// </summary>
        /// <param name="targetApplicationFullPath">The absolute path to the target application csproj or fsproj file</param>
        /// <param name="solutionDirectoryPath">The absolute path of the directory which contains the solution file for the target application</param>
        /// <returns>A list of recipe definition paths.</returns>
        private async Task<List<string>> LocateAlternateRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath )
        {
            var targetApplicationDirectoryPath = _directoryManager.GetDirectoryInfo(targetApplicationFullPath).Parent.FullName;
            string? rootDirectoryPath;

            if (await IsDirectoryUnderSourceControl(targetApplicationDirectoryPath))
            {
                rootDirectoryPath = await GetSourceControlRootDirectory(targetApplicationDirectoryPath);
            }
            else
            {
                rootDirectoryPath = solutionDirectoryPath;
            }

            return GetRecipePathsFromRootDirectory(rootDirectoryPath); 
        }

        /// <summary>
        /// This method takes a root directory path and recursively searches all its sub-directories for custom recipe paths.
        /// However, it ignores any recipe file located inside a "bin" folder.
        /// </summary>
        /// <param name="rootDirectoryPath">The absolute path of the root directory.</param>
        /// <returns>A list of recipe definition paths.</returns>
        private List<string> GetRecipePathsFromRootDirectory(string? rootDirectoryPath)
        {
            var recipePaths = new List<string>();
            if (!string.IsNullOrEmpty(rootDirectoryPath) && _directoryManager.Exists(rootDirectoryPath))
            {
                foreach (var recipeFilePath in _directoryManager.GetFiles(rootDirectoryPath, "*.recipe", SearchOption.AllDirectories))
                {
                    if (recipeFilePath.Contains(_ignorePathSubstring))
                        continue;
                    recipePaths.Add(_directoryManager.GetDirectoryInfo(recipeFilePath).Parent.FullName);
                }
            }
            return recipePaths;
        }

        /// <summary>
        /// This method finds the root directory that is monitored by the same source control as the current directory.
        /// </summary>
        /// <param name="currentDirectoryPath">The absolute path of the current directory</param>
        /// <returns>The source control root directory absolute path.</returns>
        private async Task<string?> GetSourceControlRootDirectory(string currentDirectoryPath)
        {
            var possibleRootDirectoryPath = currentDirectoryPath;
            while (currentDirectoryPath != null && await IsDirectoryUnderSourceControl(currentDirectoryPath))
            {
                possibleRootDirectoryPath = currentDirectoryPath;
                currentDirectoryPath = _directoryManager.GetDirectoryInfo(currentDirectoryPath).Parent.FullName;
            }
            return possibleRootDirectoryPath;
        }

        /// <summary>
        /// Helper method to find if the directory is monitored by a source control system.
        /// </summary>
        /// <param name="directoryPath">An absolute directory path.</param>
        /// <returns></returns>
        private async Task<bool> IsDirectoryUnderSourceControl(string? directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath))
            {
                var gitStatusResult = await _commandLineWrapper.TryRunWithResult(GIT_STATUS_COMMAND, directoryPath);
                var svnStatusResult = await _commandLineWrapper.TryRunWithResult(SVN_STATUS_COMMAND, directoryPath);
                return gitStatusResult.Success || svnStatusResult.Success;
            }
            return false;
        }

        /// <summary>
        /// This method determines if the given directory contains any recipe files
        /// </summary>
        /// <param name="directoryPath">The path of the directory that needs to be validated</param>
        /// <returns>A bool indicating the presence of a recipe file inside the directory.</returns>
        private bool ContainsRecipeFile(string directoryPath)
        {
            var directoryName = _directoryManager.GetDirectoryInfo(directoryPath).Name;
            var recipeFilePaths = _directoryManager.GetFiles(directoryPath, "*.recipe");
            if (!recipeFilePaths.Any())
            {
                return false;
            }

            return recipeFilePaths.All(filePath => Path.GetFileNameWithoutExtension(filePath).Equals(directoryName, StringComparison.Ordinal));
        }
    }
}
