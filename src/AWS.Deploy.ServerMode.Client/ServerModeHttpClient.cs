// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;

namespace AWS.Deploy.ServerMode.Client
{
    /// <summary>
    /// This derived HttpClient is created with a handler to make sure the AWS credentials are used to create the authorization header for calls to the deploy tool server mode.
    /// Instances of this class are created with the ServerModeHttpClientFactory factory.
    /// </summary>
    public class ServerModeHttpClient : HttpClient
    {
        internal ServerModeHttpClient(ServerModeHttpClientAuthorizationHandler handler)
            : base(handler)
        {

        }
    }

    /// <summary>
    /// HttpClient handler that gets the latest credentials from the client sets the authorization header.
    /// </summary>
    public class ServerModeHttpClientAuthorizationHandler : HttpClientHandler
    {
        private readonly Func<Task<AWSCredentials>> _credentialsGenerator;
        private readonly Aes? _aes;

        internal ServerModeHttpClientAuthorizationHandler(Func<Task<AWSCredentials>> credentialsGenerator, Aes? aes = null)
        {
            _credentialsGenerator = credentialsGenerator;
            _aes = aes;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var awsCreds = await _credentialsGenerator();
            if(awsCreds != null)
            {
                var immutableCredentials = await awsCreds.GetCredentialsAsync();
                AddAuthorizationHeader(request, immutableCredentials, _aes);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        public static void AddAuthorizationHeader(HttpRequestMessage request, ImmutableCredentials credentials, Aes? aes = null)
        {
            var authParameters = new Dictionary<string, string>
            {
                {"awsAccessKeyId", credentials.AccessKey },
                {"awsSecretKey", credentials.SecretKey },
                {"requestId", Guid.NewGuid().ToString() },
                {"issueDate", DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ", DateTimeFormatInfo.InvariantInfo) }
            };

            if(!string.IsNullOrEmpty(credentials.Token))
            {
                authParameters["awsSessionToken"] = credentials.Token;
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(authParameters);
            string base64;
            if(aes != null)
            {
                aes.GenerateIV();
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                using var outputStream = new MemoryStream();
                using (var encryptStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(encryptStream);
                }

                base64 = $"{Convert.ToBase64String(aes.IV)} {Convert.ToBase64String(outputStream.ToArray())}";
            }
            else
            {
                base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("aws-deploy-tool-server-mode", base64);
        }
    }

    /// <summary>
    /// Factory for creating the ServerModeHttpClient.
    /// </summary>
    public static class ServerModeHttpClientFactory
    {
        public static ServerModeHttpClient ConstructHttpClient(Func<Task<AWSCredentials>> credentialsGenerator, Aes? aes = null)
        {
            return new ServerModeHttpClient(new ServerModeHttpClientAuthorizationHandler(credentialsGenerator, aes));
        }
    }
}
