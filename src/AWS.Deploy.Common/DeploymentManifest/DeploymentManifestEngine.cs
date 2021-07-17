// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common.IO;
using Newtonsoft.Json;

namespace AWS.Deploy.Common.DeploymentManifest
{
    public interface IDeploymentManifestEngine
    {
        Task UpdateDeploymentManifestFile(string saveCdkDirectoryFullPath, string targetApplicationFullPath);
    }

    /// <summary>
    /// This class contains the helper methods to update the deployment manifest file
    /// that keeps track of the save CDK deployment projects.
    /// </summary>
    public class DeploymentManifestEngine : IDeploymentManifestEngine
    {
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;

        private const string DEPLOYMENT_MANIFEST_FILE_NAME = "aws-deployments.json";

        public DeploymentManifestEngine(IDirectoryManager directoryManager, IFileManager fileManager)
        {
            _directoryManager = directoryManager;
            _fileManager = fileManager;
        }

        /// <summary>
        /// This method updates the deployment manifest json file by adding the directory path at which the CDK deployment project is saved.
        /// If the manifest file does not exists then a new file is generated.
        /// <param name="saveCdkDirectoryFullPath">The absolute path to the directory at which the CDK deployment project is saved</param>
        /// <param name="targetApplicationFullPath">The absolute path to the target application csproj or fsproj file.</param>
        /// <exception cref="FailedToUpdateDeploymentManifestFileException">Thrown if an error occured while trying to update the deployment manifest file.</exception>
        /// </summary>
        /// <returns></returns>
        public async Task UpdateDeploymentManifestFile(string saveCdkDirectoryFullPath, string targetApplicationFullPath)
        {
            try
            {
                var deploymentManifestFilePath = GetDeploymentManifestFilePath(targetApplicationFullPath);
                var saveCdkDirectoryRelativePath = _directoryManager.GetRelativePath(targetApplicationFullPath, saveCdkDirectoryFullPath);

                DeploymentManifestModel deploymentManifestModel;

                if (_fileManager.Exists(deploymentManifestFilePath))
                {
                    deploymentManifestModel = await ReadManifestFile(deploymentManifestFilePath);
                    deploymentManifestModel.DeploymentManifestEntries.Add(new DeploymentManifestEntry(saveCdkDirectoryRelativePath));
                }
                else
                {
                    var deploymentManifestEntries = new List<DeploymentManifestEntry> { new DeploymentManifestEntry(saveCdkDirectoryRelativePath) };
                    deploymentManifestModel = new DeploymentManifestModel(deploymentManifestEntries);
                }

                var manifestFileJsonString = SerializeManifestModel(deploymentManifestModel);
                await _fileManager.WriteAllTextAsync(deploymentManifestFilePath, manifestFileJsonString);
            }
            catch (Exception ex)
            {
                throw new FailedToUpdateDeploymentManifestFileException($"Failed to update the deployment manifest file " +
                    $"for the deployment project stored at '{saveCdkDirectoryFullPath}'", ex);
            }
            
        }

        /// <summary>
        /// This method parses the deployment-manifest file into a <see cref="DeploymentManifestModel"/>
        /// </summary>
        /// <param name="filePath">The path to the deployment-manifest file</param>
        /// <returns>An instance of <see cref="DeploymentManifestModel"/></returns>
        private async Task<DeploymentManifestModel> ReadManifestFile(string filePath)
        {
            var manifestFilejsonString = await _fileManager.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<DeploymentManifestModel>(manifestFilejsonString);
        }

        /// <summary>
        /// This method parses the <see cref="DeploymentManifestModel"/> into a string
        /// </summary>
        /// <param name="deploymentManifestModel"><see cref="DeploymentManifestModel"/></param>
        /// <returns>A formatted string representation of <see cref="DeploymentManifestModel"></returns>
        private string SerializeManifestModel(DeploymentManifestModel deploymentManifestModel)
        {
            return JsonConvert.SerializeObject(deploymentManifestModel, Formatting.Indented);
        }

        /// <summary>
        /// This method returns the path at which the deployment-manifest file will be stored.
        /// <param name="targetApplicationFullPath">The absolute path to the target application csproj or fsproj file</param>
        /// </summary>
        /// <returns>The path to the deployment-manifest file.</returns>
        private string GetDeploymentManifestFilePath(string targetApplicationFullPath)
        {
            var projectDirectoryFullPath = _directoryManager.GetDirectoryInfo(targetApplicationFullPath).Parent.FullName;
            var deploymentManifestFileFullPath = Path.Combine(projectDirectoryFullPath, DEPLOYMENT_MANIFEST_FILE_NAME);
            return deploymentManifestFileFullPath;
        }
    }
}
