// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AspNetAppElasticBeanstalkLinux.Configurations
{
    public class BeanstalkApplicationConfiguration
    {
        public bool CreateNew { get; set; }
        public string ApplicationName { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public BeanstalkApplicationConfiguration()
        {

        }
#nullable restore warnings

        public BeanstalkApplicationConfiguration(
            bool createNew,
            string applicationName)
        {
            CreateNew = createNew;
            ApplicationName = applicationName;
        }
    }
}
