// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;

namespace AWS.Deploy.Shell
{
    public interface ICommandRunnerDelegate
    {
        void ErrorDataReceived(ProcessStartInfo processStartInfo, string data);
        void OutputDataReceived(ProcessStartInfo processStartInfo, string data);
        Action<ProcessStartInfo> BeforeStart { get; set; }
    }
}
