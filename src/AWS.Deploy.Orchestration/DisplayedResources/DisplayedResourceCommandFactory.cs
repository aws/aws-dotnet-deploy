// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    /// <summary>
    /// Interface for displayed resources such as <see cref="ElasticBeanstalkEnvironmentResource"/>
    /// </summary>
    public interface IDisplayedResourceCommand
    {
        Task<Dictionary<string, string>> Execute(string resourceId);
    }

    public interface IDisplayedResourceCommandFactory
    {
        IDisplayedResourceCommand? GetResource(string resourceType);
    }

    /// <summary>
    /// Factory class responsible to build and get the displayed resources.
    /// </summary>
    public class DisplayedResourceCommandFactory : IDisplayedResourceCommandFactory
    {
        private const string RESOURCE_TYPE_APPRUNNER_SERVICE = "AWS::AppRunner::Service";
        private const string RESOURCE_TYPE_ELASTICBEANSTALK_ENVIRONMENT = "AWS::ElasticBeanstalk::Environment";
        private const string RESOURCE_TYPE_ELASTICLOADBALANCINGV2_LOADBALANCER = "AWS::ElasticLoadBalancingV2::LoadBalancer";
        private const string RESOURCE_TYPE_S3_BUCKET = "AWS::S3::Bucket";
        private const string RESOURCE_TYPE_EVENTS_RULE = "AWS::Events::Rule";

        private readonly Dictionary<string, IDisplayedResourceCommand> _resources;

        public DisplayedResourceCommandFactory(IAWSResourceQueryer awsResourceQueryer)
        {
            _resources = new Dictionary<string, IDisplayedResourceCommand>
            {
                { RESOURCE_TYPE_APPRUNNER_SERVICE, new AppRunnerServiceResource(awsResourceQueryer) },
                { RESOURCE_TYPE_ELASTICBEANSTALK_ENVIRONMENT, new ElasticBeanstalkEnvironmentResource(awsResourceQueryer) },
                { RESOURCE_TYPE_ELASTICLOADBALANCINGV2_LOADBALANCER, new ElasticLoadBalancerResource(awsResourceQueryer) },
                { RESOURCE_TYPE_S3_BUCKET, new S3BucketResource(awsResourceQueryer) },
                { RESOURCE_TYPE_EVENTS_RULE, new CloudWatchEventResource(awsResourceQueryer) }
            };
        }

        public IDisplayedResourceCommand? GetResource(string resourceType)
        {
            if (!_resources.ContainsKey(resourceType))
            {
                return null;
            }

            return _resources[resourceType];
        }
    }
}
