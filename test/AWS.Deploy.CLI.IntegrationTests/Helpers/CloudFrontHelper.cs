// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class CloudFrontHelper
    {
        readonly IAmazonCloudFront _cloudFrontClient;

        public CloudFrontHelper(IAmazonCloudFront cloudFrontClient)
        {
            _cloudFrontClient = cloudFrontClient;
        }

        public async Task<Distribution> GetDistribution(string id)
        {
            var response = await _cloudFrontClient.GetDistributionAsync(new GetDistributionRequest {Id = id });
            return response.Distribution;
        }
    }
}
