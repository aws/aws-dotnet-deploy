// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.ServerMode.Services
{
    public class AesEncryptionProvider : IEncryptionProvider
    {
        private readonly Aes _aes;

        public AesEncryptionProvider(Aes aes)
        {
            _aes = aes;
        }

        public byte[] Decrypt(byte[] encryptedData, byte[]? generatedIV)
        {
            var decryptor = _aes.CreateDecryptor(_aes.Key, generatedIV);

            using var inputStream = new MemoryStream(encryptedData);
            using var decryptStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);

            using var outputStream = new MemoryStream();
            decryptStream.CopyTo(outputStream);

            return outputStream.ToArray();
        }
    }
}
