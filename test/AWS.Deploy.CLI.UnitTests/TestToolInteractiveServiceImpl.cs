// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TestToolInteractiveServiceImpl : IToolInteractiveService
    {
        public IList<string?> DebugMessages { get; } = new List<string?>();
        public IList<string?> OutputMessages { get; } = new List<string?>();
        public IList<string?> ErrorMessages { get; } = new List<string?>();

        private IList<string> InputCommands { get; set; }

        public TestToolInteractiveServiceImpl(): this(new List<string>())
        {
        }

        public TestToolInteractiveServiceImpl(IList<string> inputCommands)
        {
            InputCommands = inputCommands;
        }

        public void WriteLine(string? message)
        {
            OutputMessages.Add(message);
        }

        public void WriteDebugLine(string? message)
        {
            DebugMessages.Add(message);
        }

        public void WriteErrorLine(string? message)
        {
            ErrorMessages.Add(message);
        }

        public int InputReadCounter { get; private set; } = 0;

        public string ReadLine()
        {
            if (InputCommands.Count <= InputReadCounter)
            {
                throw new Exception("Attempting to read more then test case said");
            }

            var line = InputCommands[InputReadCounter];
            InputReadCounter++;
            return line;
        }

        public bool Diagnostics { get; set; }

        public bool OutputContains(string subString)
        {
            foreach (var message in OutputMessages)
            {
                if (message?.Contains(subString) ?? false)
                {
                    return true;
                }
            }

            return false;
        }

        public void Write(string? message)
        {
            OutputMessages.Add(message);
        }

        public void QueueConsoleInfos(params ConsoleKey[] keys)
        {
            foreach(var key in keys)
            {
                InputConsoleKeyInfos.Enqueue(new ConsoleKeyInfo(key.ToString()[0], key, false, false, false));
            }
        }

        public Queue<ConsoleKeyInfo> InputConsoleKeyInfos { get; } = new Queue<ConsoleKeyInfo>();
        public bool DisableInteractive { get; set; }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if(InputConsoleKeyInfos.Count == 0)
            {
                throw new Exception("No queued console key infos");
            }

            return InputConsoleKeyInfos.Dequeue();
        }
    }
}

