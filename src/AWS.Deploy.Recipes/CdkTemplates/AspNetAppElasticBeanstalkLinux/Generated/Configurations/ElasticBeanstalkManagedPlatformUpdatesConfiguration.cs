// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public partial class ElasticBeanstalkManagedPlatformUpdatesConfiguration
    {
        public bool ManagedActionsEnabled { get; set; } = true;
        public string PreferredStartTime { get; set; } = "Sun:00:00";
        public string UpdateLevel { get; set; } = "minor";
    }
}
