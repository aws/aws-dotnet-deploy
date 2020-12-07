// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.DeploymentCommon
{
    public class ProjectFileNotFoundException : Exception
    {
        public ProjectFileNotFoundException(string message) : base(message)
        {
        }
    }
}
