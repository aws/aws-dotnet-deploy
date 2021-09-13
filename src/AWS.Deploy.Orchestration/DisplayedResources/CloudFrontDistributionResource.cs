// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;

using Amazon.CloudFront.Model;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class CloudFrontDistributionResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public CloudFrontDistributionResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            var distribution = await _awsResourceQueryer.GetCloudFrontDistribution(resourceId);

            var endpoint = $"https://{distribution.DomainName}/";

            return new Dictionary<string, string>() {
                { "Endpoint", endpoint }
            };
        }
    }
}
