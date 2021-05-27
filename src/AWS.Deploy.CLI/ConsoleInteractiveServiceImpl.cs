// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        private readonly bool _diagnosticLoggingEnabled;

        public ConsoleInteractiveServiceImpl(bool diagnosticLoggingEnabled = false)
        {
            _diagnosticLoggingEnabled = diagnosticLoggingEnabled;
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public bool Diagnostics { get; set; }
        public bool DisableInteractive { get; set; }

        public void WriteDebugLine(string message)
        {
            if (_diagnosticLoggingEnabled)
                Console.WriteLine($"DEBUG: {message}");
        }

        public void WriteErrorLine(string message)
        {
            var color = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = color;
            }
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Write(string message)
        {
            Console.Write(message);
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }
    }
}
