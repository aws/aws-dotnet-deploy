// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using AWS.Deploy.CLI.ServerMode.Services;

namespace AWS.Deploy.CLI.ServerMode
{
    public class AwsCredentialsAuthenticationSchemeOptions
        : AuthenticationSchemeOptions
    { }

    /// <summary>
    /// The ASP.NET Core Authentication handler. Verify the Authorization header has been correctly set with AWS Credentials.
    /// </summary>
    public class AwsCredentialsAuthenticationHandler : AuthenticationHandler<AwsCredentialsAuthenticationSchemeOptions>
    {
        public const string SchemaName = "aws-deploy-tool-server-mode";

        public const string ClaimAwsAccessKeyId = "awsAccessKeyId";
        public const string ClaimAwsSecretKey = "awsSecretKey";
        public const string ClaimAwsSessionToken = "awsSessionToken";

        private readonly IEncryptionProvider _encryptionProvider;

        public AwsCredentialsAuthenticationHandler(
            IOptionsMonitor<AwsCredentialsAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IEncryptionProvider encryptionProvider)
            : base(options, logger, encoder, clock)
        {
            _encryptionProvider = encryptionProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var value))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
            }

            return Task.FromResult(ProcessAuthorizationHeader(value, _encryptionProvider));
        }

        public static AuthenticateResult ProcessAuthorizationHeader(string authorizationHeaderValue, IEncryptionProvider encryptionProvider)
        {
            var tokens = authorizationHeaderValue.Split(' ');
            if (tokens.Length != 2)
            {
                return AuthenticateResult.Fail($"Incorrect format Authorization header. Format should be \"{SchemaName} <base-64-auth-parameters>\"");
            }
            if (!string.Equals(SchemaName, tokens[0]))
            {
                return AuthenticateResult.Fail($"Unsupported authorization schema. Supported schema: {SchemaName}");
            }

            try
            {
                var base64Bytes = Convert.FromBase64String(tokens[1]);

                var decryptedBytes = encryptionProvider.Decrypt(base64Bytes);
                var json = Encoding.UTF8.GetString(decryptedBytes);

                var authParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                var claimIdentity = new ClaimsIdentity(nameof(AwsCredentialsAuthenticationHandler));
                foreach (var kvp in authParameters)
                {
                    claimIdentity.AddClaim(new Claim(kvp.Key, kvp.Value));
                }

                var ticket = new AuthenticationTicket(
                        new ClaimsPrincipal(claimIdentity), SchemaName);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception)
            {
                return AuthenticateResult.Fail("Error decoding authorization value");
            }
        }
    }

}
