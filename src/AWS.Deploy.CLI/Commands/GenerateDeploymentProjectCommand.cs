// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.Utilities;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Commands;

/// <summary>
/// The class supports the functionality to create a new CDK project and save it at a customer
/// specified directory location.
/// </summary>
public class GenerateDeploymentProjectCommand(
    IToolInteractiveService toolInteractiveService,
    IConsoleUtilities consoleUtilities,
    ICdkProjectHandler cdkProjectHandler,
    ICommandLineWrapper commandLineWrapper,
    IDirectoryManager directoryManager,
    IFileManager fileManager,
    IDeploymentManifestEngine deploymentManifestEngine,
    IRecipeHandler recipeHandler,
    IProjectParserUtility projectParserUtility)
    : CancellableAsyncCommand<GenerateDeploymentProjectCommandSettings>
{
    private const int DEFAULT_PERSISTED_RECIPE_PRIORITY = 1000;

    /// <summary>
    /// This method takes a user specified directory path and generates the CDK deployment project at this location.
    /// If the provided directory path is an empty string, then a default directory is created to save the CDK deployment project.
    /// </summary>
    /// <param name="context">Command context</param>
    /// <param name="settings">Command settings</param>
    /// <param name="cancellationTokenSource">Cancellation token source</param>
    /// <returns>The command exit code</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, GenerateDeploymentProjectCommandSettings settings, CancellationTokenSource cancellationTokenSource)
    {
        toolInteractiveService.Diagnostics = settings.Diagnostics;

        var projectDefinition = await projectParserUtility.Parse(settings.ProjectPath ?? "");

        var saveDirectory = settings.Output;
        var projectDisplayName = settings.ProjectDisplayName;

        OrchestratorSession session = new OrchestratorSession(projectDefinition);

        var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;

        if (!string.IsNullOrEmpty(saveDirectory))
        {
            var targetApplicationDirectoryFullPath = new DirectoryInfo(targetApplicationFullPath).Parent!.FullName;
            saveDirectory = Path.GetFullPath(saveDirectory, targetApplicationDirectoryFullPath);
        }

        var orchestrator = new Orchestrator(session, recipeHandler);
        var recommendations = await GenerateRecommendationsToSaveDeploymentProject(orchestrator);
        var selectedRecommendation = consoleUtilities.AskToChooseRecommendation(recommendations);

        if (string.IsNullOrEmpty(saveDirectory))
            saveDirectory = GenerateDefaultSaveDirectoryPath(targetApplicationFullPath);

        var newDirectoryCreated = CreateSaveCdkDirectory(saveDirectory);

        var (isValid, errorMessage) = ValidateSaveCdkDirectory(saveDirectory, targetApplicationFullPath);
        if (!isValid)
        {
            if (newDirectoryCreated)
                directoryManager.Delete(saveDirectory);
            errorMessage = $"Failed to generate deployment project.{Environment.NewLine}{errorMessage}";
            throw new InvalidSaveDirectoryForCdkProject(DeployToolErrorCode.InvalidSaveDirectoryForCdkProject, errorMessage.Trim());
        }

        var directoryUnderSourceControl = await IsDirectoryUnderSourceControl(saveDirectory);
        if (!directoryUnderSourceControl)
        {
            var userPrompt = "Warning: The target directory is not being tracked by source control. If the saved deployment " +
                "project is used for deployment it is important that the deployment project is retained to allow " +
                "future redeployments to previously deployed applications. " + Environment.NewLine + Environment.NewLine +
                "Do you still want to continue?";

            toolInteractiveService.WriteLine();
            var yesNoResult = consoleUtilities.AskYesNoQuestion(userPrompt, YesNo.Yes);

            if (yesNoResult == YesNo.No)
            {
                if (newDirectoryCreated)
                    directoryManager.Delete(saveDirectory);
                return CommandReturnCodes.SUCCESS;
            }
        }

        cdkProjectHandler.CreateCdkProject(selectedRecommendation, session, saveDirectory);
        await GenerateDeploymentRecipeSnapShot(selectedRecommendation, saveDirectory, projectDisplayName, targetApplicationFullPath);

        var saveCdkDirectoryFullPath = directoryManager.GetDirectoryInfo(saveDirectory).FullName;
        toolInteractiveService.WriteLine();
        toolInteractiveService.WriteLine($"Saving AWS CDK deployment project to: {saveCdkDirectoryFullPath}");

        await deploymentManifestEngine.UpdateDeploymentManifestFile(saveCdkDirectoryFullPath, targetApplicationFullPath);

        return CommandReturnCodes.SUCCESS;
    }

    /// <summary>
    /// This method generates the appropriate recommendations for the target deployment project.
    /// </summary>
    /// <param name="orchestrator"><see cref="Orchestrator"/></param>
    /// <returns>A list of <see cref="Recommendation"/></returns>
    private async Task<List<Recommendation>> GenerateRecommendationsToSaveDeploymentProject(Orchestrator orchestrator)
    {
        var recommendations = await orchestrator.GenerateRecommendationsToSaveDeploymentProject();
        if (recommendations.Count == 0)
        {
            throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.DeploymentProjectNotSupported, "The project you are trying to deploy is currently not supported.");
        }
        return recommendations;
    }

    /// <summary>
    /// This method takes the path to the target deployment project and creates
    /// a default save directory inside the parent folder of the current directory.
    /// For example:
    /// Target project directory - C:\Codebase\MyWebApp
    /// Generated default save directory - C:\Codebase\MyWebApp.Deployment If the save directory already exists, then a suffix number is attached.
    /// </summary>
    /// <param name="targetApplicationFullPath">The full path of the target application.</param>
    /// <returns>The default save directory path.</returns>
    private string GenerateDefaultSaveDirectoryPath(string targetApplicationFullPath)
    {
        var targetApplicationDi = directoryManager.GetDirectoryInfo(targetApplicationFullPath);
        if(targetApplicationDi.Parent == null)
        {
            throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.InvalidFilePath, $"Failed to find parent directory for directory {targetApplicationFullPath}.");
        }
        var applicatonDirectoryFullPath = targetApplicationDi.Parent.FullName;
        var saveCdkDirectoryFullPath = applicatonDirectoryFullPath + ".Deployment";

        var suffixNumber = 0;
        while (directoryManager.Exists(saveCdkDirectoryFullPath))
            saveCdkDirectoryFullPath = applicatonDirectoryFullPath + $".Deployment{++suffixNumber}";

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
        if (!directoryManager.Exists(saveCdkDirectoryPath))
        {
            directoryManager.CreateDirectory(saveCdkDirectoryPath);
            newDirectoryCreated = true;
        }
        return newDirectoryCreated;
    }

    /// <summary>
    /// This method takes the path to the intended location of the CDK deployment project and performs validations on it.
    /// </summary>
    /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
    /// <param name="targetApplicationFullPath">The full path of the target application.</param>
    /// <returns>A tuple containing a boolean that indicates if the directory is valid and a corresponding string error message.</returns>
    private Tuple<bool, string> ValidateSaveCdkDirectory(string saveCdkDirectoryPath, string targetApplicationFullPath)
    {
        var targetApplicationDi = directoryManager.GetDirectoryInfo(targetApplicationFullPath);
        if (targetApplicationDi.Parent == null)
        {
            throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.InvalidFilePath, $"Failed to find parent directory for directory {targetApplicationFullPath}.");
        }

        var errorMessage = string.Empty;
        var isValid = true;
        var targetApplicationDirectoryFullPath = targetApplicationDi.Parent.FullName;

        if (!directoryManager.IsEmpty(saveCdkDirectoryPath))
        {
            errorMessage += "The directory specified for saving the CDK project is non-empty. " +
                "Please provide an empty directory path and try again." + Environment.NewLine;

            isValid = false;
        }
        if (directoryManager.ExistsInsideDirectory(targetApplicationDirectoryFullPath, saveCdkDirectoryPath))
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
        var gitStatusResult = await commandLineWrapper.TryRunWithResult("git status", saveCdkDirectoryPath);
        var svnStatusResult = await commandLineWrapper.TryRunWithResult("svn status", saveCdkDirectoryPath);
        return gitStatusResult.Success || svnStatusResult.Success;
    }

    /// <summary>
    /// Generates a snapshot of the deployment recipe inside the location at which the CDK deployment project is saved.
    /// </summary>
    /// <param name="recommendation"><see cref="Recommendation"/></param>
    /// <param name="saveCdkDirectoryPath">Relative or absolute path of the directory at which the CDK deployment project will be saved.</param>
    /// <param name="projectDisplayName">The name of the deployment project that will be displayed in the list of available deployment options.</param>
    /// <param name="targetApplicationFullPath">The full path of the target application.</param>
    private async Task GenerateDeploymentRecipeSnapShot(Recommendation recommendation, string saveCdkDirectoryPath, string? projectDisplayName, string targetApplicationFullPath)
    {
        var targetApplicationDi = directoryManager.GetDirectoryInfo(targetApplicationFullPath);
        if (targetApplicationDi.Parent == null)
        {
            throw new FailedToGenerateAnyRecommendations(DeployToolErrorCode.InvalidFilePath, $"Failed to find parent directory for directory {targetApplicationFullPath}.");
        }

        var targetApplicationDirectoryName = targetApplicationDi.Name;
        var recipeSnapshotFileName = directoryManager.GetDirectoryInfo(saveCdkDirectoryPath).Name + ".recipe";
        var recipeSnapshotFilePath = Path.Combine(saveCdkDirectoryPath, recipeSnapshotFileName);
        var recipePath = recommendation.Recipe.RecipePath;

        if (string.IsNullOrEmpty(recipePath))
            throw new InvalidOperationException("The recipe path cannot be null or empty as part " +
                $"of the {nameof(recommendation.Recipe)} object");

        var recipeBody = await fileManager.ReadAllTextAsync(recipePath);
        var recipe = JsonConvert.DeserializeObject<RecipeDefinition>(recipeBody);
        if (recipe == null)
            throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeDeploymentProjectRecipe, "Failed to deserialize deployment project recipe");

        var recipeName = string.IsNullOrEmpty(projectDisplayName) ?
            $"Deployment project for {targetApplicationDirectoryName} to {recommendation.Recipe.TargetService}"
            : projectDisplayName;

        recipe.Id = Guid.NewGuid().ToString();
        recipe.Name = recipeName;
        recipe.CdkProjectTemplateId = null;
        recipe.CdkProjectTemplate = null;
        recipe.PersistedDeploymentProject = true;
        recipe.RecipePriority = DEFAULT_PERSISTED_RECIPE_PRIORITY;
        recipe.BaseRecipeId = recommendation.Recipe.Id;

        var recipeSnapshotBody = JsonConvert.SerializeObject(recipe, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new SerializeModelContractResolver()
        });
        await fileManager.WriteAllTextAsync(recipeSnapshotFilePath, recipeSnapshotBody);
    }
}
