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

        public StreamWriter StdInWriter { get; set; }
        public StreamReader StdErrorReader { get; }
        public StreamReader StdOutReader { get; }

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

            // Save the stdOut stream position
            var stdOutReaderPosition = StdOutReader.BaseStream.Position;

            // Start writing to the stream from where we left the last time
            _stdOutWriter.BaseStream.Position = _stdOutWriterPosition;
            _stdOutWriter.WriteLine(message);
            _stdOutWriter.Flush();

            // Save the writer position for next time
            _stdOutWriterPosition = _stdOutWriter.BaseStream.Position;

            // Restore the stdOut to the original position
            StdOutReader.BaseStream.Position = stdOutReaderPosition;
        }

        public void WriteDebugLine(string message) => throw new System.NotImplementedException();

        public void WriteErrorLine(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);

            // Save the stdError stream position
            var stdErrorReaderPosition = StdErrorReader.BaseStream.Position;

            // Start writing to the stream from where we left the last time
            _stdErrorWriter.BaseStream.Position = _stdErrorWriterPosition;
            _stdErrorWriter.WriteLine(message);
            _stdErrorWriter.Flush();

            // Save the writer position for next time
            _stdErrorWriterPosition = _stdErrorWriter.BaseStream.Position;

            // Restore the stdOut to the original position
            StdErrorReader.BaseStream.Position = stdErrorReaderPosition;
        }

        public string ReadLine()
        {
            var stdInWriterPosition = StdInWriter.BaseStream.Position;

            // Restore the stdIn to the last read position
            _stdInReader.BaseStream.Position = _stdInReaderPosition;

            // read a line to return
            var readLine = _stdInReader.ReadLine();

            // Save the stdIn position for next time
            _stdInReaderPosition = _stdInReader.BaseStream.Position;

            StdInWriter.BaseStream.Position = stdInWriterPosition;

            Console.WriteLine(readLine);
            Debug.WriteLine(readLine);

            return readLine;
        }

        public bool Diagnostics { get; set; }
    }
}
