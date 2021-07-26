// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.DeploymentManifest;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AWS.Deploy.CLI.Extensions
{
    public static class CustomServiceCollectionExtension
    {
        /// <summary>
        /// Extension method for <see cref="IServiceCollection"/> that injects essential app dependencies.
        /// It is safer to use singleton instances for dependencies because every command (ex. deploy, list-deployments) run as a separate instance.
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> instance that holds the app dependencies.</param>
        /// <param name="lifetime"></param>
        public static void AddCustomServices(this IServiceCollection serviceCollection, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IAWSClientFactory), typeof(DefaultAWSClientFactory), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IAWSResourceQueryer), typeof(AWSResourceQueryer), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IAWSUtilities), typeof(AWSUtilities), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICDKInstaller), typeof(CDKInstaller), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICDKManager), typeof(CDKManager), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICdkProjectHandler), typeof(CdkProjectHandler), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICloudApplicationNameGenerator), typeof(CloudApplicationNameGenerator), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICommandLineWrapper), typeof(CommandLineWrapper), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IConsoleUtilities), typeof(ConsoleUtilities), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IDeployedApplicationQueryer), typeof(DeployedApplicationQueryer), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IDeploymentBundleHandler), typeof(DeploymentBundleHandler), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IDirectoryManager), typeof(DirectoryManager), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IFileManager), typeof(FileManager), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(INPMPackageInitializer), typeof(NPMPackageInitializer), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IOrchestratorInteractiveService), typeof(ConsoleOrchestratorLogger), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IProjectDefinitionParser), typeof(ProjectDefinitionParser), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IProjectParserUtility), typeof(ProjectParserUtility), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ISystemCapabilityEvaluator), typeof(SystemCapabilityEvaluator), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ITemplateMetadataReader), typeof(TemplateMetadataReader), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IToolInteractiveService), typeof(ConsoleInteractiveServiceImpl), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ITypeHintCommandFactory), typeof(TypeHintCommandFactory), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IZipFileManager), typeof(ZipFileManager), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IDeploymentManifestEngine), typeof(DeploymentManifestEngine), lifetime));
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(ICommandFactory), typeof(CommandFactory), lifetime));

            var packageJsonTemplate = typeof(PackageJsonGenerator).Assembly.ReadEmbeddedFile(PackageJsonGenerator.TemplateIdentifier);
            serviceCollection.TryAdd(new ServiceDescriptor(typeof(IPackageJsonGenerator), (serviceProvider) => new PackageJsonGenerator(packageJsonTemplate), lifetime));

            // required to run the application
            serviceCollection.AddSingleton<App>();
        }
    }
}
