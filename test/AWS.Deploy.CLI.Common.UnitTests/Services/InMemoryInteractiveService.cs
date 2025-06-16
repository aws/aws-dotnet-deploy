// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.IO;
using AWS.Deploy.Orchestration;

namespace AWS.Deploy.CLI.IntegrationTests.Services
{
    public class InMemoryInteractiveService : IToolInteractiveService, IOrchestratorInteractiveService
    {
        private static readonly object s_readToEndLocker = new object();
        private readonly object _writeLocker = new object();
        private readonly object _readLocker = new object();
        private long _stdOutWriterPosition;
        private long _stdInReaderPosition;
        private readonly StreamWriter _stdOutWriter;
        private readonly StreamReader _stdInReader;

        /// <summary>
        /// Allows consumers to write string to the BaseStream
        /// which will be returned on <see cref="ReadLine"/> method call.
        /// </summary>
        public StreamWriter StdInWriter { get; }

        /// <summary>
        /// Allows consumers to read string which is written via <see cref="WriteLine"/>
        /// </summary>
        public StreamReader StdOutReader { get; }

        public InMemoryInteractiveService()
        {
            var stdOut = new MemoryStream();
            _stdOutWriter = new StreamWriter(stdOut);
            StdOutReader = new StreamReader(stdOut);

            var stdIn = new MemoryStream();
            _stdInReader = new StreamReader(stdIn);
            StdInWriter = new StreamWriter(stdIn);
        }

        public void Write(string? message)
        {
            lock (_writeLocker)
            {
                Debug.Write(message);

                // Save BaseStream position, it must be only modified by the consumer of StdOutReader
                // After writing to the BaseStream, we will reset it to the original position.
                var stdOutReaderPosition = StdOutReader.BaseStream.Position;

                // Reset the BaseStream to the last save position to continue writing from where we left.
                _stdOutWriter.BaseStream.Position = _stdOutWriterPosition;
                _stdOutWriter.Write(message);
                _stdOutWriter.Flush();

                // Save the BaseStream position for future writes.
                _stdOutWriterPosition = _stdOutWriter.BaseStream.Position;

                // Reset the BaseStream position to the original position
                StdOutReader.BaseStream.Position = stdOutReaderPosition;
            }
        }

        public void WriteLine(string? message)
        {
            lock (_writeLocker)
            {
                Debug.WriteLine(message);

                // Save BaseStream position, it must be only modified by the consumer of StdOutReader
                // After writing to the BaseStream, we will reset it to the original position.
                var stdOutReaderPosition = StdOutReader.BaseStream.Position;

                // Reset the BaseStream to the last save position to continue writing from where we left.
                _stdOutWriter.BaseStream.Position = _stdOutWriterPosition;
                _stdOutWriter.WriteLine(message);
                _stdOutWriter.Flush();

                // Save the BaseStream position for future writes.
                _stdOutWriterPosition = _stdOutWriter.BaseStream.Position;

                // Reset the BaseStream position to the original position
                StdOutReader.BaseStream.Position = stdOutReaderPosition;
            }
        }

        public void WriteDebugLine(string? message)
        {
            WriteLine(message);
        }

        public void WriteErrorLine(string? message)
        {
            WriteLine(message);
        }

        public string ReadLine()
        {
            lock (_readLocker)
            {
                var stdInWriterPosition = StdInWriter.BaseStream.Position;

                // Reset the BaseStream to the last save position to continue writing from where we left.
                _stdInReader.BaseStream.Position = _stdInReaderPosition;

                var readLine = _stdInReader.ReadLine();

                if (readLine == null)
                {
                    throw new InvalidOperationException();
                }

                // Save the BaseStream position for future reads.
                _stdInReaderPosition = _stdInReader.BaseStream.Position;

                // Reset the BaseStream position to the original position
                StdInWriter.BaseStream.Position = stdInWriterPosition;

                WriteLine(readLine);

                return readLine;
            }
        }

        public bool Diagnostics { get; set; }

        public bool DisableInteractive { get; set; }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            var stdInWriterPosition = StdInWriter.BaseStream.Position;

            // Reset the BaseStream to the last save position to continue writing from where we left.
            _stdInReader.BaseStream.Position = _stdInReaderPosition;

            var keyChar = _stdInReader.Read();
            var key = new ConsoleKeyInfo((char)keyChar, (ConsoleKey)keyChar, false, false, false);

            // Save the BaseStream position for future reads.
            _stdInReaderPosition = _stdInReader.BaseStream.Position;

            // Reset the BaseStream position to the original position
            StdInWriter.BaseStream.Position = stdInWriterPosition;

            WriteLine(key.ToString());

            return key;
        }

        public void ReadStdOutStartToEnd()
        {
            lock (s_readToEndLocker)
            {
                // Save BaseStream position, it must be only modified by the consumer of StdOutReader
                // After writing to the BaseStream, we will reset it to the original position.
                var stdOutReaderPosition = StdOutReader.BaseStream.Position;

                StdOutReader.BaseStream.Position = 0;

                var output = StdOutReader.ReadToEnd();

                Console.WriteLine(output);
                Debug.WriteLine(output);

                // Reset the BaseStream position to the original position
                StdOutReader.BaseStream.Position = stdOutReaderPosition;
            }
        }

        public void LogSectionStart(string message, string? description)
        {
            WriteLine(message);
        }

        public void LogErrorMessage(string? message)
        {
            WriteLine(message);
        }

        public void LogInfoMessage(string? message)
        {
            WriteLine(message);
        }

        public void LogDebugMessage(string? message)
        {
            WriteLine(message);
        }
    }
}
