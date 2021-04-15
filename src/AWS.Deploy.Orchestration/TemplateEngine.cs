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

        public TemplateEngine()
        {
            _bootstrapper = new Bootstrapper(CreateHost(), null, virtualizeConfiguration: true);
        }

        public async Task GenerateCDKProjectFromTemplate(Recommendation recommendation, OrchestratorSession session, string outputDirectory)
        {
            //The location of the base template that will be installed into the templating engine
            var cdkProjectTemplateDirectory = Path.Combine(Path.GetDirectoryName(recommendation.Recipe.RecipePath), recommendation.Recipe.CdkProjectTemplate);

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
                { "AWSAccountID" , session.AWSAccountId },
                { "AWSRegion" , session.AWSRegion },

                // CDK Template projects can parameterize the version number of the AWS.Deploy.Recipes.CDK.Common package. This avoid
                // projects having to be modified every time the package version is bumped.
                { "AWSDeployRecipesCDKCommonVersion", FileVersionInfo.GetVersionInfo(typeof(CloudFormationIdentifierConstants).Assembly.Location).ProductVersion }
            };

            foreach(var option in recommendation.Recipe.OptionSettings)
            {
                var currentValue = recommendation.GetOptionSettingValue(option);
                if (currentValue != null)
                    templateParameters[option.Id] = currentValue.ToString();
            }

            try
            {
                //Generate the CDK project using the installed template into the output directory
                await _bootstrapper.CreateAsync(template, recommendation.ProjectDefinition.AssemblyName, outputDirectory, templateParameters, false, "");
            }
            catch
            {
                throw new TemplateGenerationFailedException("Failed to generate CDK project from template");
            }
        }

        private void InstallTemplates(string folderLocation)
        {
            try
            {
                _bootstrapper.Install(folderLocation);
            }
            catch(Exception e)
            {
                throw new DefaultTemplateInstallationFailedException("Failed to install the default template that is required to the generate the CDK project", e);
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
