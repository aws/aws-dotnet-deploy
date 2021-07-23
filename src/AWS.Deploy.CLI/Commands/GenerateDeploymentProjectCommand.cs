// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Utilities;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.CLI.Commands
{
    /// <summary>
    /// The class supports the functionality to create a new CDK project and save it at a customer
    /// specified directory location.
    /// </summary>
    public class GenerateDeploymentProjectCommand
    {
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly ICdkProjectHandler _cdkProjectHandler;
        private readonly ICommandLineWrapper _commandLineWrapper;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly OrchestratorSession _session;
        private readonly string _targetApplicationFullPath;
        
        public GenerateDeploymentProjectCommand(
            IToolInteractiveService toolInteractiveService,
            IConsoleUtilities consoleUtilities,
            ICdkProjectHandler cdkProjectHandler,
            ICommandLineWrapper commandLineWrapper,
            IDirectoryManager directoryManager,
            IFileManager fileManager,
            OrchestratorSession session,
            string targetApplicationFullPath)
        {
            _toolInteractiveService = toolInteractiveService;
            _consoleUtilities = consoleUtilities;
            _cdkProjectHandler = cdkProjectHandler;
            _commandLineWrapper = commandLineWrapper;
            _directoryManager = directoryManager;
            _fileManager = fileManager;
            _session = session;
            _targetApplicationFullPath = targetApplicationFullPath;
        }

        /// <summary>
        /// This method takes a user specified directory path and generates the CDK deployment project at this location.
        /// If the provided directory path is an empty string, then a default directory is created to save the CDK deployment project.
        /// </summary>
        /// <param name="saveCdkDirectoryPath">An absolute or a relative path provided by the user.</param>
        /// <param name="projectDisplayName">The name of the deployment project that will be displayed in the list of available deployment options.</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string saveCdkDirectoryPath, string projectDisplayName)
        {
            var orchestrator = new Orchestrator(_session, new[] { RecipeLocator.FindRecipeDefinitionsPath() });
            var recommendations = await GenerateDeploymentRecommendations(orchestrator);
            var selectedRecommendation = _consoleUtilities.AskToChooseRecommendation(recommendations);

            if (string.IsNullOrEmpty(saveCdkDirectoryPath))
                saveCdkDirectoryPath = GenerateDefaultSaveDirectoryPath();

            var newDirectoryCreated = CreateSaveCdkDirectory(saveCdkDirectoryPath);

            var (isValid, errorMessage) = ValidateSaveCdkDirectory(saveCdkDirectoryPath);
            if (!isValid)
            {
                if (newDirectoryCreated)
                    _directoryManager.Delete(saveCdkDirectoryPath);
                throw new InvalidSaveDirectoryForCdkProject(errorMessage);
            }
            
            var directoryUnderSourceControl = await IsDirectoryUnderSourceControl(saveCdkDirectoryPath);
            if (!directoryUnderSourceControl)
            {
                var userPrompt = "Warning: The target directory is not being tracked by source control. If the saved deployment " +
                    "project is used for deployment it is important that the deployment project is retained to allow " +
                    "future redeployments to previously deployed applications. " + Environment.NewLine + Environment.NewLine +
                    "Do you still want to continue?";

                _toolInteractiveService.WriteLine();
                var yesNoResult = _consoleUtilities.AskYesNoQuestion(userPrompt, YesNo.Yes);

                if (yesNoResult == YesNo.No)
                {
                    if (newDirectoryCreated)
                        _directoryManager.Delete(saveCdkDirectoryPath);
                    return;
                }
            }

            await _cdkProjectHandler.CreateCdkProjectForDeployment(selectedRecommendation, _session, saveCdkDirectoryPath);
            await GenerateDeploymentRecipeSnapShot(selectedRecommendation, saveCdkDirectoryPath, projectDisplayName);

            var saveCdkDirectoryInfo = _directoryManager.GetDirectoryInfo(saveCdkDirectoryPath);
            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine($"The CDK deployment project is saved at: {saveCdkDirectoryInfo.FullName}");
        }

        /// <summary>
        /// This method generates the appropriate recommendations for the target deployment project.
        /// </summary>
        /// <param name="orchestrator"><see cref="Orchestrator"/></param>
        /// <returns>A list of <see cref="Recommendation"/></returns>
        private async Task<List<Recommendation>> GenerateDeploymentRecommendations(Orchestrator orchestrator)
        {
            var recommendations = await orchestrator.GenerateDeploymentRecommendations(forDeployment: false);
            if (recommendations.Count == 0)
            {
                throw new FailedToGenerateAnyRecommendations("The project you are trying to deploy is currently not supported.");
            }
            return recommendations;
        }

        /// <summary>
        /// This method takes the path to the target deployment project and creates
        /// a default save directory inside the parent folder of the current directory.
        /// For example:
        /// Target project directory - C:\Codebase\MyWebApp
        /// Generated default save directory - C:\Codebase\MyWebAppCDK If the save directory already exists, then a suffix number is attached.
        /// </summary>
        /// <returns>The defaukt save directory path.</returns>
        private string GenerateDefaultSaveDirectoryPath()
        {
            var applicatonDirectoryFullPath = _directoryManager.GetDirectoryInfo(_targetApplicationFullPath).Parent.FullName;
            var saveCdkDirectoryFullPath = applicatonDirectoryFullPath + "CDK";

            var suffixNumber = 0;
            while (_directoryManager.Exists(saveCdkDirectoryFullPath))
                saveCdkDirectoryFullPath = applicatonDirectoryFullPath + $"CDK{++suffixNumber}";
            
            return saveCdkDirectoryFullPath;
        }

        /// <summary>
        /// This method takes a path and creates a new directory at this path if one does not already exist.
        /// </summary>
        /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
        /// <returns>A boolean to indicate if a new directory was created.</returns>
        private bool CreateSaveCdkDirectory(string saveCdkDirectoryPath)
        {
            var newDirectoryCreated = false;
            if (!_directoryManager.Exists(saveCdkDirectoryPath))
            {
                _directoryManager.CreateDirectory(saveCdkDirectoryPath);
                newDirectoryCreated = true;
            }
            return newDirectoryCreated;   
        }

        /// <summary>
        /// This method takes the path to the intended location of the CDK deployment project and performs validations on it.
        /// </summary>
        /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
        /// <returns>A tuple containaing a boolean that indicates if the directory is valid and a corresponding string error message.</returns>
        private Tuple<bool, string> ValidateSaveCdkDirectory(string saveCdkDirectoryPath)
        {
            var errorMessage = string.Empty;
            var isValid = true;
            var targetApplicationDirectoryFullPath = _directoryManager.GetDirectoryInfo(_targetApplicationFullPath).Parent.FullName;
           
            if (!_directoryManager.IsEmpty(saveCdkDirectoryPath))
            {
                errorMessage += "The directory specified for saving the CDK project is non-empty. " +
                    "Please provide an empty directory path and try again." + Environment.NewLine;

                isValid = false;
            }
            if (_directoryManager.ExistsInsideDirectory(targetApplicationDirectoryFullPath, saveCdkDirectoryPath))
            {
                errorMessage += "The directory used to save the CDK deployment project is contained inside of " +
                    "the target application project directory. Please specify a different directory and try again.";

                isValid = false;
            }

            return new Tuple<bool, string>(isValid, errorMessage.Trim());
        }

        /// <summary>
        /// Checks if the location of the saved CDK deployment project is monitored by a source control system.
        /// </summary>
        /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
        /// <returns></returns>
        private async Task<bool> IsDirectoryUnderSourceControl(string saveCdkDirectoryPath)
        {
            var gitStatusResult = await _commandLineWrapper.TryRunWithResult("git status", saveCdkDirectoryPath);
            var svnStatusResult = await _commandLineWrapper.TryRunWithResult("svn status", saveCdkDirectoryPath);
            return gitStatusResult.Success || svnStatusResult.Success;
        }

        /// <summary>
        /// Generates a snapshot of the deployment recipe inside the location at which the CDK deployment project is saved.
        /// </summary>
        /// <param name="recommendation"><see cref="Recommendation"/></param>
        /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
        /// <param name="projectDisplayName">The name of the deployment project that will be displayed in the list of available deployment options.</param>
        private async Task GenerateDeploymentRecipeSnapShot(Recommendation recommendation, string saveCdkDirectoryPath, string projectDisplayName)
        {
            var targetApplicationDirectoryName = _directoryManager.GetDirectoryInfo(_targetApplicationFullPath).Parent.Name;
            var recipeSnapshotFileName = _directoryManager.GetDirectoryInfo(saveCdkDirectoryPath).Name + ".recipe";
            var recipeSnapshotFilePath = Path.Combine(saveCdkDirectoryPath, recipeSnapshotFileName);
            var recipePath = recommendation.Recipe.RecipePath;

            if (string.IsNullOrEmpty(recipePath))
                throw new InvalidOperationException("The recipe path cannot be null or empty as part " +
                    $"of the {nameof(recommendation.Recipe)} object");

            var recipeBody = await _fileManager.ReadAllTextAsync(recipePath);
            var recipe = JsonConvert.DeserializeObject<RecipeDefinition>(recipeBody);
            
            var recipeName = string.IsNullOrEmpty(projectDisplayName) ?
                $"Deployment project for {targetApplicationDirectoryName} to {recommendation.Recipe.TargetService}"
                : projectDisplayName;

            recipe.Id = Guid.NewGuid().ToString();
            recipe.Name = recipeName;
            recipe.CdkProjectTemplateId = null;
            recipe.CdkProjectTemplate = null;

            var recipeSnapshotBody = JsonConvert.SerializeObject(recipe, Formatting.Indented);
            await _fileManager.WriteAllTextAsync(recipeSnapshotFilePath, recipeSnapshotBody);
        }
    }
}