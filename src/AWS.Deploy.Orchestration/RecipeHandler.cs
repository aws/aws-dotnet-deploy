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
using AWS.Deploy.Common.Recipes.Validation;
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
        private readonly IFileManager _fileManager;
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IValidatorFactory _validatorFactory;

        public RecipeHandler(IDeploymentManifestEngine deploymentManifestEngine, IOrchestratorInteractiveService orchestratorInteractiveService, IDirectoryManager directoryManager, IFileManager fileManager, IOptionSettingHandler optionSettingHandler, IValidatorFactory validatorFactory)
        {
            _orchestratorInteractiveService = orchestratorInteractiveService;
            _deploymentManifestEngine = deploymentManifestEngine;
            _directoryManager = directoryManager;
            _fileManager = fileManager;
            _optionSettingHandler = optionSettingHandler;
            _validatorFactory = validatorFactory;
        }

        public async Task<List<RecipeDefinition>> GetRecipeDefinitions(List<string>? recipeDefinitionPaths = null)
        {
            recipeDefinitionPaths ??= new List<string> { RecipeLocator.FindRecipeDefinitionsPath() };

            var recipeDefinitions = new List<RecipeDefinition>();
            var uniqueRecipeId = new HashSet<string>();

            try
            {
                foreach(var recipeDefinitionsPath in recipeDefinitionPaths)
                {
                    foreach (var recipeDefinitionFile in _directoryManager.GetFiles(recipeDefinitionsPath, "*.recipe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var content = await _fileManager.ReadAllTextAsync(recipeDefinitionFile);
                            var definition = JsonConvert.DeserializeObject<RecipeDefinition>(content);
                            if (definition == null)
                                throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeRecipe, $"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]");
                            definition.RecipePath = recipeDefinitionFile;
                            if (!uniqueRecipeId.Contains(definition.Id))
                            {
                                definition.DeploymentBundleSettings = GetDeploymentBundleSettings(definition.DeploymentBundle);
                                definition.OptionSettings.AddRange(definition.DeploymentBundleSettings);
                                var dependencyTree = new Dictionary<string, List<string>>();
                                BuildDependencyTree(definition, definition.OptionSettings, dependencyTree);
                                foreach (var dependee in dependencyTree.Keys)
                                {
                                    var optionSetting = _optionSettingHandler.GetOptionSetting(definition, dependee);
                                    optionSetting.Dependents = dependencyTree[dependee];
                                }
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

        private List<OptionSettingItem> GetDeploymentBundleSettings(DeploymentBundleTypes deploymentBundleTypes)
        {
            var deploymentBundleDefinitionsPath = DeploymentBundleDefinitionLocator.FindDeploymentBundleDefinitionPath();

            try
            {
                foreach (var deploymentBundleFile in Directory.GetFiles(deploymentBundleDefinitionsPath, "*.deploymentbundle", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var content = File.ReadAllText(deploymentBundleFile);
                        var definition = JsonConvert.DeserializeObject<DeploymentBundleDefinition>(content);
                        if (definition == null)
                            throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentBundle, $"Failed to Deserialize Deployment Bundle [{deploymentBundleFile}]");
                        if (definition.Type.Equals(deploymentBundleTypes))
                        {
                            // Assign Build category to all of the deployment bundle settings.
                            foreach (var setting in definition.Parameters)
                            {
                                setting.Category = Category.DeploymentBundle.Id;
                            }

                            return definition.Parameters;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentBundle, $"Failed to Deserialize Deployment Bundle [{deploymentBundleFile}]: {e.Message}", e);
                    }
                }
            }
            catch (IOException)
            {
                throw new NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode.DeploymentBundleDefinitionNotFound, "Failed to find a deployment bundle definition");
            }

            throw new NoDeploymentBundleDefinitionsFoundException(DeployToolErrorCode.DeploymentBundleDefinitionNotFound, "Failed to find a deployment bundle definition");
        }

        /// <summary>
        /// Wrapper method to fetch custom recipe definition paths from a deployment-manifest file as well as
        /// other locations that are monitored by the same source control root as the target application that needs to be deployed.
        /// </summary>
        /// <param name="projectDefinition">The <see cref="ProjectDefinition"/> of the application to be deployed.</param>
        /// <returns>A <see cref="HashSet{String}"/> containing absolute paths of directories inside which the custom recipe snapshot is stored</returns>
        public async Task<HashSet<string>> LocateCustomRecipePaths(ProjectDefinition projectDefinition)
        {
            var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;
            var solutionDirectoryPath = !string.IsNullOrEmpty(projectDefinition.ProjectSolutionPath) ?
                new DirectoryInfo(projectDefinition.ProjectSolutionPath).Parent?.FullName ?? string.Empty : string.Empty;

            return await LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath);
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
        /// Runs the recipe level validators and returns a list of failed validations
        /// </summary>
        public List<ValidationResult> RunRecipeValidators(Recommendation recommendation, IDeployToolValidationContext deployToolValidationContext)
        {
            var validatorFailedResults =
               _validatorFactory.BuildValidators(recommendation.Recipe)
                           .Select(async validator => await validator.Validate(recommendation, deployToolValidationContext))
                           .Select(x => x.Result)
                           .Where(x => !x.IsValid)
                           .ToList();

            return validatorFailedResults;
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
            var targetApplicationDirectoryPath = _directoryManager.GetDirectoryInfo(targetApplicationFullPath).Parent?.FullName ?? string.Empty;
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
                    var directoryParent = _directoryManager.GetDirectoryInfo(recipeFilePath).Parent?.FullName;
                    if (string.IsNullOrEmpty(directoryParent))
                        continue;
                    recipePaths.Add(directoryParent);
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

        /// Creates an option setting item dependency tree that indicates
        /// which option setting items need to be validated if a value update occurs.
        /// The function recursively goes through all the settings and their children to build this tree.
        /// This method also creates a Fully Qualified Id which will help reference <see cref="OptionSettingItem"/>.
        /// </summary>
        private void BuildDependencyTree(RecipeDefinition recipe, List<OptionSettingItem> optionSettingItems, Dictionary<string, List<string>> dependencyTree, string parentFullyQualifiedId = "")
        {
            foreach (var optionSettingItem in optionSettingItems)
            {
                optionSettingItem.FullyQualifiedId = string.IsNullOrEmpty(parentFullyQualifiedId)
                    ? optionSettingItem.Id
                    : $"{parentFullyQualifiedId}.{optionSettingItem.Id}";
                optionSettingItem.ParentId = parentFullyQualifiedId;
                foreach (var dependency in optionSettingItem.DependsOn)
                {
                    if (dependencyTree.ContainsKey(dependency.Id))
                    {
                        dependencyTree[dependency.Id].Add(optionSettingItem.FullyQualifiedId);
                    }
                    else
                    {
                        dependencyTree[dependency.Id] = new List<string> { optionSettingItem.FullyQualifiedId };
                    }
                }
                BuildDependencyTree(recipe, optionSettingItem.ChildOptionSettings, dependencyTree, optionSettingItem.FullyQualifiedId);
            }
        }
    }
}
