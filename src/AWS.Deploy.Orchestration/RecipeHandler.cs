// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class RecipeHandler : IRecipeHandler
    {
        private readonly string _ignorePathSubstring = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IDirectoryManager _directoryManager;

        public RecipeHandler(IDeploymentManifestEngine deploymentManifestEngine, IOrchestratorInteractiveService orchestratorInteractiveService, IDirectoryManager directoryManager)
        {
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _deploymentManifestEngine = deploymentManifestEngine;
            _directoryManager = directoryManager;
        }

        public async Task<List<RecipeDefinition>> GetRecipeDefinitions(ProjectDefinition? projectDefinition, List<string>? recipeDefinitionPaths = null)
        {
            recipeDefinitionPaths ??= new List<string>();
            recipeDefinitionPaths.Add(RecipeLocator.FindRecipeDefinitionsPath());
            if(projectDefinition != null)
            {
                var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;
                var solutionDirectoryPath = !string.IsNullOrEmpty(projectDefinition.ProjectSolutionPath) ?
                    new DirectoryInfo(projectDefinition.ProjectSolutionPath).Parent.FullName : string.Empty;

                var customPaths = await LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath);
                recipeDefinitionPaths = recipeDefinitionPaths.Union(customPaths).ToList();
            }

            var recipeDefinitions = new List<RecipeDefinition>();
            var uniqueRecipeId = new HashSet<string>();

            try
            {
                foreach(var recipeDefinitionsPath in recipeDefinitionPaths)
                {
                    foreach (var recipeDefinitionFile in Directory.GetFiles(recipeDefinitionsPath, "*.recipe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var content = File.ReadAllText(recipeDefinitionFile);
                            var definition = JsonConvert.DeserializeObject<RecipeDefinition>(content);
                            if (definition == null)
                                throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeRecipe, $"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]");
                            definition.RecipePath = recipeDefinitionFile;
                            if (!uniqueRecipeId.Contains(definition.Id))
                            {
                                recipeDefinitions.Add(definition);
                                uniqueRecipeId.Add(definition.Id);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeRecipe, $"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]: {e.Message}", e);
                        }
                    }
                }
            }
            catch(IOException)
            {
                throw new NoRecipeDefinitionsFoundException(DeployToolErrorCode.FailedToFindRecipeDefinitions, "Failed to find recipe definitions");
            }

            return recipeDefinitions;
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
                {
                    _orchestratorInteractiveService.LogInfoMessage($"Found custom recipe file at: {recipePath}");
                    customRecipePaths.Add(recipePath);
                }
            }

            foreach (var recipePath in LocateAlternateRecipePaths(targetApplicationFullPath, solutionDirectoryPath))
            {
                if (ContainsRecipeFile(recipePath))
                {
                    _orchestratorInteractiveService.LogInfoMessage($"Found custom recipe file at: {recipePath}");
                    customRecipePaths.Add(recipePath);
                }
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
                _orchestratorInteractiveService.LogErrorMessage(Environment.NewLine);
                _orchestratorInteractiveService.LogErrorMessage("Failed to load custom deployment recommendations " +
                   "from the deployment-manifest file due to an error while trying to deserialze the file.");
                return await Task.FromResult(new List<string>());
            }
        }

        /// <summary>
        /// Fetches custom recipe paths from other locations that are monitored by the same source control root as the target application that needs to be deployed.
        /// If the target application is not under source control, then it scans the sub-directories of the solution folder for custom recipes.
        /// If source control root directory is equal to the file system root, then it scans the sub-directories of the solution folder for custom recipes.
        /// </summary>
        /// <param name="targetApplicationFullPath">The absolute path to the target application csproj or fsproj file</param>
        /// <param name="solutionDirectoryPath">The absolute path of the directory which contains the solution file for the target application</param>
        /// <returns>A list of recipe definition paths.</returns>
        private List<string> LocateAlternateRecipePaths(string targetApplicationFullPath, string solutionDirectoryPath)
        {
            var targetApplicationDirectoryPath = _directoryManager.GetDirectoryInfo(targetApplicationFullPath).Parent.FullName;
            var fileSystemRootPath = _directoryManager.GetDirectoryInfo(targetApplicationDirectoryPath).Root.FullName;
            var rootDirectoryPath = GetSourceControlRootDirectory(targetApplicationDirectoryPath);

            if (string.IsNullOrEmpty(rootDirectoryPath) || string.Equals(rootDirectoryPath, fileSystemRootPath))
                rootDirectoryPath = solutionDirectoryPath;

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
                var recipePathList = new List<string>();
                try
                {
                    recipePathList = _directoryManager.GetFiles(rootDirectoryPath, "*.recipe", SearchOption.AllDirectories).ToList();
                }
                catch (Exception e)
                {
                    _orchestratorInteractiveService.LogInfoMessage($"Failed to find custom recipe paths starting from {rootDirectoryPath}. Encountered the following exception: {e.GetType()}");
                }

                foreach (var recipeFilePath in recipePathList)
                {
                    if (recipeFilePath.Contains(_ignorePathSubstring))
                        continue;
                    recipePaths.Add(_directoryManager.GetDirectoryInfo(recipeFilePath).Parent.FullName);
                }
            }
            return recipePaths;
        }

        /// <summary>
        /// Helper method to find the source control root directory of the current directory path.
        /// If the current directory is not monitored by any source control system, then it returns string.Empty
        /// </summary>
        /// <param name="directoryPath">An absolute directory path.</param>
        /// <returns> First parent directory path that contains a ".git" folder or string.Empty if cannot find any</returns>
        private string GetSourceControlRootDirectory(string? directoryPath)
        {
            var currentDir = directoryPath;
            while (currentDir != null)
            {
                if (_directoryManager.GetDirectories(currentDir, ".git").Any())
                {
                    var sourceControlRootDirectory = _directoryManager.GetDirectoryInfo(currentDir).FullName;
                    _orchestratorInteractiveService.LogDebugMessage($"Source control root directory found at: {sourceControlRootDirectory}");
                    return sourceControlRootDirectory;
                }

                currentDir = _directoryManager.GetDirectoryInfo(currentDir).Parent?.FullName;
            }

            _orchestratorInteractiveService.LogDebugMessage($"Could not find any source control root directory");
            return string.Empty;
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
