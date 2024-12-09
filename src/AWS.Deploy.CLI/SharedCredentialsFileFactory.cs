// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;

namespace AWS.Deploy.CLI
{
    /// <inheritdoc />
    public class SharedCredentialsFileFactory : ISharedCredentialsFileFactory
    {
        /// <inheritdoc />
        public SharedCredentialsFile Create()
        {
            return new SharedCredentialsFile();
        }
    }
}
