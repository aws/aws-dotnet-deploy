// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using Amazon.Runtime;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.CLI.Utilities
{
    /// <summary>
    /// Handle the callback from AWSCredentials when an MFA token code is required.
    /// </summary>
    internal class AssumeRoleMfaTokenCodeCallback
    {
        private readonly AssumeRoleAWSCredentialsOptions _options;
        private readonly IToolInteractiveService _toolInteractiveService;
        private readonly IDirectoryManager _directoryManager;

        internal AssumeRoleMfaTokenCodeCallback(IToolInteractiveService toolInteractiveService, IDirectoryManager directoryManager, AssumeRoleAWSCredentialsOptions options)
        {
            _toolInteractiveService = toolInteractiveService;
            _options = options;
            _directoryManager = directoryManager;
        }

        internal string Execute()
        {
            _toolInteractiveService.WriteLine();
            _toolInteractiveService.WriteLine($"Enter MFA code for {_options.MfaSerialNumber}: ");
            var consoleUtilites = new ConsoleUtilities(_toolInteractiveService, _directoryManager);
            var code = consoleUtilites.ReadSecretFromConsole();
            
            return code;
        }
    }
}
