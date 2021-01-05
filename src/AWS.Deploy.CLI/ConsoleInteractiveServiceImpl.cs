// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        private readonly bool _diagnosticLoggingEnabled;

        public ConsoleInteractiveServiceImpl(bool diagnosticLoggingEnabled)
        {
            _diagnosticLoggingEnabled = diagnosticLoggingEnabled;
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

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
    }
}
