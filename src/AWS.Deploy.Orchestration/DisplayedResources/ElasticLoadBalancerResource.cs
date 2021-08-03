// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancingV2;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class ElasticLoadBalancerResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public ElasticLoadBalancerResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var loadBalancer = await _awsResourceQueryer.DescribeElasticLoadBalancer(resourceId);
            var listeners = await _awsResourceQueryer.DescribeElasticLoadBalancerListeners(resourceId);

            var protocol = "http";
            var httpsListeners = listeners.Where(x => x.Protocol.Equals(ProtocolEnum.HTTPS)).ToList();
            if (httpsListeners.Any())
                protocol = "https";

            return new Dictionary<string, string>() {
                { "Endpoint", $"{protocol}://{loadBalancer.DNSName}/" }
            };
        }
    }
}
