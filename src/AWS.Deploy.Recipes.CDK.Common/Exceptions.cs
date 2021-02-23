// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Recipes.CDK.Common
{
    public class InvalidAWSDeployToolSettingsException : Exception
    {
        public InvalidAWSDeployToolSettingsException(string message) : base(message)
        {
        }
    }
}
