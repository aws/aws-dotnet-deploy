// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.DockerEngine
{
    public class ImageDefinition
    {
        public string SdkType { get; set; }
        public List<ImageMapping> ImageMapping { get; set; }

        public override string ToString()
        {
            return $"Image Definition for {SdkType}";
        }
    }

    public class ImageMapping
    {
        public string TargetFramework { get; set; }
        public string BaseImage { get; set; }
        public string BuildImage { get; set; }

        public override string ToString()
        {
            return $"Image Mapping for {TargetFramework}";
        }
    }
}
