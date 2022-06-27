// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace AWS.Deploy.Constants
{
    internal static class CDK
    {
        /// <summary>
        /// Default version of CDK CLI
        /// </summary>
        public static readonly Version DefaultCDKVersion = Version.Parse("2.13.0");

        /// <summary>
        /// The name of the CDK bootstrap CloudFormation stack
        /// </summary>
        public const string CDKBootstrapStackName = "CDKToolkit";
    }
}
