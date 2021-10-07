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
        private readonly Mock<CertificateVerificationEngine> _certificateVerificationEngineMock;

        public ServerModeSessionTests()
        {
            _httpClientHandlerMock = new Mock<HttpClientHandler>();
            _commandLineWrapper = new Mock<CommandLineWrapper>(false);
            _certificateVerificationEngineMock = new Mock<CertificateVerificationEngine>();
            _serverModeSession = new ServerModeSession(_commandLineWrapper.Object, _httpClientHandlerMock.Object, _certificateVerificationEngineMock.Object, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Start()
        {
            // Arrange
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();

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
            MockCommandLineWrapperRun(-100);
            MockHttpGet(HttpStatusCode.NotFound, TimeSpan.FromSeconds(5));
            MockVerifyCertificate();

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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGetThrows();
            MockVerifyCertificate();

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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.Forbidden);
            MockVerifyCertificate();

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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();
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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();
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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();
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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();
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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
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
            MockCommandLineWrapperRun(0, TimeSpan.FromSeconds(100));
            MockHttpGet(HttpStatusCode.OK);
            MockVerifyCertificate();
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

        private void MockCommandLineWrapperRun(int statusCode) =>
            _commandLineWrapper
                .Setup(wrapper => wrapper.Run(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(statusCode);

        private void MockCommandLineWrapperRun(int statusCode, TimeSpan delay) =>
            _commandLineWrapper
                .Setup(wrapper => wrapper.Run(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(statusCode, delay);

        private void MockVerifyCertificate() =>
            _certificateVerificationEngineMock
            .Setup(engine => engine.VerifyCertificate(It.IsAny<string>()));

        private Task<AWSCredentials> CredentialGenerator() => throw new NotImplementedException();
    }
}
