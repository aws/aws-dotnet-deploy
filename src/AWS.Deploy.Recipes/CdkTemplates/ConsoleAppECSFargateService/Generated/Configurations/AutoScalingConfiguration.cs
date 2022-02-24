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

namespace ConsoleAppEcsFargateService.Configurations
{
    public partial class AutoScalingConfiguration
    {
        const int defaultCooldown = 300;

        public bool Enabled { get; set; }

        public int MinCapacity { get; set; } = 1;

        public int MaxCapacity { get; set; } = 3;

        public enum ScalingTypeEnum { Cpu, Memory }

        public ScalingTypeEnum? ScalingType { get; set; }



        public double CpuTypeTargetUtilizationPercent { get; set; } = 70;

        public int CpuTypeScaleInCooldownSeconds { get; set; } = defaultCooldown;

        public int CpuTypeScaleOutCooldownSeconds { get; set; } = defaultCooldown;



        public int MemoryTypeTargetUtilizationPercent { get; set; } = 70;

        public int MemoryTypeScaleInCooldownSeconds { get; set; } = defaultCooldown;

        public int MemoryTypeScaleOutCooldownSeconds { get; set; } = defaultCooldown;

        /// A parameterless constructor is needed for <see cref="Microsoft.Extensions.Configuration.ConfigurationBuilder"/>
        /// or the classes will fail to initialize.
        /// The warnings are disabled since a parameterless constructor will allow non-nullable properties to be initialized with null values.
#nullable disable warnings
        public AutoScalingConfiguration()
        {

        }
#nullable restore warnings

        public AutoScalingConfiguration(
            int minCapacity,
            int maxCapacity,
            int cpuTypeTargetUtilizationPercent,
            int cpuTypeScaleInCooldownSeconds,
            int cpuTypeScaleOutCooldownSeconds,
            int memoryTypeTargetUtilizationPercent,
            int memoryTypeScaleInCooldownSeconds,
            int memoryTypeScaleOutCooldownSeconds
            )
        {
            MinCapacity = minCapacity;
            MaxCapacity = maxCapacity;
            CpuTypeTargetUtilizationPercent = cpuTypeTargetUtilizationPercent;
            CpuTypeScaleInCooldownSeconds = cpuTypeScaleInCooldownSeconds;
            CpuTypeScaleOutCooldownSeconds = cpuTypeScaleOutCooldownSeconds;
            MemoryTypeTargetUtilizationPercent = memoryTypeTargetUtilizationPercent;
            MemoryTypeScaleInCooldownSeconds = memoryTypeScaleInCooldownSeconds;
            MemoryTypeScaleOutCooldownSeconds = memoryTypeScaleOutCooldownSeconds;
        }
    }
}
