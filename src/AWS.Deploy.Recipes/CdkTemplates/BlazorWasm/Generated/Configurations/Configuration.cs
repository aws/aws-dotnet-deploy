// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace BlazorWasm.Configurations
{
    public partial class Configuration
    {
        /// <summary>
        /// The default page to use when endpoint accessed with no resource path.
        /// </summary>
        public string IndexDocument { get; set; }

        /// <summary>
        /// The error page to use when an error occurred accessing the resource path.
        /// </summary>
        public string? ErrorDocument { get; set; }

        /// <summary>
        /// Redirect any 404 and 403 requests to the index document. This is useful in Blazor applications that modify the resource path in the browser.
        /// If the modified resource path is reused in a new browser it will result in a 403 from Amazon CloudFront since no S3 object
        /// exists at that resource path.
        /// </summary>
        public bool Redirect404ToRoot { get; set; } = true;

        /// <summary>
        /// Configure if and how access logs are written for the CloudFront distribution.
        /// </summary>
        public AccessLoggingConfiguration? AccessLogging { get; set; }

        /// <summary>
        /// Configure the edge locations that will respond to request for the CloudFront distribution
        /// </summary>
        public Amazon.CDK.AWS.CloudFront.PriceClass PriceClass { get; set; } = Amazon.CDK.AWS.CloudFront.PriceClass.PRICE_CLASS_ALL;

        /// <summary>
        /// The AWS WAF (web application firewall) ACL arn
        /// </summary>
        public string? WebAclId { get; set; }

        /// <summary>
        /// Control if IPv6 should be enabled for the CloudFront distribution
        /// </summary>
        public bool EnableIpv6 { get; set; } = true;

        /// <summary>
        /// The maximum http version that users can use to communicate with the CloudFront distribution
        /// </summary>
        public Amazon.CDK.AWS.CloudFront.HttpVersion MaxHttpVersion { get; set; } = Amazon.CDK.AWS.CloudFront.HttpVersion.HTTP2;

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public Configuration()
        {

        }
#nullable restore warnings

        public Configuration(
            string indexDocument
            )
        {
            IndexDocument = indexDocument;
        }
    }
}
