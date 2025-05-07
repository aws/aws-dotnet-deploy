// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using AWS.Deploy.Common;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.Installers.Folder;
using Microsoft.TemplateEngine.IDE;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using DefaultTemplateEngineHost = Microsoft.TemplateEngine.Edge.DefaultTemplateEngineHost;
using WellKnownSearchFilters = Microsoft.TemplateEngine.Utils.WellKnownSearchFilters;

namespace AWS.Deploy.Orchestration
{
    public class TemplateEngine
    {
        private const string HOST_IDENTIFIER = "aws-net-deploy-template-generator";
        private const string HOST_VERSION = "v2.0.0";
        private readonly Bootstrapper _bootstrapper;

        public TemplateEngine()
        {
            _bootstrapper = new Bootstrapper(CreateHost(), true);
        }

        public async Task GenerateCdkProjectFromTemplateAsync(Recommendation recommendation, OrchestratorSession session, string outputDirectory, string assemblyName)
        {
            if (string.IsNullOrEmpty(recommendation.Recipe.CdkProjectTemplate))
            {
                throw new InvalidOperationException($"{nameof(recommendation.Recipe.CdkProjectTemplate)} cannot be null or an empty string");
            }
            if (string.IsNullOrEmpty(recommendation.Recipe.CdkProjectTemplateId))
            {
                throw new InvalidOperationException($"{nameof(recommendation.Recipe.CdkProjectTemplateId)} cannot be null or an empty string");
            }

            //The location of the base template that will be installed into the templating engine
            var cdkProjectTemplateDirectory = Path.Combine(
                Path.GetDirectoryName(recommendation.Recipe.RecipePath) ??
                    throw new InvalidRecipePathException(DeployToolErrorCode.BaseTemplatesInvalidPath, $"The following RecipePath is invalid as we could not retrieve the parent directory: {recommendation.Recipe.RecipePath}"),
                recommendation.Recipe.CdkProjectTemplate);

            //Installing the base template into the templating engine to make it available for generation
            await InstallTemplates(cdkProjectTemplateDirectory);

            //Looking up the installed template in the templating engine
            var templates = await
                _bootstrapper
                    .GetTemplatesAsync(
                        new[] { WellKnownSearchFilters.NameFilter(recommendation.Recipe.CdkProjectTemplateId) });
            var template = templates.FirstOrDefault()?.Info;

            //If the template is not found, throw an exception
            if (template == null)
                throw new Exception($"Failed to find a Template for [{recommendation.Recipe.CdkProjectTemplateId}]");

            var templateParameters = new Dictionary<string, string?> {
                // CDK Template projects can parameterize the version number of the AWS.Deploy.Recipes.CDK.Common package. This avoids
                // projects having to be modified every time the package version is bumped.
                { "AWSDeployRecipesCDKCommonVersion", FileVersionInfo.GetVersionInfo(typeof(AWS.Deploy.Recipes.CDK.Common.CDKRecipeSetup).Assembly.Location).ProductVersion
                                                      ?? throw new InvalidAWSDeployRecipesCDKCommonVersionException(DeployToolErrorCode.InvalidAWSDeployRecipesCDKCommonVersion, "The version number of the AWS.Deploy.Recipes.CDK.Common package is invalid.") }
            };

            try
            {
                //Generate the CDK project using the installed template into the output directory
                await _bootstrapper.CreateAsync(template, assemblyName, outputDirectory, templateParameters);
            }
            catch
            {
                throw new TemplateGenerationFailedException(DeployToolErrorCode.FailedToGenerateCDKProjectFromTemplate, "Failed to generate CDK project from template");
            }
        }

        private async Task InstallTemplates(string folderLocation)
        {
            try
            {
                var installRequests = new[]
                {
                    new InstallRequest(folderLocation, folderLocation, force: true)
                };

                var result = await _bootstrapper.InstallTemplatePackagesAsync(installRequests);
                if (result.Any(x => x.Success == false))
                    throw new Exception("Failed to install the default template that is required to the generate the CDK project");
            }
            catch(Exception e)
            {
                throw new DefaultTemplateInstallationFailedException(DeployToolErrorCode.FailedToInstallProjectTemplates, "Failed to install the default template that is required to the generate the CDK project", e);
            }
        }

        private ITemplateEngineHost CreateHost()
        {
            var preferences = new Dictionary<string, string>
            {
                { "prefs:language", "C#" }
            };

            var builtIns = new List<(Type, IIdentifiedComponent)>
            {
                (typeof(IGenerator), new RunnableProjectGenerator()),
                (typeof(IInstallerFactory), new FolderInstallerFactory())
            };

            ITemplateEngineHost host = new DefaultTemplateEngineHost(HOST_IDENTIFIER, HOST_VERSION, preferences, builtIns);

            return host;
        }
    }
}
