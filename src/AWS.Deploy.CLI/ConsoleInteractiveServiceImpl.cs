// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.CLI.Commands.CommandHandlerInput;

namespace AWS.Deploy.CLI
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        private readonly ICommandInputService _commandInputService;

        public ConsoleInteractiveServiceImpl(ICommandInputService commandInputService)
        {
            _commandInputService = commandInputService;
            Console.Title = Constants.CLI.CLI_APP_NAME;
        }

        public string ReadLine()
        {
            return Console.ReadLine() ?? string.Empty;
        }

        public bool DisableInteractive { get; set; }

        public void WriteDebugLine(string? message)
        {
            if (_commandInputService.Diagnostics)
                Console.WriteLine($"DEBUG: {message}");
        }

        public void WriteErrorLine(string? message)
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

        public void WriteLine(string? message)
        {
            Console.WriteLine(message);
        }

        public void Write(string? message)
        {
            Console.Write(message);
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }
    }
}
