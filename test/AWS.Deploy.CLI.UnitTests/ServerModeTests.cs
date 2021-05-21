// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Deploy.CLI.Commands;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ServerModeTests
    {
        [Fact]
        public async Task TcpPortIsInUseTest()
        {
            var serverModeCommand1 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, false);
            var serverModeCommand2 = new ServerModeCommand(new TestToolInteractiveServiceImpl(), 1234, null, false);

            var serverModeTask1 = serverModeCommand1.ExecuteAsync();
            var serverModeTask2 = serverModeCommand2.ExecuteAsync();

            await Task.WhenAny(serverModeTask1, serverModeTask2);

            Assert.False(serverModeTask1.IsCompleted);

            Assert.True(serverModeTask2.IsCompleted);
            Assert.True(serverModeTask2.IsFaulted);

            Assert.NotNull(serverModeTask2.Exception);
            Assert.Single(serverModeTask2.Exception.InnerExceptions);

            Assert.IsType<TcpPortInUseException>(serverModeTask2.Exception.InnerException);
        }
    }
}
