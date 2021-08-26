// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public const string ClaimAwsIssueDate = "issueDate";
        public const string ClaimAwsRequestId = "requestId";

        /// <summary>
        /// The max duration auth request values are valid based on the issue date.
        /// </summary>
        public static readonly TimeSpan MaxIssueDateDuration = TimeSpan.FromSeconds(10);

        // Cache of auth request ids that have already been used. Request ids are not allowed to be reused.
        // The timestamp of when they were added is stored. Values can be cleared out cache after the MaxIssueDateDuration.
        private static readonly IDictionary<string, DateTime> _processedRequestIds = new Dictionary<string, DateTime>();

        /// <summary>
        /// Readonly view of the already processed auth request ids. The main use case of this property is for testing.
        /// </summary>
        public static IReadOnlyDictionary<string, DateTime> ProcessRequestIds
        {
            get
            {
                return new ReadOnlyDictionary<string, DateTime>(_processedRequestIds);
            }
        }

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
            if (tokens.Length != 2 && tokens.Length != 3)
            {
                var ivPlaceholder = "";
                if (encryptionProvider is AesEncryptionProvider)
                {
                    ivPlaceholder = "<iv> ";
                }
                return AuthenticateResult.Fail($"Incorrect format Authorization header. Format should be \"{SchemaName} {ivPlaceholder}<base-64-auth-parameters>\"");
            }
            if (tokens.Length == 2 && encryptionProvider is AesEncryptionProvider)
            {
                return AuthenticateResult.Fail($"Incorrect format Authorization header. Format should be \"{SchemaName} <iv> <base-64-auth-parameters>\"");
            }
            if (!string.Equals(SchemaName, tokens[0]))
            {
                return AuthenticateResult.Fail($"Unsupported authorization schema. Supported schema: {SchemaName}");
            }

            try
            {
                byte[]? base64IV;
                byte[] base64Bytes;
                if (tokens.Length == 2)
                {
                    base64IV = null;
                    base64Bytes = Convert.FromBase64String(tokens[1]);
                }
                else
                {
                    base64IV = Convert.FromBase64String(tokens[1]);
                    base64Bytes = Convert.FromBase64String(tokens[2]);
                }

                var decryptedBytes = encryptionProvider.Decrypt(base64Bytes, base64IV);
                var json = Encoding.UTF8.GetString(decryptedBytes);

                var authParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                // Validate the issue date and request id are valid.
                var validateResult = ValidateAuthParameters(authParameters);
                if(validateResult != null)
                {
                    return validateResult;
                }

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

        public static AuthenticateResult? ValidateAuthParameters(IDictionary<string, string> authParameters)
        {
            lock (_processedRequestIds)
            {
                if (!authParameters.TryGetValue(ClaimAwsIssueDate, out var issueDateStr))
                {
                    return AuthenticateResult.Fail($"Authorization header missing {ClaimAwsIssueDate} property");
                }

                if (!DateTime.TryParse(issueDateStr, out var issueDate))
                {
                    return AuthenticateResult.Fail("Failed to parse issue date");
                }

                issueDate = issueDate.ToUniversalTime();

                // The encrypted authorization header, which includes a date stamp, is only valid for a small amount of time.
                // The request can potentially go longer then a minute but the issue date check
                // is verified at the start of the request. This is to reduce the window the authorization
                // header could be replayed.
                if (issueDate < DateTime.UtcNow.Subtract(MaxIssueDateDuration))
                {
                    return AuthenticateResult.Fail("Issue date has expired");
                }

                // Check to see if the issue date was incorrectly set in the future. A one second buffer is used in
                // case the caller was using a less precise clock.
                if(DateTime.UtcNow.AddSeconds(1) < issueDate)
                {
                    return AuthenticateResult.Fail("Issue date invalid set in the future");
                }

                if (!authParameters.TryGetValue(ClaimAwsRequestId, out var requestId))
                {
                    return AuthenticateResult.Fail($"Authorization header missing {ClaimAwsRequestId} property");
                }

                // If the authorization header value is attempted to be reused then fail auth check.
                if (_processedRequestIds.ContainsKey(requestId))
                {
                    return AuthenticateResult.Fail($"Value for authorization header has already been used");
                }

                // Store the request id so it can not be reused.
                _processedRequestIds.Add(requestId, DateTime.UtcNow);

                // Remove request ids that are older then MaxIssueDateDuration
                var expirationDate = DateTime.UtcNow.Subtract(MaxIssueDateDuration.Add(TimeSpan.FromSeconds(2)));
                var expiredRequests = _processedRequestIds.Where(x => x.Value < expirationDate);
                foreach (var expiredRequest in expiredRequests)
                {
                    _processedRequestIds.Remove(expiredRequest.Key);
                }
            }

            return null;
        }
    }
}
