// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Amazon.Runtime;

namespace AWS.Deploy.CLI.ServerMode
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Create an AWSCredentials object from the key information set as claims on the current request's ClaimsPrincipal.
        /// </summary>
        /// <param name="user"></param>
        public static AWSCredentials? ToAWSCredentials(this ClaimsPrincipal user)
        {
            var awsAccessKeyId = user.Claims.FirstOrDefault(x => string.Equals(x.Type, AwsCredentialsAuthenticationHandler.ClaimAwsAccessKeyId))?.Value;
            var awsSecretKey = user.Claims.FirstOrDefault(x => string.Equals(x.Type, AwsCredentialsAuthenticationHandler.ClaimAwsSecretKey))?.Value;
            var awsSessionToken = user.Claims.FirstOrDefault(x => string.Equals(x.Type, AwsCredentialsAuthenticationHandler.ClaimAwsSessionToken))?.Value;

            if(string.IsNullOrEmpty(awsAccessKeyId) || string.IsNullOrEmpty(awsSecretKey))
            {
                return null;
            }

            if(!string.IsNullOrEmpty(awsSessionToken))
            {
                return new SessionAWSCredentials(awsAccessKeyId, awsSecretKey, awsSessionToken);
            }

            return new BasicAWSCredentials(awsAccessKeyId, awsSecretKey);
        }
    }
}
