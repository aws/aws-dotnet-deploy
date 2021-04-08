// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI.Extensions
{
    public static class CustomServiceCollectionExtension
    {
        /// <summary>
        /// Extension method for <see cref="IServiceCollection"/> that injects essential app dependencies.
        /// It is safer to use singleton instances for dependencies because every command (ex. deploy, list-deployments) run as a separate instance.
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> instance that holds the app dependencies.</param>
        public static void AddCustomServices(this IServiceCollection serviceCollection)
        {
            // dependencies that are required in various part of the app
            serviceCollection.AddSingleton<IAWSClientFactory, DefaultAWSClientFactory>();
            serviceCollection.AddSingleton<IAWSResourceQueryer, AWSResourceQueryer>();
            serviceCollection.AddSingleton<IAWSUtilities, AWSUtilities>();
            serviceCollection.AddSingleton<ICDKInstaller, CDKInstaller>();
            serviceCollection.AddSingleton<ICDKManager, CDKManager>();
            serviceCollection.AddSingleton<ICdkProjectHandler, CdkProjectHandler>();
            serviceCollection.AddSingleton<ICloudApplicationNameGenerator, CloudApplicationNameGenerator>();
            serviceCollection.AddSingleton<ICommandLineWrapper, CommandLineWrapper>();
            serviceCollection.AddSingleton<IConsoleUtilities, ConsoleUtilities>();
            serviceCollection.AddSingleton<IDeployedApplicationQueryer, DeployedApplicationQueryer>();
            serviceCollection.AddSingleton<IDeploymentBundleHandler, DeploymentBundleHandler>();
            serviceCollection.AddSingleton<IDirectoryManager, DirectoryManager>();
            serviceCollection.AddSingleton<IFileManager, FileManager>();
            serviceCollection.AddSingleton<INPMPackageInitializer, NPMPackageInitializer>();
            serviceCollection.AddSingleton<IOrchestratorInteractiveService, ConsoleOrchestratorLogger>();
            serviceCollection.AddSingleton<IPackageJsonGenerator, PackageJsonGenerator>();
            serviceCollection.AddSingleton<IProjectDefinitionParser, ProjectDefinitionParser>();
            serviceCollection.AddSingleton<IProjectParserUtility, ProjectParserUtility>();
            serviceCollection.AddSingleton<ISystemCapabilityEvaluator, SystemCapabilityEvaluator>();
            serviceCollection.AddSingleton<ITemplateMetadataReader, TemplateMetadataReader>();
            serviceCollection.AddSingleton<IToolInteractiveService, ConsoleInteractiveServiceImpl>();
            serviceCollection.AddSingleton<ITypeHintCommandFactory, TypeHintCommandFactory>();
            serviceCollection.AddSingleton<IZipFileManager, ZipFileManager>();
            serviceCollection.AddSingleton<ICommandFactory, CommandFactory>();

            var packageJsonTemplate = typeof(PackageJsonGenerator).Assembly.ReadEmbeddedFile(PackageJsonGenerator.TemplateIdentifier);
            serviceCollection.AddSingleton<IPackageJsonGenerator>(new PackageJsonGenerator(packageJsonTemplate));

            // required to run the application
            serviceCollection.AddSingleton<App>();
        }
    }
}
