// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;

namespace AWS.Deploy.CLI
{
    /// <summary>
    /// Represents a factory for creating <see cref="CredentialProfileStoreChain">
    /// </summary>
    public interface ICredentialProfileStoreChainFactory
    {
        /// <summary>
        /// Creates a <see cref="CredentialProfileStoreChain">
        /// </summary>
        CredentialProfileStoreChain Create();
    }
}
