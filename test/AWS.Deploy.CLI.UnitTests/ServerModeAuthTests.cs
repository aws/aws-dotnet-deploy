// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Security.Claims;
using System.Threading.Tasks;
using AWS.Deploy.CLI.ServerMode;
using AWS.Deploy.CLI.ServerMode.Services;

using Xunit;
using Amazon.Runtime;
using AWS.Deploy.ServerMode.Client;
using System.Security.Cryptography;
using Newtonsoft.Json;
using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.UnitTests.Utilities;
using System.Threading;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ServerModeAuthTests
    {
        [Fact]
        public async Task GetBasicCredentialsFromClaimsPrincipal()
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId, "accessKeyId"),
                new Claim(AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey, "secretKey")
            });

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var awsCredentials = claimsPrincipal.ToAWSCredentials();
            Assert.IsType<Amazon.Runtime.BasicAWSCredentials>(awsCredentials);

            var imutCreds = await awsCredentials.GetCredentialsAsync();
            Assert.Equal("accessKeyId", imutCreds.AccessKey);
            Assert.Equal("secretKey", imutCreds.SecretKey);
        }

        [Fact]
        public async Task GetSessionCredentialsFromClaimsPrincipal()
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
                new Claim(AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId, "accessKeyId"),
                new Claim(AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey, "secretKey"),
                new Claim(AwsCredentialsAuthenticationHandler.ClaimAwsSessionToken, "sessionToken"),
            });

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var awsCredentials = claimsPrincipal.ToAWSCredentials();
            Assert.IsType<Amazon.Runtime.SessionAWSCredentials>(awsCredentials);

            var imutCreds = await awsCredentials.GetCredentialsAsync();
            Assert.Equal("accessKeyId", imutCreds.AccessKey);
            Assert.Equal("secretKey", imutCreds.SecretKey);
            Assert.Equal("sessionToken", imutCreds.Token);
        }

        [Fact]
        public void NoCredentialsSetForClaimsPrincipal()
        {
            var claimsIdentity = new ClaimsIdentity(new Claim[]
            {
            });

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var awsCredentials = claimsPrincipal.ToAWSCredentials();
            Assert.Null(awsCredentials);
        }

        [Fact]
        public void ProcessAuthorizationHeaderSuccess()
        {
            var request = new HttpRequestMessage();
            var creds = new ImmutableCredentials("accessKeyId", "secretKey", "token");

            ServerModeHttpClientAuthorizationHandler.AddAuthorizationHeader(request, creds);

            if (!request.Headers.TryGetValues("Authorization", out var value))
            {
                throw new Exception("Missing Authorization header");
            }

            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(value.FirstOrDefault(), new NoEncryptionProvider());
            Assert.True(authResults.Succeeded);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailNoSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("no-schema-value", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Contains("Incorrect format Authorization header", authResults.Failure.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailWrongSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("wrong-schema value", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Contains("Unsupported authorization schema", authResults.Failure.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailInValidJson()
        {
            var base64BadJson = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("you are not json"));
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader($"aws-deploy-tool-server-mode {base64BadJson}", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal("Error decoding authorization value", authResults.Failure.Message);
        }

        [Fact]
        public void PassCredentialsEncrypted()
        {
            var aes = Aes.Create();

            var request = new HttpRequestMessage();
            var creds = new ImmutableCredentials("accessKeyId", "secretKey", "token");

            ServerModeHttpClientAuthorizationHandler.AddAuthorizationHeader(request, creds, aes);

            if (!request.Headers.TryGetValues("Authorization", out var value))
            {
                throw new Exception("Missing Authorization header");
            }

            var authPayloadBase64 = value.FirstOrDefault().Split(' ')[1];
            var authPayload = Encoding.UTF8.GetString(Convert.FromBase64String(authPayloadBase64));

            // This should fail because the payload is encrypted.
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject(authPayload));

            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(value.FirstOrDefault(), new AesEncryptionProvider(aes));
            Assert.True(authResults.Succeeded);

            var accessKeyId = authResults.Principal.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId, x.Type))?.Value;
            Assert.Equal(creds.AccessKey, accessKeyId);

            var secretKey = authResults.Principal.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey, x.Type))?.Value;
            Assert.Equal(creds.SecretKey, secretKey);

            var token = authResults.Principal.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsSessionToken, x.Type))?.Value;
            Assert.Equal(creds.Token, token);
        }

        [Fact]
        public async Task MissingEncryptionInfoVersion()
        {
            InMemoryInteractiveService interactiveService = new InMemoryInteractiveService();

            var portNumber = 4010;

            var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            var keyInfo = new EncryptionKeyInfo
            {
                Key = Convert.ToBase64String(aes.Key),
                IV = Convert.ToBase64String(aes.IV)
            };
            var keyInfoStdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyInfo)));
            await interactiveService.StdInWriter.WriteAsync(keyInfoStdin);
            await interactiveService.StdInWriter.FlushAsync();

            var serverCommand = new ServerModeCommand(interactiveService, portNumber, null, true);


            var cancelSource = new CancellationTokenSource();
            Exception actualException = null;
            try
            {
                await serverCommand.ExecuteAsync(cancelSource.Token);
            }
            catch(InvalidEncryptionKeyInfoException e)
            {
                actualException = e;
            }
            finally
            {
                cancelSource.Cancel();
            }

            Assert.NotNull(actualException);
            Assert.Equal("Missing require \"Version\" property in encryption key info", actualException.Message);
        }

        [Fact]
        public async Task EncryptionWithInvalidVersion()
        {
            InMemoryInteractiveService interactiveService = new InMemoryInteractiveService();

            var portNumber = 4010;

            var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            var keyInfo = new EncryptionKeyInfo
            {
                Version = "not-valid",
                Key = Convert.ToBase64String(aes.Key),
                IV = Convert.ToBase64String(aes.IV)
            };
            var keyInfoStdin = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyInfo)));
            await interactiveService.StdInWriter.WriteAsync(keyInfoStdin);
            await interactiveService.StdInWriter.FlushAsync();

            var serverCommand = new ServerModeCommand(interactiveService, portNumber, null, true);


            var cancelSource = new CancellationTokenSource();
            Exception actualException = null;
            try
            {
                await serverCommand.ExecuteAsync(cancelSource.Token);
            }
            catch (InvalidEncryptionKeyInfoException e)
            {
                actualException = e;
            }
            finally
            {
                cancelSource.Cancel();
            }

            Assert.NotNull(actualException);
            Assert.Equal("Unsupported encryption key info not-valid", actualException.Message);
        }
    }
}
