// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Represents a factory for creating <see cref="AWSCredentials">
    /// </summary>
    public interface IAWSCredentialsFactory
    {
        /// <summary>
        /// Creates <see cref="AWSCredentials">
        /// </summary>
        AWSCredentials Create();
    }
}
