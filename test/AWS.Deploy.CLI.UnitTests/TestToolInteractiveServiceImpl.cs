// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace AWS.Deploy.CLI.UnitTests
{
    public class TestToolInteractiveServiceImpl : IToolInteractiveService
    {
        public IList<string> OutputMessages { get; } = new List<string>();
        public IList<string> ErrorMessages { get; } = new List<string>();

        private IList<string> InputCommands { get; set; }


        public TestToolInteractiveServiceImpl(IList<string> inputCommands)
        {
            InputCommands = inputCommands;
        }

        public void WriteLine(string message)
        {
            OutputMessages.Add(message);
        }

        public void WriteErrorLine(string message)
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

        public bool OutputContains(string subString)
        {
            foreach (var message in OutputMessages)
            {
                if (message.Contains(subString))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
