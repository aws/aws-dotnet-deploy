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
        readonly Func<Task<AWSCredentials>> _credentialsGenerator;

        internal ServerModeHttpClientAuthorizationHandler(Func<Task<AWSCredentials>> credentialsGenerator)
        {
            _credentialsGenerator = credentialsGenerator;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var awsCreds = await _credentialsGenerator();
            if(awsCreds != null)
            {
                var immutableCredentials = await awsCreds.GetCredentialsAsync();
                AddAuthorizationHeader(request, immutableCredentials);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        public static void AddAuthorizationHeader(HttpRequestMessage request, ImmutableCredentials credentials)
        {
            var authParameters = new Dictionary<string, string>
            {
                {"awsAccessKeyId", credentials.AccessKey },
                {"awsSecretKey", credentials.SecretKey }
            };

            if(!string.IsNullOrEmpty(credentials.Token))
            {
                authParameters["awsSessionToken"] = credentials.Token;
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(authParameters);
            var base64 = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(json));
            request.Headers.Authorization = new AuthenticationHeaderValue("aws-deploy-tool-server-mode", base64);
        }
    }

    /// <summary>
    /// Factory for creating the ServerModeHttpClient.
    /// </summary>
    public static class ServerModeHttpClientFactory
    {
        public static ServerModeHttpClient ConstructHttpClient(Func<Task<AWSCredentials>> credentialsGenerator)
        {
            return new ServerModeHttpClient(new ServerModeHttpClientAuthorizationHandler(credentialsGenerator));
        }
    }
}
