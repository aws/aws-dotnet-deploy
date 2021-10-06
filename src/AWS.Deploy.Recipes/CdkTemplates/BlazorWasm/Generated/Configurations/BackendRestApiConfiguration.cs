using System;
using System.Collections.Generic;
using System.Text;

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace BlazorWasm.Configurations
{
    public partial class BackendRestApiConfiguration
    {
        /// <summary>
        /// Enable Backend rest api
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// Uri to the backend rest api
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The resource path pattern to determine which request to go to backend rest api. (i.e. "/api/*") 
        /// </summary>
        public string ResourcePathPattern { get; set; }

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public BackendRestApiConfiguration()
        {

        }
#nullable restore warnings

        public BackendRestApiConfiguration(
            string uri,
            string resourcePathPattern
            )
        {
            Uri = uri;
            ResourcePathPattern = resourcePathPattern;
        }
    }
}
