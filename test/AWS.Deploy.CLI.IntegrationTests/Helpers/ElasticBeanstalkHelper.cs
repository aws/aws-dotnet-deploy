// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.S3;
using Amazon.S3.Transfer;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class ElasticBeanstalkHelper
    {
        private readonly IAmazonElasticBeanstalk _client;
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IToolInteractiveService _interactiveService;

        public ElasticBeanstalkHelper(IAmazonElasticBeanstalk client, IAWSResourceQueryer awsResourceQueryer, IToolInteractiveService toolInteractiveService)
        {
            _client = client;
            _awsResourceQueryer = awsResourceQueryer;
            _interactiveService = toolInteractiveService;
        }

        public async Task CreateApplicationAsync(string applicationName)
        {
            _interactiveService.WriteLine($"Create Elastic Beanstalk application: {applicationName}");
            await _client.CreateApplicationAsync(new CreateApplicationRequest
            {
                ApplicationName = applicationName,
                Description = "aws-dotnet-deploy-integ-test"
            });
        }

        public async Task CreateApplicationVersionAsync(string applicationName, string versionLabel, string deploymentPackage)
        {
            _interactiveService.WriteLine($"Creating new application version for {applicationName}: {versionLabel}");

            var bucketName = (await _client.CreateStorageLocationAsync()).S3Bucket;

            var key = string.Format("{0}/AWSDeploymentArchive_{0}_{1}{2}",
                                        applicationName.Replace(' ', '-'),
                                        versionLabel.Replace(' ', '-'),
                                        new FileManager().GetExtension(deploymentPackage));

            await UploadToS3Async(bucketName, key, deploymentPackage);

            await _client.CreateApplicationVersionAsync(new CreateApplicationVersionRequest
            {
                ApplicationName = applicationName,
                VersionLabel = versionLabel,
                SourceBundle = new S3Location { S3Bucket = bucketName, S3Key = key }
            });
        }

        public async Task<bool> CreateEnvironmentAsync(string applicationName, string environmentName, string targetFramework, string versionLabel, BeanstalkPlatformType platformType, string ec2Role)
        {
            _interactiveService.WriteLine($"Creating new Elastic Beanstalk environment {environmentName} with versionLabel {versionLabel}");

            var startingEventDate = DateTime.Now;

            await _client.CreateEnvironmentAsync(new CreateEnvironmentRequest
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName,
                VersionLabel = versionLabel,
                PlatformArn = (await _awsResourceQueryer.GetLatestElasticBeanstalkPlatformArn(targetFramework, platformType)).PlatformArn,
                OptionSettings = new List<ConfigurationOptionSetting>
                {
                    new ConfigurationOptionSetting("aws:autoscaling:launchconfiguration", "IamInstanceProfile", ec2Role),
                    new ConfigurationOptionSetting("aws:elasticbeanstalk:healthreporting:system", "SystemType", "basic")
                }
            });

            return await WaitForEnvironmentCreateCompletion(applicationName, environmentName, startingEventDate);
        }

        public async Task<bool> DeleteApplication(string applicationName, string environmentName)
        {
            _interactiveService.WriteLine($"Deleting Elastic Beanstalk application: {applicationName}");
            _interactiveService.WriteLine($"Deleting Elastic Beanstalk environment: {environmentName}");
            await _client.DeleteApplicationAsync(new DeleteApplicationRequest
            {
                ApplicationName = applicationName,
                TerminateEnvByForce = true
            });

            return await WaitForEnvironmentDeletion(environmentName);
        }

        public async Task<bool> VerifyEnvironmentVersionLabel(string environmentName, string expectedVersionLabel)
        {
            var envDescription = await _awsResourceQueryer.DescribeElasticBeanstalkEnvironment(environmentName);
            _interactiveService.WriteLine($"The Elastic Beanstalk environment is pointing to \"{envDescription.VersionLabel}\" version label");
            return string.Equals(envDescription.VersionLabel, expectedVersionLabel);
        }

        private async Task<bool> WaitForEnvironmentCreateCompletion(string applicationName, string environmentName, DateTime startingEventDate)
        {
            _interactiveService.WriteLine("Waiting for environment update to complete");

            var success = true;
            var environment = new EnvironmentDescription();
            var lastEventDate = startingEventDate;
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

            do
            {
                Thread.Sleep(5000);

                var responseEnvironments = await _client.DescribeEnvironmentsAsync(requestEnvironment);
                environment = responseEnvironments.Environments[0];

                requestEvents.StartTimeUtc = lastEventDate;
                var responseEvents = await _client.DescribeEventsAsync(requestEvents);
                if (responseEvents.Events.Any())
                {
                    for (var i = responseEvents.Events.Count - 1; i >= 0; i--)
                    {
                        var evnt = responseEvents.Events[i];
                        if (evnt.EventDate <= lastEventDate)
                            continue;

                        _interactiveService.WriteLine(evnt.EventDate.ToLocalTime() + "    " + evnt.Severity + "    " + evnt.Message);
                        if (evnt.Severity == EventSeverity.ERROR || evnt.Severity == EventSeverity.FATAL)
                        {
                            success = false;
                        }
                    }

                    lastEventDate = responseEvents.Events[0].EventDate;
                }
            } while (environment.Status == EnvironmentStatus.Launching || environment.Status == EnvironmentStatus.Updating);

            return success;
        }

        private async Task<bool> WaitForEnvironmentDeletion(string environmentName)
        {
            var attemptCount = 0;
            const int maxAttempts = 7;

            while (attemptCount < maxAttempts)
            {
                attemptCount += 1;
                var response = await _client.DescribeEnvironmentsAsync(new DescribeEnvironmentsRequest
                {
                    EnvironmentNames = new List<string> { environmentName }
                });

                if (!response.Environments.Any() || response.Environments.Single().Status == EnvironmentStatus.Terminated)
                    return true;

                await Task.Delay(GetWaitTime(attemptCount));
            }

            return false;
        }

        private TimeSpan GetWaitTime(int attemptCount)
        {
            var waitTime = Math.Pow(2, attemptCount) * 5;
            return TimeSpan.FromSeconds(waitTime);
        }

        private async Task UploadToS3Async(string bucketName, string key, string filePath)
        {
            _interactiveService.WriteLine("Uploading application deployment package to S3...");

            using (var stream =  File.OpenRead(filePath))
            {
                var request = new TransferUtilityUploadRequest()
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream
                };

                var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USWest2);
                await new TransferUtility(s3Client).UploadAsync(request);
            }
        }
    }
}
