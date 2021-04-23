// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.IO;

namespace AWS.Deploy.CLI.IntegrationTests.Services
{
    public class InMemoryInteractiveService : IToolInteractiveService
    {
        private long _stdOutWriterPosition;
        private long _stdErrorWriterPosition;
        private long _stdInReaderPosition;
        private readonly StreamWriter _stdOutWriter;
        private readonly StreamWriter _stdErrorWriter;
        private readonly StreamReader _stdInReader;

        /// <summary>
        /// Allows consumers to write string to the BaseStream
        /// which will be returned on <see cref="ReadLine"/> method call.
        /// </summary>
        public StreamWriter StdInWriter { get; }

        /// <summary>
        /// Allows consumers to read string which are written via <see cref="WriteLine"/>
        /// </summary>
        public StreamReader StdOutReader { get; }

        /// <summary>
        /// Allows consumers to read string which are written via <see cref="WriteErrorLine"/>
        /// </summary>
        public StreamReader StdErrorReader { get; }

        public InMemoryInteractiveService()
        {
            var stdOut = new MemoryStream();
            _stdOutWriter = new StreamWriter(stdOut);
            StdOutReader = new StreamReader(stdOut);

            var stdError = new MemoryStream();
            _stdErrorWriter = new StreamWriter(stdError);
            StdErrorReader = new StreamReader(stdError);

            var stdIn = new MemoryStream();
            _stdInReader = new StreamReader(stdIn);
            StdInWriter = new StreamWriter(stdIn);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);

            // Save BaseStream position, it must be only modified the consumer of StdOutReader
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

        public void WriteDebugLine(string message) => throw new System.NotImplementedException();

        public void WriteErrorLine(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);

            // Save BaseStream position, it must be only modified the consumer of StdErrorReader
            // After writing to the BaseStream, we will reset it to the original position.
            var stdErrorReaderPosition = StdErrorReader.BaseStream.Position;

            // Reset the BaseStream to the last save position to continue writing from where we left.
            _stdErrorWriter.BaseStream.Position = _stdErrorWriterPosition;
            _stdErrorWriter.WriteLine(message);
            _stdErrorWriter.Flush();

            // Save the BaseStream position for future writes.
            _stdErrorWriterPosition = _stdErrorWriter.BaseStream.Position;

            // Reset the BaseStream position to the original position
            StdErrorReader.BaseStream.Position = stdErrorReaderPosition;
        }

        public string ReadLine()
        {
            var stdInWriterPosition = StdInWriter.BaseStream.Position;

            // Reset the BaseStream to the last save position to continue writing from where we left.
            _stdInReader.BaseStream.Position = _stdInReaderPosition;

            var readLine = _stdInReader.ReadLine();

            // Save the BaseStream position for future reads.
            _stdInReaderPosition = _stdInReader.BaseStream.Position;

            // Reset the BaseStream position to the original position
            StdInWriter.BaseStream.Position = stdInWriterPosition;

            Console.WriteLine(readLine);
            Debug.WriteLine(readLine);

            return readLine;
        }

        public bool Diagnostics { get; set; }
    }
}
