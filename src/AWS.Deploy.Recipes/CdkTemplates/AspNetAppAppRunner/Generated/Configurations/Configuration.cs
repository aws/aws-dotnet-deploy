// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppAppRunner.Configurations
{
    public partial class Configuration
    {
        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The Identity and Access Management (IAM) role that provides the AWS App Runner service access to pull the container image from ECR.
        /// </summary>
        public IAMRoleConfiguration ServiceAccessIAMRole { get; set; }

        /// <summary>
        /// The name of the AppRunner service.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// The port that your application listens to in the container. Defaults to port 80.
        /// </summary>
        public int Port { get; set; } = 80;

        /// <summary>
        /// An optional command that App Runner runs to start the application in the source image. If specified, this command overrides the Docker image’s default start command.
        /// </summary>
        public string? StartCommand { get; set; }

        /// <summary>
        /// The ARN of the KMS key that's used for encryption.
        /// </summary>
        public string? EncryptionKmsKey { get; set; }

        /// <summary>
        /// The number of consecutive checks that must succeed before App Runner decides that the service is healthy.
        /// </summary>
        public double? HealthCheckHealthyThreshold { get; set; }

        /// <summary>
        /// The time interval, in seconds, between health checks.
        /// </summary>
        public int? HealthCheckInterval { get; set; }

        /// <summary>
        /// The URL that health check requests are sent to.
        /// </summary>
        public string? HealthCheckPath { get; set; }

        /// <summary>
        /// The IP protocol that App Runner uses to perform health checks for your service.
        /// </summary>
        public string HealthCheckProtocol { get; set; } = "TCP";

        /// <summary>
        /// The time, in seconds, to wait for a health check response before deciding it failed.
        /// </summary>
        public int? HealthCheckTimeout { get; set; }

        /// <summary>
        /// The number of consecutive checks that must fail before App Runner decides that the service is unhealthy.
        /// </summary>
        public int? HealthCheckUnhealthyThreshold { get; set; }

        /// <summary>
        /// The number of CPU units reserved for each instance of your App Runner service.
        /// </summary>
        public string Cpu { get; set; }

        /// <summary>
        /// The amount of memory, in MB or GB, reserved for each instance of your App Runner service.
        /// </summary>
        public string Memory { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public Configuration()
        {

        }
#nullable restore warnings

        public Configuration(
            IAMRoleConfiguration applicationIAMRole,
            IAMRoleConfiguration serviceAccessIAMRole,
            string serviceName,
            int? port,
            string healthCheckProtocol,
            string cpu,
            string memory)
        {
            ApplicationIAMRole = applicationIAMRole;
            ServiceAccessIAMRole = serviceAccessIAMRole;
            ServiceName = serviceName;
            Port = port ?? 80;
            HealthCheckProtocol = healthCheckProtocol;
            Cpu = cpu;
            Memory = memory;
        }
    }
}
