// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

// This is a generated file from the original deployment recipe. It contains properties for
// all of the settings defined in the recipe file. It is recommended to not modify this file in order
// to allow easy updates to the file when the original recipe that this project was created from has updates.
// This class is marked as a partial class. If you add new settings to the recipe file, those settings should be
// added to partial versions of this class outside of the Generated folder for example in the Configuration folder.

namespace AspNetAppEcsFargate.Configurations
{
    public partial class AutoScalingConfiguration
    {
        const int defaultCooldown = 300;

        public bool Enabled { get; set; }

        public int MinCapacity { get; set; } = 3;

        public int MaxCapacity { get; set; } = 6;

        public enum ScalingTypeEnum { Cpu, Memory, Request }

        public ScalingTypeEnum? ScalingType { get; set; }



        public double CpuTypeTargetUtilizationPercent { get; set; } = 70;

        public int CpuTypeScaleInCooldownSeconds { get; set; } = defaultCooldown;

        public int CpuTypeScaleOutCooldownSeconds { get; set; } = defaultCooldown;



        public int RequestTypeRequestsPerTarget { get; set; } = 10000;

        public int RequestTypeScaleInCooldownSeconds { get; set; } = defaultCooldown;

        public int RequestTypeScaleOutCooldownSeconds { get; set; } = defaultCooldown;



        public int MemoryTypeTargetUtilizationPercent { get; set; } = 70;

        public int MemoryTypeScaleInCooldownSeconds { get; set; } = defaultCooldown;

        public int MemoryTypeScaleOutCooldownSeconds { get; set; } = defaultCooldown;
    }
}
