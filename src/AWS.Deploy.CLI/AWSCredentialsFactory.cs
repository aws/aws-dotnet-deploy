// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;
using Amazon.Runtime.Credentials;

namespace AWS.Deploy.CLI
{
    /// <inheritdoc />
    public class AWSCredentialsFactory : IAWSCredentialsFactory
    {
        /// <inheritdoc />
        public AWSCredentials Create()
        {
            return DefaultAWSCredentialsIdentityResolver.GetCredentials();
        }
    }
}
