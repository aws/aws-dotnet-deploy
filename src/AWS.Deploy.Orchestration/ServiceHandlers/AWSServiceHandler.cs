// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Orchestration.ServiceHandlers
{
    public interface IAWSServiceHandler
    {
        IS3Handler S3Handler { get; }
        IElasticBeanstalkHandler ElasticBeanstalkHandler { get; }
    }

    public class AWSServiceHandler : IAWSServiceHandler
    {
        public IS3Handler S3Handler { get; }
        public IElasticBeanstalkHandler ElasticBeanstalkHandler { get; }

        public AWSServiceHandler(IS3Handler s3Handler, IElasticBeanstalkHandler elasticBeanstalkHandler)
        {
            S3Handler = s3Handler;
            ElasticBeanstalkHandler = elasticBeanstalkHandler;
        }
    }
}
