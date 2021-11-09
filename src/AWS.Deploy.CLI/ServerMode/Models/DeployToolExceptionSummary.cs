// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.CLI.ServerMode.Models
{
    public class DeployToolExceptionSummary
    {
        public string ErrorCode { get; set; }
        public string Message { get; set; }

        public DeployToolExceptionSummary(string errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }
}
