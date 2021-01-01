// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;

namespace AWS.Deploy.CLI
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        private readonly Verbosity _verbosity;

        public ConsoleInteractiveServiceImpl(Verbosity verbosity)
        {
            _verbosity = verbosity;
        }
        
        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public void WriteDebugLine(string message)
        {
            Debug.WriteLine(message);

            if (_verbosity == Verbosity.All)
                Console.WriteLine($"DEBUG: {message}");
        }

        public void WriteErrorLine(string message)
        {
            Console.Error.WriteLine(message);

            Debug.WriteLine(message);
        }

        public void WriteLine(string message)
        {
            if (_verbosity >= Verbosity.Message)
                Console.WriteLine(message);

            Debug.WriteLine(message);
        }

        public enum Verbosity
        {
            Error = 0,
            Message = 10,
            All = 100
        }
    }
}
