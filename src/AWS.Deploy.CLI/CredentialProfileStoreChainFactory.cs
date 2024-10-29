// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;

namespace AWS.Deploy.CLI
{
    /// <inheritdoc />
    public class CredentialProfileStoreChainFactory : ICredentialProfileStoreChainFactory
    {
        /// <inheritdoc />
        public CredentialProfileStoreChain Create()
        {
            return new CredentialProfileStoreChain();
        }
    }
}
