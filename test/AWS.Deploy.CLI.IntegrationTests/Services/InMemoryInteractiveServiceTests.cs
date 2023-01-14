// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using NUnit.Framework;

namespace AWS.Deploy.CLI.IntegrationTests.Services
{
    [TestFixture]
    public class InMemoryInteractiveServiceTests
    {
        [Test]
        public void Write()
        {
            var service = new InMemoryInteractiveService();

            service.Write("Hello");

            Assert.AreEqual("Hello", service.StdOutReader.ReadToEnd());

            service.Write("World");

            Assert.AreEqual("World", service.StdOutReader.ReadToEnd());
        }

        [Test]
        public void WriteLine()
        {
            var service = new InMemoryInteractiveService();

            service.WriteLine("Line 1");
            service.WriteLine("Line 2");
            service.WriteLine("Line 3");

            Assert.AreEqual("Line 1", service.StdOutReader.ReadLine());
            Assert.AreEqual("Line 2", service.StdOutReader.ReadLine());
            Assert.AreEqual("Line 3", service.StdOutReader.ReadLine());

            service.WriteLine("Line 4");
            service.WriteLine("Line 5");
            service.WriteLine("Line 6");

            Assert.AreEqual("Line 4", service.StdOutReader.ReadLine());
            Assert.AreEqual("Line 5", service.StdOutReader.ReadLine());
            Assert.AreEqual("Line 6", service.StdOutReader.ReadLine());
        }

        [Test]
        public void WriteErrorLine()
        {
            var service = new InMemoryInteractiveService();

            service.WriteErrorLine("Error Line 1");
            service.WriteErrorLine("Error Line 2");
            service.WriteErrorLine("Error Line 3");

            Assert.AreEqual("Error Line 1", service.StdOutReader.ReadLine());
            Assert.AreEqual("Error Line 2", service.StdOutReader.ReadLine());
            Assert.AreEqual("Error Line 3", service.StdOutReader.ReadLine());

            service.WriteErrorLine("Error Line 4");
            service.WriteErrorLine("Error Line 5");
            service.WriteErrorLine("Error Line 6");

            Assert.AreEqual("Error Line 4", service.StdOutReader.ReadLine());
            Assert.AreEqual("Error Line 5", service.StdOutReader.ReadLine());
            Assert.AreEqual("Error Line 6", service.StdOutReader.ReadLine());
        }

        [Test]
        public void WriteDebugLine()
        {
            var service = new InMemoryInteractiveService();

            service.WriteDebugLine("Debug Line 1");
            service.WriteDebugLine("Debug Line 2");
            service.WriteDebugLine("Debug Line 3");

            Assert.AreEqual("Debug Line 1", service.StdOutReader.ReadLine());
            Assert.AreEqual("Debug Line 2", service.StdOutReader.ReadLine());
            Assert.AreEqual("Debug Line 3", service.StdOutReader.ReadLine());

            service.WriteDebugLine("Debug Line 4");
            service.WriteDebugLine("Debug Line 5");
            service.WriteDebugLine("Debug Line 6");

            Assert.AreEqual("Debug Line 4", service.StdOutReader.ReadLine());
            Assert.AreEqual("Debug Line 5", service.StdOutReader.ReadLine());
            Assert.AreEqual("Debug Line 6", service.StdOutReader.ReadLine());
        }

        [Test]
        public void ReadLine()
        {
            var service = new InMemoryInteractiveService();

            service.StdInWriter.WriteLine("Line 1");
            service.StdInWriter.WriteLine("Line 2");
            service.StdInWriter.WriteLine("Line 3");
            service.StdInWriter.Flush();

            Assert.AreEqual("Line 1", service.ReadLine());
            Assert.AreEqual("Line 2", service.ReadLine());
            Assert.AreEqual("Line 3", service.ReadLine());

            service.StdInWriter.WriteLine("Line 4");
            service.StdInWriter.WriteLine("Line 5");
            service.StdInWriter.WriteLine("Line 6");
            service.StdInWriter.Flush();

            Assert.AreEqual("Line 4", service.ReadLine());
            Assert.AreEqual("Line 5", service.ReadLine());
            Assert.AreEqual("Line 6", service.ReadLine());
        }

        [Test]
        public void ReadKey()
        {
            var service = new InMemoryInteractiveService();

            service.StdInWriter.Write(ConsoleKey.A);
            service.StdInWriter.Write(ConsoleKey.B);
            service.StdInWriter.Write(ConsoleKey.C);
            service.StdInWriter.Flush();

            Assert.AreEqual(ConsoleKey.A, service.ReadKey(false).Key);
            Assert.AreEqual(ConsoleKey.B, service.ReadKey(false).Key);
            Assert.AreEqual(ConsoleKey.C, service.ReadKey(false).Key);

            service.StdInWriter.Write(ConsoleKey.D);
            service.StdInWriter.Write(ConsoleKey.E);
            service.StdInWriter.Write(ConsoleKey.F);
            service.StdInWriter.Flush();

            Assert.AreEqual(ConsoleKey.D, service.ReadKey(false).Key);
            Assert.AreEqual(ConsoleKey.E, service.ReadKey(false).Key);
            Assert.AreEqual(ConsoleKey.F, service.ReadKey(false).Key);
        }

        [Test]
        public void ReadLineSetToNull()
        {
            var service = new InMemoryInteractiveService();
            Assert.Throws<InvalidOperationException>(() => service.ReadLine());
        }
    }
}
