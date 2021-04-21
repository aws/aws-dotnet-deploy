// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Services
{
    public class InMemoryInteractiveServiceTests
    {
        [Fact]
        public void WriteLine()
        {
            var service = new InMemoryInteractiveService();

            service.WriteLine("Line 1");
            service.WriteLine("Line 2");
            service.WriteLine("Line 3");

            Assert.Equal("Line 1", service.StdOutReader.ReadLine());
            Assert.Equal("Line 2", service.StdOutReader.ReadLine());
            Assert.Equal("Line 3", service.StdOutReader.ReadLine());

            service.WriteLine("Line 4");
            service.WriteLine("Line 5");
            service.WriteLine("Line 6");

            Assert.Equal("Line 4", service.StdOutReader.ReadLine());
            Assert.Equal("Line 5", service.StdOutReader.ReadLine());
            Assert.Equal("Line 6", service.StdOutReader.ReadLine());
        }

        [Fact]
        public void WriteErrorLine()
        {
            var service = new InMemoryInteractiveService();

            service.WriteErrorLine("Error Line 1");
            service.WriteErrorLine("Error Line 2");
            service.WriteErrorLine("Error Line 3");

            Assert.Equal("Error Line 1", service.StdErrorReader.ReadLine());
            Assert.Equal("Error Line 2", service.StdErrorReader.ReadLine());
            Assert.Equal("Error Line 3", service.StdErrorReader.ReadLine());

            service.WriteErrorLine("Error Line 4");
            service.WriteErrorLine("Error Line 5");
            service.WriteErrorLine("Error Line 6");

            Assert.Equal("Error Line 4", service.StdErrorReader.ReadLine());
            Assert.Equal("Error Line 5", service.StdErrorReader.ReadLine());
            Assert.Equal("Error Line 6", service.StdErrorReader.ReadLine());
        }

        [Fact]
        public void ReadLine()
        {
            var service = new InMemoryInteractiveService();

            service.StdInWriter.WriteLine("Line 1");
            service.StdInWriter.WriteLine("Line 2");
            service.StdInWriter.WriteLine("Line 3");
            service.StdInWriter.Flush();

            Assert.Equal("Line 1", service.ReadLine());
            Assert.Equal("Line 2", service.ReadLine());
            Assert.Equal("Line 3", service.ReadLine());

            service.StdInWriter.WriteLine("Line 4");
            service.StdInWriter.WriteLine("Line 5");
            service.StdInWriter.WriteLine("Line 6");
            service.StdInWriter.Flush();

            Assert.Equal("Line 4", service.ReadLine());
            Assert.Equal("Line 5", service.ReadLine());
            Assert.Equal("Line 6", service.ReadLine());
        }
    }
}
