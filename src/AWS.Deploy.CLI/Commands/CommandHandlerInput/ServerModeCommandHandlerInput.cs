// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.Commands.CommandHandlerInput
{
    public class ServerModeCommandHandlerInput
    {
        public int Port { get; set; }
        public int ParentPid { get; set; }
        public bool EncryptionKeyInfoStdIn { get; set; }
        public bool Diagnostics { get; set; }
    }
}
