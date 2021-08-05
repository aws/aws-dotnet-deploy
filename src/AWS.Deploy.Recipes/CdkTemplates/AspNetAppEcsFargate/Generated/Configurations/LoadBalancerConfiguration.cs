using System;
using System.Collections.Generic;
using System.Text;

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppEcsFargate.Configurations
{
    public partial class LoadBalancerConfiguration
    {
        /// <summary>
        /// If set and <see cref="CreateNew"/> is false, create a new LoadBalancer
        /// </summary>
        public bool CreateNew { get; set; }

        /// <summary>
        /// If not creating a new Load Balancer then this is set to an existing load balancer arn.
        /// </summary>
        public string ExistingLoadBalancerArn { get; set; }

        /// <summary>
        /// How much time to allow currently executing request in ECS tasks to finish before deregistering tasks.
        /// </summary>
        public int DeregistrationDelayInSeconds { get; set; } = 60;

        /// <summary>
        /// The ping path destination where Elastic Load Balancing sends health check requests.
        /// </summary>
        public string? HealthCheckPath { get; set; }

        /// <summary>
        /// The approximate number of seconds between health checks.
        /// </summary>
        public int? HealthCheckInternval { get; set; }

        /// <summary>
        /// The number of consecutive health check successes required before considering an unhealthy target healthy.
        /// </summary>
        public int? HealthyThresholdCount { get; set; }

        /// <summary>
        /// The number of consecutive health check successes required before considering an unhealthy target unhealthy.
        /// </summary>
        public int? UnhealthyThresholdCount { get; set; }

        public enum ListenerConditionTypeEnum { None, Path}
        /// <summary>
        /// The type of listener condition to create. Current valid values are "None" and "Path"
        /// </summary>
        public ListenerConditionTypeEnum? ListenerConditionType { get; set; }

        /// <summary>
        /// The resource path pattern to use with ListenerConditionType is set to "Path"
        /// </summary>
        public string? ListenerConditionPathPattern { get; set; }

        /// <summary>
        /// The priority of the listener condition rule.
        /// </summary>
        public double ListenerConditionPriority { get; set; } = 100;


        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public LoadBalancerConfiguration()
        {

        }
#nullable restore warnings

        public LoadBalancerConfiguration(
            bool createNew,
            string loadBalancerId)
        {
            CreateNew = createNew;
            ExistingLoadBalancerArn = loadBalancerId;
        }
    }
}
