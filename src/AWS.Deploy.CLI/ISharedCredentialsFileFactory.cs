// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Represents a factory for creating <see cref="SharedCredentialsFile">
    /// </summary>
    public interface ISharedCredentialsFileFactory
    {
        /// <summary>
        /// Creates <see cref="SharedCredentialsFile">
        /// </summary>
        SharedCredentialsFile Create();
    }
}
