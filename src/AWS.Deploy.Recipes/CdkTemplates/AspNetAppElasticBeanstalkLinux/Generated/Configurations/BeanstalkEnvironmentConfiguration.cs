// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public partial class BeanstalkEnvironmentConfiguration
    {
        public bool CreateNew { get; set; }
        public string EnvironmentName { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public BeanstalkEnvironmentConfiguration()
        {

        }
#nullable restore warnings

        public BeanstalkEnvironmentConfiguration(
            bool createNew,
            string environmentName)
        {
            CreateNew = createNew;
            EnvironmentName = environmentName;
        }
    }
}
