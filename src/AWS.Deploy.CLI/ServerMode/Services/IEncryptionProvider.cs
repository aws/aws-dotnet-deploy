// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public interface IEncryptionProvider
    {
        byte[] Decrypt(byte[] encryptedData, byte[]? generatedIV);
    }
}
