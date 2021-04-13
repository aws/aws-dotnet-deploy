// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI
{
    public interface IToolInteractiveService
    {
        void Write(string message);
        void WriteLine(string message);
        void WriteDebugLine(string message);
        void WriteErrorLine(string message);

        string ReadLine();
        bool Diagnostics { get; set; }
        ConsoleKeyInfo ReadKey(bool intercept);
    }

    public static class ToolInteractiveServiceExtensions
    {
        public static void WriteLine(this IToolInteractiveService service)
        {
            service.WriteLine(string.Empty);
        }
    }
}
