// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.Orchestration.DisplayedResources
{
    public class S3BucketResource : IDisplayedResourceCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public S3BucketResource(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<Dictionary<string, string>> Execute(string resourceId)
        {
            string region = await _awsResourceQueryer.GetS3BucketLocation(resourceId);
            string regionSeparator = ".";
            if (string.Equals("us-east-1", region) ||
                string.Equals("us-west-1", region) ||
                string.Equals("us-west-2", region) ||
                string.Equals("ap-southeast-1", region) ||
                string.Equals("ap-southeast-2", region) ||
                string.Equals("ap-northeast-1", region) ||
                string.Equals("eu-west-1", region) ||
                string.Equals("sa-east-1", region))
            {
                regionSeparator = "-";
            }

            var endpoint = $"http://{resourceId}.s3-website{regionSeparator}{region}.amazonaws.com/";

            return new Dictionary<string, string>() {
                { "Endpoint", endpoint },
                { "Bucket Name", resourceId }
            };
        }
    }
}
