using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Moq;
using Moq.Protected;
using Xunit;

namespace AWS.Deploy.ServerMode.Client.UnitTests
{
    public class ServerModeSessionTests
    {
        private readonly ServerModeSession _serverModeSession;
        private readonly Mock<HttpClientHandler> _httpClientHandlerMock;
        private readonly Mock<CommandLineWrapper> _commandLineWrapper;

        public ServerModeSessionTests()
        {
            _httpClientHandlerMock = new Mock<HttpClientHandler>();
            _commandLineWrapper = new Mock<CommandLineWrapper>(false);
            _serverModeSession = new ServerModeSession(_commandLineWrapper.Object, _httpClientHandlerMock.Object, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Start()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);

            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await _serverModeSession.Start(CancellationToken.None);
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task Start_PortUnavailable()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = -100 };
            MockCommandLineWrapperRun(runResult);
            MockHttpGet(HttpStatusCode.NotFound, TimeSpan.FromSeconds(5));

            // Act & Assert
            await Assert.ThrowsAsync<PortUnavailableException>(async () =>
            {
                await _serverModeSession.Start(CancellationToken.None);
            });
        }

        [Fact]
        public async Task Start_HttpGetThrows()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGetThrows();

            // Act & Assert
            await Assert.ThrowsAsync<InternalServerModeException>(async () =>
            {
                await _serverModeSession.Start(CancellationToken.None);
            });
        }

        [Fact]
        public async Task Start_HttpGetForbidden()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.Forbidden);

            // Act & Assert
            await Assert.ThrowsAsync<InternalServerModeException>(async () =>
            {
                await _serverModeSession.Start(CancellationToken.None);
            });
        }

        [Fact]
        public async Task IsAlive_BaseUrlNotInitialized()
        {
            // Act
            var isAlive = await _serverModeSession.IsAlive(CancellationToken.None);

            // Assert
            Assert.False(isAlive);
        }

        [Fact]
        public async Task IsAlive_GetAsyncThrows()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            await _serverModeSession.Start(CancellationToken.None);

            MockHttpGetThrows();

            // Act
            var isAlive = await _serverModeSession.IsAlive(CancellationToken.None);

            // Assert
            Assert.False(isAlive);
        }

        [Fact]
        public async Task IsAlive_HttpResponseSuccess()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            await _serverModeSession.Start(CancellationToken.None);

            // Act
            var isAlive = await _serverModeSession.IsAlive(CancellationToken.None);

            // Assert
            Assert.True(isAlive);
        }

        [Fact]
        public async Task IsAlive_HttpResponseFailure()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            await _serverModeSession.Start(CancellationToken.None);

            MockHttpGet(HttpStatusCode.Forbidden);

            // Act
            var isAlive = await _serverModeSession.IsAlive(CancellationToken.None);

            // Assert
            Assert.False(isAlive);
        }

        [Fact]
        public async Task TryGetRestAPIClient()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            await _serverModeSession.Start(CancellationToken.None);

            // Act
            var success = _serverModeSession.TryGetRestAPIClient(CredentialGenerator, out var restApiClient);

            // Assert
            Assert.True(success);
            Assert.NotNull(restApiClient);
        }

        [Fact]
        public void TryGetRestAPIClient_WithoutStart()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);

            // Act
            var success = _serverModeSession.TryGetRestAPIClient(CredentialGenerator, out var restApiClient);

            // Assert
            Assert.False(success);
            Assert.Null(restApiClient);
        }

        [Fact]
        public async Task TryGetDeploymentCommunicationClient()
        {
            // Arrange
            var runResult = new RunResult { ExitCode = 0 };
            MockCommandLineWrapperRun(runResult, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            await _serverModeSession.Start(CancellationToken.None);

            // Act
            var success = _serverModeSession.TryGetDeploymentCommunicationClient(out var deploymentCommunicationClient);

            // Assert
            Assert.True(success);
            Assert.NotNull(deploymentCommunicationClient);
        }

        [Fact]
        public void TryGetDeploymentCommunicationClient_WithoutStart()
        {
            var success = _serverModeSession.TryGetDeploymentCommunicationClient(out var deploymentCommunicationClient);
            Assert.False(success);
            Assert.Null(deploymentCommunicationClient);
        }

        private void MockHttpGet(HttpStatusCode httpStatusCode)
        {
            var response = new HttpResponseMessage(httpStatusCode);

            _httpClientHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void MockHttpGet(HttpStatusCode httpStatusCode, TimeSpan delay)
        {
            var response = new HttpResponseMessage(httpStatusCode);

            _httpClientHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response, delay);
        }

        private void MockHttpGetThrows() =>
            _httpClientHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Throws(new Exception());

        private void MockCommandLineWrapperRun(RunResult runResult) =>
            _commandLineWrapper
                .Setup(wrapper => wrapper.Run(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(runResult);

        private void MockCommandLineWrapperRun(RunResult runResult, TimeSpan delay) =>
            _commandLineWrapper
                .Setup(wrapper => wrapper.Run(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(runResult, delay);

        private Task<AWSCredentials> CredentialGenerator() => throw new NotImplementedException();
    }
}
