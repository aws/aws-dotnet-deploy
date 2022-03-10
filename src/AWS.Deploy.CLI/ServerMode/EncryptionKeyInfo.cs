// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace AWS.Deploy.CLI.ServerMode
{
    public class EncryptionKeyInfo
    {
        public const string VERSION_1_0 = "1.0";

        /// <summary>
        /// The version of the key info. If the property is set to a value that server mode has not implemented to support then
        /// a fatal exception is thrown.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Encryption key base 64 encoded
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Encryption IV base 64 encoded
        /// </summary>
        public string? IV { get; set; }

        public static EncryptionKeyInfo ParseStdInKeyInfo(string input)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(input));
                var keyInfo = JsonConvert.DeserializeObject<EncryptionKeyInfo>(json);

                if(string.IsNullOrEmpty(keyInfo?.Key))
                {
                    throw new InvalidEncryptionKeyInfoException("The symmetric key is missing a \"Key\" attribute.");
                }

                return keyInfo;
            }
            catch (Exception)
            {
                throw new InvalidEncryptionKeyInfoException($"The symmetric key has not been passed to Stdin or is invalid.");
            }
        }
    }
}
