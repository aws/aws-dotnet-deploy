// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Deploy.Orchestration.Docker
{
    public class ImageDefinition
    {
        public string SdkType { get; set; }
        public List<ImageMapping> ImageMapping { get; set; }

        public ImageDefinition(string sdkType, List<ImageMapping> imageMapping)
        {
            SdkType = sdkType;
            ImageMapping = imageMapping;
        }

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

        public ImageMapping(string targetFramework, string baseImage, string buildImage)
        {
            TargetFramework = targetFramework;
            BaseImage = baseImage;
            BuildImage = buildImage;
        }

        public override string ToString()
        {
            return $"Image Mapping for {TargetFramework}";
        }
    }
}
