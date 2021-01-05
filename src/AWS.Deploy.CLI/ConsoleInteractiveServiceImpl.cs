// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.CLI
{
    public class ConsoleInteractiveServiceImpl : IToolInteractiveService
    {
        public string ReadLine()
        {
            return Console.ReadLine();
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
