// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public class ElasticBeanstalkManagedPlatformUpdatesConfiguration
    {
        public bool ManagedActionsEnabled { get; set; } = true;
        public string PreferredStartTime { get; set; } = "Sun:00:00";
        public string UpdateLevel { get; set; } = "minor";
    }
}
