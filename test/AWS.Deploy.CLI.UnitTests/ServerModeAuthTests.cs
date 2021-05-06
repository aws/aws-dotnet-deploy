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

using Xunit;
using Amazon.Runtime;
using AWS.Deploy.ServerMode.Client;

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

            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader(value.FirstOrDefault());
            Assert.True(authResults.Succeeded);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailNoSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("no-schema-value");
            Assert.False(authResults.Succeeded);
            Assert.Contains("Incorrect format Authorization header", authResults.Failure.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailWrongSchema()
        {
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader("wrong-schema value");
            Assert.False(authResults.Succeeded);
            Assert.Contains("Unsupported authorization schema", authResults.Failure.Message);
        }

        [Fact]
        public void ProcessAuthorizationHeaderFailInValidJson()
        {
            var base64BadJson = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("you are not json"));
            var authResults = AwsCredentialsAuthenticationHandler.ProcessAuthorizationHeader($"aws-deploy-tool-server-mode {base64BadJson}");
            Assert.False(authResults.Succeeded);
            Assert.Equal("Error decoding authorization value", authResults.Failure.Message);
        }
    }
}
