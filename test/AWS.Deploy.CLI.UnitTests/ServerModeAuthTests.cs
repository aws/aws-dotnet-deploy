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
using System.Globalization;
using AWS.Deploy.CLI.Commands.Settings;
using AWS.Deploy.CLI.IntegrationTests.Services;

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

            var awsCredentials = Assert.IsType<Amazon.Runtime.BasicAWSCredentials>(claimsPrincipal.ToAWSCredentials());

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

            var awsCredentials = Assert.IsType<Amazon.Runtime.SessionAWSCredentials>(claimsPrincipal.ToAWSCredentials());

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

            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(value.First(), new NoEncryptionProvider());
            Assert.True(authResults.Succeeded);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailNoSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("no-schema-value", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Contains("Incorrect format Authorization header", authResults.Failure!.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailWrongSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("wrong-schema value", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Contains("Unsupported authorization schema", authResults.Failure!.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailInValidJson()
        {
            var base64BadJson = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("you are not json"));
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader($"aws-deploy-tool-server-mode {base64BadJson}", new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal("Error decoding authorization value", authResults.Failure?.Message);
        }

        [Fact]
        public void AuthPassCredentialsEncrypted()
        {
            var aes = Aes.Create();

            var request = new HttpRequestMessage();
            var creds = new ImmutableCredentials("accessKeyId", "secretKey", "token");

            ServerModeHttpClientAuthorizationHandler.AddAuthorizationHeader(request, creds, aes);

            if (!request.Headers.TryGetValues("Authorization", out var headerValues))
            {
                throw new Exception("Missing Authorization header");
            }

            var values = headerValues as string[] ?? headerValues.ToArray();
            var authHeader = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                throw new Exception("Authorization header is empty");
            }

            var authPayloadBase64 = authHeader.Split(' ')[1];
            var authPayload = Encoding.UTF8.GetString(Convert.FromBase64String(authPayloadBase64));

            // This should fail because the payload is encrypted.
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject(authPayload));

            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(authHeader, new AesEncryptionProvider(aes));
            Assert.True(authResults.Succeeded);

            var accessKeyId = authResults.Principal?.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId, x.Type))?.Value;
            Assert.Equal(creds.AccessKey, accessKeyId);

            var secretKey = authResults.Principal?.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey, x.Type))?.Value;
            Assert.Equal(creds.SecretKey, secretKey);

            var token = authResults.Principal?.Claims.FirstOrDefault(x => string.Equals(AwsCredentialsAuthenticationHandler.ClaimAwsSessionToken, x.Type))?.Value;
            Assert.Equal(creds.Token, token);
        }

        [Fact]
        public void AuthMissingIssueDate()
        {
            var authValue = MockAuthorizationHeaderValue("access", "secret", null!, null);
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(authValue, new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal($"Authorization header missing {AwsCredentialsAuthenticationHandler.ClaimAwsIssueDate} property", authResults.Failure?.Message);
        }

        [Fact]
        public void AuthExpiredIssueDate()
        {
            var authValue = MockAuthorizationHeaderValue("access", "secret", null!, DateTime.UtcNow.AddMinutes(-5));
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(authValue, new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal("Issue date has expired", authResults.Failure?.Message);
        }

        [Fact]
        public void AuthFutureIssueDate()
        {
            var authValue = MockAuthorizationHeaderValue("access", "secret", null!, DateTime.UtcNow.AddMinutes(5));
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(authValue, new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal("Issue date invalid set in the future", authResults.Failure?.Message);
        }

        [Fact]
        public void AuthInvalidFormatForIssueDate()
        {
            var authValue = MockAuthorizationHeaderValue("access", "secret", null!, "not a date");
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(authValue, new NoEncryptionProvider());
            Assert.False(authResults.Succeeded);
            Assert.Equal("Failed to parse issue date", authResults.Failure?.Message);
        }

        [Fact]
        public async Task AuthMissingEncryptionInfoVersion()
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

            var serverCommandSettings = new ServerModeCommandSettings
            {
                Port = portNumber,
                ParentPid = null,
                UnsecureMode = false
            };
            var serverCommand = new ServerModeCommand(interactiveService);


            var cancelSource = new CancellationTokenSource();
            Exception? actualException = null;
            try
            {
                await serverCommand.ExecuteAsync(null!, serverCommandSettings, cancelSource);
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
            Assert.Equal("Missing required \"Version\" property in the symmetric key", actualException?.Message);
        }

        [Fact]
        public async Task AuthEncryptionWithInvalidVersion()
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

            var serverCommandSettings = new ServerModeCommandSettings
            {
                Port = portNumber,
                ParentPid = null,
                UnsecureMode = false
            };
            var serverCommand = new ServerModeCommand(interactiveService);
            var cancelSource = new CancellationTokenSource();
            Exception? actualException = null;
            try
            {
                await serverCommand.ExecuteAsync(null!, serverCommandSettings, cancelSource);
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
            Assert.Equal("Unsupported symmetric key not-valid", actualException?.Message);
        }

        [Fact]
        public void AuthMissingRequestId()
        {
            var authParameters = new Dictionary<string, string>
            {
                {"awsAccessKeyId", "accessKey" },
                {"awsSecretKey", "secretKey" },
                {"issueDate", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo) }
            };

            var results = AwsCredentialsAuthenticationHandler.ValidateAuthParameters(authParameters);
            Assert.False(results?.Succeeded);
            Assert.Equal($"Authorization header missing {AwsCredentialsAuthenticationHandler.ClaimAwsRequestId} property", results?.Failure?.Message);
        }

        [Fact]
        public void AuthAttemptReplayRequestId()
        {
            var authParameters = new Dictionary<string, string>
            {
                {"awsAccessKeyId", "accessKey" },
                {"awsSecretKey", "secretKey" },
                {"requestId", Guid.NewGuid().ToString() },
                {"issueDate", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo) }
            };

            var results = AwsCredentialsAuthenticationHandler.ValidateAuthParameters(authParameters);
            Assert.Null(results);

            results = AwsCredentialsAuthenticationHandler.ValidateAuthParameters(authParameters);
            Assert.False(results?.Succeeded);
            Assert.Equal($"Value for authorization header has already been used", results?.Failure?.Message);
        }

        [Fact]
        public void AuthExpiredRequestIdAreClearedFromCache()
        {
            Dictionary<string, string> GenerateAuthParameters()
            {
                return new Dictionary<string, string>
                {
                    {"awsAccessKeyId", "accessKey" },
                    {"awsSecretKey", "secretKey" },
                    {"requestId", Guid.NewGuid().ToString() },
                    {"issueDate", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo) }
                };
            }

            var request1 = GenerateAuthParameters();
            AwsCredentialsAuthenticationHandler.ValidateAuthParameters(request1);
            Assert.Contains(request1["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);

            var request2 = GenerateAuthParameters();
            AwsCredentialsAuthenticationHandler.ValidateAuthParameters(request2);
            Assert.Contains(request1["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);
            Assert.Contains(request2["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);

            Thread.Sleep(AwsCredentialsAuthenticationHandler.MaxIssueDateDuration.Add(TimeSpan.FromSeconds(3)));

            var request3 = GenerateAuthParameters();
            AwsCredentialsAuthenticationHandler.ValidateAuthParameters(request3);
            Assert.DoesNotContain(request1["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);
            Assert.DoesNotContain(request2["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);
            Assert.Contains(request3["requestId"], AwsCredentialsAuthenticationHandler.ProcessRequestIds);
        }

        private string MockAuthorizationHeaderValue(string accessKey, string secretKey, string sessionToken, object? issueDate)
        {
            var authParameters = new Dictionary<string, string>
            {
                {AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId, accessKey },
                {AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey, secretKey },
                {AwsCredentialsAuthenticationHandler.ClaimAwsRequestId, Guid.NewGuid().ToString() }
            };

            if (!string.IsNullOrEmpty(sessionToken))
            {
                authParameters[AwsCredentialsAuthenticationHandler.ClaimAwsSessionToken] = sessionToken;
            }

            if (issueDate != null)
            {
                if(issueDate is DateTime)
                {
                    authParameters[AwsCredentialsAuthenticationHandler.ClaimAwsIssueDate] = ((DateTime)issueDate).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
                }
                else
                {
                    authParameters[AwsCredentialsAuthenticationHandler.ClaimAwsIssueDate] = issueDate.ToString() ?? string.Empty;
                }
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(authParameters);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            return AwsCredentialsAuthenticationHandler.SchemaName + " " + base64;
        }
    }
}
