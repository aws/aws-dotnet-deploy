// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common
{
    public class UserInputOption : IUserInputOption
    {
        public UserInputOption(string value)
        {
            Name = value;
        }

        public string Name { get; set; }

        public string? Description { get; set; }
    }
}
