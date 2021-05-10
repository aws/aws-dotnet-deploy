// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace BlazorWasm.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The default page to use when endpoint accessed with no resource path.
        /// </summary>
        public string IndexDocument { get; set; }

        /// <summary>
        /// The error page to use when an error occurred accessing the resource path.
        /// </summary>
        public string ErrorDocument { get; set; }

        /// <summary>
        /// Redirect any 404 requests to the index document. This is useful in Blazor applications that modify the
        /// resource path. If the modified resource path is reused in a new browser it will result in a 404 from
        /// S3 since no S3 object exists at that resource path.
        /// </summary>
        public bool Redirect404ToRoot { get; set; } = true;

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public Configuration()
        {

        }
#nullable restore warnings

        public Configuration(
            string indexDocument,
            string errorDocument
            )
        {
            IndexDocument = indexDocument;
            ErrorDocument = errorDocument;
        }
    }
}
