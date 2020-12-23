// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CloudFormation;

namespace AWS.Deploy.CLI.CloudFormation
{
    internal static class StackStatusExtension
    {
        public static bool IsDeleted(this StackStatus stackStatus)
        {
            return stackStatus.Value.Equals("DELETE_COMPLETE");
        }

        public static bool IsFailed(this StackStatus stackStatus)
        {
            return stackStatus.Value.EndsWith("FAILED");
        }

        public static bool IsInProgress(this StackStatus stackStatus)
        {
            return stackStatus.Value.EndsWith("_IN_PROGRESS");
        }
    }
}
