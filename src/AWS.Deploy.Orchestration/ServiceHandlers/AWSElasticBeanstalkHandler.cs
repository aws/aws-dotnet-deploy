// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;

namespace AWS.Deploy.Orchestration.ServiceHandlers
{
    public interface IElasticBeanstalkHandler
    {
        Task<S3Location> CreateApplicationStorageLocationAsync(string applicationName, string versionLabel, string deploymentPackage);
        Task<CreateApplicationVersionResponse> CreateApplicationVersionAsync(string applicationName, string versionLabel, S3Location sourceBundle);
        Task<bool> UpdateEnvironmentAsync(string applicationName, string environmentName, string versionLabel, List<ConfigurationOptionSetting> optionSettings);
        List<ConfigurationOptionSetting> GetEnvironmentConfigurationSettings(Recommendation recommendation);
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

            foreach (var tuple in Constants.ElasticBeanstalk.OptionSettingQueryList)
            {
                var optionSetting = _optionSettingHandler.GetOptionSetting(recommendation, tuple.OptionSettingId);

                if (!optionSetting.Updatable)
                    continue;

                var optionSettingValue = optionSetting.GetValue<string>(new Dictionary<string, string>());

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
