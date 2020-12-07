// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Deploy.Common
{
    public class RecommendationEngineException : Exception
    {
        public RecommendationEngineException(string message) : base(message)
        {
        }
    }
}
