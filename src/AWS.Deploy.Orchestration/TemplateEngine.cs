// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes.CDK.Common;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.IDE;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Utils;

namespace AWS.Deploy.Orchestration
{
    public class TemplateEngine
    {
        private const string HostIdentifier = "aws-net-deploy-template-generator";
        private const string HostVersion = "v1.0.0";
        private readonly Bootstrapper _bootstrapper;
        private static readonly object s_locker = new();

        public TemplateEngine()
        {
            _bootstrapper = new Bootstrapper(CreateHost(), null, virtualizeConfiguration: true);
        }

        public void GenerateCDKProjectFromTemplate(Recommendation recommendation, OrchestratorSession session, string outputDirectory, string assemblyName)
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
            InstallTemplates(cdkProjectTemplateDirectory);

            //Looking up the installed template in the templating engine
            var template =
                _bootstrapper
                    .ListTemplates(
                        true,
                        WellKnownSearchFilters.NameFilter(recommendation.Recipe.CdkProjectTemplateId))
                    .FirstOrDefault()
                    ?.Info;

            //If the template is not found, throw an exception
            if (template == null)
                throw new Exception($"Failed to find a Template for [{recommendation.Recipe.CdkProjectTemplateId}]");

            var templateParameters = new Dictionary<string, string> {
                // CDK Template projects can parameterize the version number of the AWS.Deploy.Recipes.CDK.Common package. This avoid
                // projects having to be modified every time the package version is bumped.
                { "AWSDeployRecipesCDKCommonVersion", FileVersionInfo.GetVersionInfo(typeof(Constants.CloudFormationIdentifier).Assembly.Location).ProductVersion
                                                      ?? throw new InvalidAWSDeployRecipesCDKCommonVersionException(DeployToolErrorCode.InvalidAWSDeployRecipesCDKCommonVersion, "The version number of the AWS.Deploy.Recipes.CDK.Common package is invalid.") }
            };

            try
            {
                lock (s_locker)
                {
                    //Generate the CDK project using the installed template into the output directory
                    _bootstrapper.CreateAsync(template, assemblyName, outputDirectory, templateParameters, false, "").GetAwaiter().GetResult();
                }
            }
            catch
            {
                throw new TemplateGenerationFailedException(DeployToolErrorCode.FailedToGenerateCDKProjectFromTemplate, "Failed to generate CDK project from template");
            }
        }

        private void InstallTemplates(string folderLocation)
        {
            try
            {
                lock (s_locker)
                {
                    _bootstrapper.Install(folderLocation);
                }
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

            var builtIns = new AssemblyComponentCatalog(new[]
            {
                typeof(RunnableProjectGenerator).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Orchestrator.RunnableProjects
                typeof(AssemblyComponentCatalog).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Edge
            });

            ITemplateEngineHost host = new DefaultTemplateEngineHost(HostIdentifier, HostVersion, CultureInfo.CurrentCulture.Name, preferences, builtIns, null);

            return host;
        }
    }
}
