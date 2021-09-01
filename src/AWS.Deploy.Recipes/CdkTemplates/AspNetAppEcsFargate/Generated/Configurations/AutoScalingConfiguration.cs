// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetAppEcsFargate.Configurations
{
    public class AutoScalingConfiguration
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
