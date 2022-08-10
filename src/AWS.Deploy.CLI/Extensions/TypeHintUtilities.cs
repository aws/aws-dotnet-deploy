// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using Amazon.EC2.Model;

namespace AWS.Deploy.CLI.Extensions
{
    public static class TypeHintUtilities
    {
        public static string GetDisplayableVpc(this Vpc vpc)
        {
            var name = vpc.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? string.Empty;
            var namePart =
                string.IsNullOrEmpty(name)
                    ? ""
                    : $" ({name}) ";

            var isDefaultPart =
                vpc.IsDefault
                    ? " *** Account Default VPC ***"
                    : "";

            return $"{vpc.VpcId}{namePart}{isDefaultPart}";
        }
    }
}
