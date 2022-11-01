// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using System.Text.Json.Serialization;

namespace AWS.Deploy.Orchestration.ServiceHandlers
{
    public interface IElasticBeanstalkHandler
    {
        /// <summary>
        /// Deployments to Windows Elastic Beanstalk envvironments require a manifest file to be included with the binaries.
        /// This method creates the manifest file if it doesn't exist, or it creates a new one.
        /// The two main settings that are updated are IIS Website and IIS App Path.
        /// </summary>
        void SetupWindowsDeploymentManifest(Recommendation recommendation, string dotnetZipFilePath);

        /// <summary>
        /// When deploying a self contained deployment bundle, Beanstalk needs a Procfile to tell the environment what process to start up.
        /// Check out the AWS Elastic Beanstalk developer guide for more information on Procfiles
        /// https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-linux-procfile.html
        /// </summary>
        void SetupProcfileForSelfContained(string dotnetZipFilePath);

        Task<S3Location> CreateApplicationStorageLocationAsync(string applicationName, string versionLabel, string deploymentPackage);
        Task<CreateApplicationVersionResponse> CreateApplicationVersionAsync(string applicationName, string versionLabel, S3Location sourceBundle);
        Task<bool> UpdateEnvironmentAsync(string applicationName, string environmentName, string versionLabel, List<ConfigurationOptionSetting> optionSettings);
        List<ConfigurationOptionSetting> GetEnvironmentConfigurationSettings(Recommendation recommendation);
    }

    /// <summary>
    /// This class represents the structure of the Windows manifest file to be included with Windows Elastic Beanstalk deployments.
    /// </summary>
    public class ElasticBeanstalkWindowsManifest
    {
        [JsonPropertyName("manifestVersion")]
        public int ManifestVersion { get; set; } = 1;

        [JsonPropertyName("deployments")]
        public ManifestDeployments Deployments { get; set; } = new();

        public class ManifestDeployments
        {

            [JsonPropertyName("aspNetCoreWeb")]
            public List<AspNetCoreWebDeployments> AspNetCoreWeb { get; set; } = new();

            public class AspNetCoreWebDeployments
            {

                [JsonPropertyName("name")]
                public string Name { get; set; } = "app";


                [JsonPropertyName("parameters")]
                public AspNetCoreWebParameters Parameters { get; set; } = new();

                public class AspNetCoreWebParameters
                {
                    [JsonPropertyName("appBundle")]
                    public string AppBundle { get; set; } = ".";

                    [JsonPropertyName("iisPath")]
                    public string IISPath { get; set; } = "/";

                    [JsonPropertyName("iisWebSite")]
                    public string IISWebSite { get; set; } = "Default Web Site";
                }
            }
        }
    }

    public class AWSElasticBeanstalkHandler : IElasticBeanstalkHandler
    {
        private readonly IAWSClientFactory _awsClientFactory;
        private readonly IOrchestratorInteractiveService _interactiveService;
        private readonly IFileManager _fileManager;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public AWSElasticBeanstalkHandler(IAWSClientFactory awsClientFactory, IOrchestratorInteractiveService interactiveService, IFileManager fileManager, IOptionSettingHandler optionSettingHandler)
        {
            _awsClientFactory = awsClientFactory;
            _interactiveService = interactiveService;
            _fileManager = fileManager;
            _optionSettingHandler = optionSettingHandler;
        }

        private T GetOrCreateNode<T>(object? json) where T : new()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json?.ToString() ?? string.Empty, new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// Deployments to Windows Elastic Beanstalk envvironments require a manifest file to be included with the binaries.
        /// This method creates the manifest file if it doesn't exist, or it creates a new one.
        /// The two main settings that are updated are IIS Website and IIS App Path.
        /// </summary>
        public void SetupWindowsDeploymentManifest(Recommendation recommendation, string dotnetZipFilePath)
        {
            var iisWebSiteOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.IISWebSiteOptionId);
            var iisAppPathOptionSetting = _optionSettingHandler.GetOptionSetting(recommendation, Constants.ElasticBeanstalk.IISAppPathOptionId);

            var iisWebSiteValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, iisWebSiteOptionSetting);
            var iisAppPathValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, iisAppPathOptionSetting);

            var iisWebSite = !string.IsNullOrEmpty(iisWebSiteValue) ? iisWebSiteValue : "Default Web Site";
            var iisAppPath = !string.IsNullOrEmpty(iisAppPathValue) ? iisAppPathValue : "/";

            var newManifestFile = new ElasticBeanstalkWindowsManifest();
            newManifestFile.Deployments.AspNetCoreWeb.Add(new ElasticBeanstalkWindowsManifest.ManifestDeployments.AspNetCoreWebDeployments
            {
                Parameters = new ElasticBeanstalkWindowsManifest.ManifestDeployments.AspNetCoreWebDeployments.AspNetCoreWebParameters
                {
                    IISPath = iisAppPath,
                    IISWebSite = iisWebSite
                }
            });

            using (var zipArchive = ZipFile.Open(dotnetZipFilePath, ZipArchiveMode.Update))
            {
                var zipEntry = zipArchive.GetEntry(Constants.ElasticBeanstalk.WindowsManifestName);
                var serializedManifest = JsonSerializer.Serialize(new Dictionary<string, object>());
                if (zipEntry != null)
                {
                    using (var streamReader = new StreamReader(zipEntry.Open()))
                    {
                        serializedManifest = streamReader.ReadToEnd();
                    }
                }

                var jsonDoc = GetOrCreateNode<Dictionary<string, object>>(serializedManifest);

                if (!jsonDoc.ContainsKey("manifestVersion"))
                {
                    jsonDoc["manifestVersion"] = newManifestFile.ManifestVersion;
                }

                if (jsonDoc.ContainsKey("deployments"))
                {
                    var deploymentNode = GetOrCreateNode<Dictionary<string, object>>(jsonDoc["deployments"]);

                    if (deploymentNode.ContainsKey("aspNetCoreWeb"))
                    {
                        var aspNetCoreWebNode = GetOrCreateNode<List<object>>(deploymentNode["aspNetCoreWeb"]);
                        if (aspNetCoreWebNode.Count == 0)
                        {
                            aspNetCoreWebNode.Add(newManifestFile.Deployments.AspNetCoreWeb[0]);
                        }
                        else
                        {
                            // We only need 1 entry in the 'aspNetCoreWeb' node that defines the parameters we are interested in. Typically, only 1 entry exists.
                            var aspNetCoreWebEntry = GetOrCreateNode<Dictionary<string, object>>(JsonSerializer.Serialize(aspNetCoreWebNode[0]));

                            var nameValue = aspNetCoreWebEntry.ContainsKey("name") ? aspNetCoreWebEntry["name"].ToString() : string.Empty;
                            aspNetCoreWebEntry["name"] = !string.IsNullOrEmpty(nameValue) ? nameValue : newManifestFile.Deployments.AspNetCoreWeb[0].Name;

                            if (aspNetCoreWebEntry.ContainsKey("parameters"))
                            {
                                var parametersNode = GetOrCreateNode<Dictionary<string, object>>(aspNetCoreWebEntry["parameters"]);
                                parametersNode["appBundle"] = ".";
                                parametersNode["iisPath"] = iisAppPath;
                                parametersNode["iisWebSite"] = iisWebSite;

                                aspNetCoreWebEntry["parameters"] = parametersNode;
                            }
                            else
                            {
                                aspNetCoreWebEntry["parameters"] = newManifestFile.Deployments.AspNetCoreWeb[0].Parameters;
                            }
                            aspNetCoreWebNode[0] = aspNetCoreWebEntry;
                        }
                        deploymentNode["aspNetCoreWeb"] = aspNetCoreWebNode;
                    }
                    else
                    {
                        deploymentNode["aspNetCoreWeb"] = newManifestFile.Deployments.AspNetCoreWeb;
                    }

                    jsonDoc["deployments"] = deploymentNode;
                }
                else
                {
                    jsonDoc["deployments"] = newManifestFile.Deployments;
                }

                using (var jsonStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(jsonDoc, new JsonSerializerOptions { WriteIndented = true })))
                {
                    zipEntry ??= zipArchive.CreateEntry(Constants.ElasticBeanstalk.WindowsManifestName);
                    using var zipEntryStream = zipEntry.Open();
                    jsonStream.Position = 0;
                    jsonStream.CopyTo(zipEntryStream);
                }
            }
        }

        /// <summary>
        /// When deploying a self contained deployment bundle, Beanstalk needs a Procfile to tell the environment what process to start up.
        /// Check out the AWS Elastic Beanstalk developer guide for more information on Procfiles
        /// https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-linux-procfile.html
        ///
        /// This code is a copy of the code in the AspNetAppElasticBeanstalkLinux CDK recipe definition. Any changes to this method
        /// should be made into that version as well.
        /// </summary>
        /// <param name="dotnetZipFilePath"></param>
        public void SetupProcfileForSelfContained(string dotnetZipFilePath)
        {
            const string RUNTIME_CONFIG_SUFFIX = ".runtimeconfig.json";
            const string PROCFILE_NAME = "Procfile";

            string runtimeConfigFilename;
            string runtimeConfigJson;
            using (var zipArchive = ZipFile.Open(dotnetZipFilePath, ZipArchiveMode.Read))
            {
                // Skip Procfile setup if one already exists.
                if (zipArchive.GetEntry(PROCFILE_NAME) != null)
                {
                    return;
                }

                var runtimeConfigEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.EndsWith(RUNTIME_CONFIG_SUFFIX));
                if (runtimeConfigEntry == null)
                {
                    return;
                }

                runtimeConfigFilename = runtimeConfigEntry.Name;
                using var stream = runtimeConfigEntry.Open();
                runtimeConfigJson = new StreamReader(stream).ReadToEnd();
            }

            var runtimeConfigDoc = JsonDocument.Parse(runtimeConfigJson);

            if (!runtimeConfigDoc.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptionsNode))
            {
                return;
            }

            // If there are includedFrameworks then the zip file is a self contained deployment bundle.
            if (!runtimeOptionsNode.TryGetProperty("includedFrameworks", out _))
            {
                return;
            }

            var executableName = runtimeConfigFilename.Substring(0, runtimeConfigFilename.Length - RUNTIME_CONFIG_SUFFIX.Length);
            var procCommand = $"web: ./{executableName}";

            using (var zipArchive = ZipFile.Open(dotnetZipFilePath, ZipArchiveMode.Update))
            {
                var procfileEntry = zipArchive.CreateEntry(PROCFILE_NAME);
                using var zipEntryStream = procfileEntry.Open();
                zipEntryStream.Write(System.Text.UTF8Encoding.UTF8.GetBytes(procCommand));
            }
        }

        public async Task<S3Location> CreateApplicationStorageLocationAsync(string applicationName, string versionLabel, string deploymentPackage)
        {
            string bucketName;
            try
            {
                var ebClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
                bucketName = (await ebClient.CreateStorageLocationAsync()).S3Bucket;
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkException(DeployToolErrorCode.FailedToCreateElasticBeanstalkStorageLocation, "An error occured while creating the Elastic Beanstalk storage location", e);
            }

            var key = string.Format("{0}/AWSDeploymentArchive_{0}_{1}{2}",
                                        applicationName.Replace(' ', '-'),
                                        versionLabel.Replace(' ', '-'),
                                        _fileManager.GetExtension(deploymentPackage));

            return new S3Location { S3Bucket = bucketName, S3Key = key };
        }

        public async Task<CreateApplicationVersionResponse> CreateApplicationVersionAsync(string applicationName, string versionLabel, S3Location sourceBundle)
        {
            _interactiveService.LogInfoMessage("Creating new application version: " + versionLabel);

            try
            {
                var ebClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
                var response = await ebClient.CreateApplicationVersionAsync(new CreateApplicationVersionRequest
                {
                    ApplicationName = applicationName,
                    VersionLabel = versionLabel,
                    SourceBundle = sourceBundle
                });
                return response;
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkException(DeployToolErrorCode.FailedToCreateElasticBeanstalkApplicationVersion, "An error occured while creating the Elastic Beanstalk application version", e);
            }
        }

        public List<ConfigurationOptionSetting> GetEnvironmentConfigurationSettings(Recommendation recommendation)
        {
            var additionalSettings = new List<ConfigurationOptionSetting>();

            List<(string OptionSettingId, string OptionSettingNameSpace, string OptionSettingName)> tupleList;
            switch (recommendation.Recipe.Id)
            {
                case Constants.RecipeIdentifier.EXISTING_BEANSTALK_ENVIRONMENT_RECIPE_ID:
                    tupleList = Constants.ElasticBeanstalk.OptionSettingQueryList;
                    break;
                case Constants.RecipeIdentifier.EXISTING_BEANSTALK_WINDOWS_ENVIRONMENT_RECIPE_ID:
                    tupleList = Constants.ElasticBeanstalk.WindowsOptionSettingQueryList;
                    break;
                default:
                    throw new InvalidOperationException($"The recipe '{recommendation.Recipe.Id}' is not supported.");
            };

            foreach (var tuple in tupleList)
            {
                var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, tuple.OptionSettingId);

                if (!optionSetting.Updatable)
                    continue;

                var optionSettingValue = optionSetting.GetValue<string>(new Dictionary<string, object>());

                additionalSettings.Add(new ConfigurationOptionSetting
                {
                    Namespace = tuple.OptionSettingNameSpace,
                    OptionName = tuple.OptionSettingName,
                    Value = optionSettingValue
                });
            }

            return additionalSettings;
        }

        public async Task<bool> UpdateEnvironmentAsync(string applicationName, string environmentName, string versionLabel, List<ConfigurationOptionSetting> optionSettings)
        {
            _interactiveService.LogInfoMessage("Getting latest environment event date before update");

            var startingEventDate = await GetLatestEventDateAsync(applicationName, environmentName);

            _interactiveService.LogInfoMessage($"Updating environment {environmentName} to new application version {versionLabel}");

            var updateRequest = new UpdateEnvironmentRequest
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName,
                VersionLabel = versionLabel,
                OptionSettings = optionSettings
            };

            try
            {
                var ebClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
                var updateEnvironmentResponse = await ebClient.UpdateEnvironmentAsync(updateRequest);
                return await WaitForEnvironmentUpdateCompletion(applicationName, environmentName, startingEventDate);
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkException(DeployToolErrorCode.FailedToUpdateElasticBeanstalkEnvironment, "An error occured while updating the Elastic Beanstalk environment", e);
            }
        }

        private async Task<DateTime> GetLatestEventDateAsync(string applicationName, string environmentName)
        {
            var request = new DescribeEventsRequest
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName
            };

            var ebClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();
            var response = await ebClient.DescribeEventsAsync(request);
            if (response.Events.Count == 0)
                return DateTime.Now;

            return response.Events.First().EventDate;
        }

        private async Task<bool> WaitForEnvironmentUpdateCompletion(string applicationName, string environmentName, DateTime startingEventDate)
        {
            _interactiveService.LogInfoMessage("Waiting for environment update to complete");

            var success = true;
            var environment = new EnvironmentDescription();
            var lastPrintedEventDate = startingEventDate;
            var requestEvents = new DescribeEventsRequest
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName
            };
            var requestEnvironment = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName,
                EnvironmentNames = new List<string> { environmentName }
            };
            var ebClient = _awsClientFactory.GetAWSClient<IAmazonElasticBeanstalk>();

            do
            {
                Thread.Sleep(5000);

                var responseEnvironments = await ebClient.DescribeEnvironmentsAsync(requestEnvironment);
                if (responseEnvironments.Environments.Count == 0)
                    throw new AWSResourceNotFoundException(DeployToolErrorCode.BeanstalkEnvironmentDoesNotExist, $"Failed to find environment {environmentName} belonging to application {applicationName}");

                environment = responseEnvironments.Environments[0];

                requestEvents.StartTimeUtc = lastPrintedEventDate;
                var responseEvents = await ebClient.DescribeEventsAsync(requestEvents);
                if (responseEvents.Events.Any())
                {
                    for (var i = responseEvents.Events.Count - 1; i >= 0; i--)
                    {
                        var evnt = responseEvents.Events[i];
                        if (evnt.EventDate <= lastPrintedEventDate)
                            continue;

                        _interactiveService.LogInfoMessage(evnt.EventDate.ToLocalTime() + "    " + evnt.Severity + "    " + evnt.Message);
                        if (evnt.Severity == EventSeverity.ERROR || evnt.Severity == EventSeverity.FATAL)
                        {
                            success = false;
                        }
                    }

                    lastPrintedEventDate = responseEvents.Events[0].EventDate;
                }

            } while (environment.Status == EnvironmentStatus.Launching || environment.Status == EnvironmentStatus.Updating);

            return success;
        }
    }
}
