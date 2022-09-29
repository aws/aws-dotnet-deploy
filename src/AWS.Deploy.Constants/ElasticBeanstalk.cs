using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.Deploy.Constants
{
    internal static class ElasticBeanstalk
    {
        public const string EnhancedHealthReportingOptionId = "EnhancedHealthReporting";
        public const string EnhancedHealthReportingOptionNameSpace = "aws:elasticbeanstalk:healthreporting:system";
        public const string EnhancedHealthReportingOptionName = "SystemType";

        public const string XRayTracingOptionId = "XRayTracingSupportEnabled";
        public const string XRayTracingOptionNameSpace = "aws:elasticbeanstalk:xray";
        public const string XRayTracingOptionName = "XRayEnabled";

        public const string ProxyOptionId = "ReverseProxy";
        public const string ProxyOptionNameSpace = "aws:elasticbeanstalk:environment:proxy";
        public const string ProxyOptionName = "ProxyServer";

        public const string HealthCheckURLOptionId = "HealthCheckURL";
        public const string HealthCheckURLOptionNameSpace = "aws:elasticbeanstalk:application";
        public const string HealthCheckURLOptionName = "Application Healthcheck URL";

        public const string LinuxPlatformType = ".NET Core";
        public const string WindowsPlatformType = "Windows Server";

        public const string IISAppPathOptionId = "IISAppPath";
        public const string IISWebSiteOptionId = "IISWebSite";

        public const string WindowsManifestName = "aws-windows-deployment-manifest.json";

        /// <summary>
        /// This list stores a named tuple of OptionSettingId, OptionSettingNameSpace and OptionSettingName.
        /// <para>OptionSettingId refers to the Id property for an option setting item in the recipe file.</para>
        /// <para>OptionSettingNameSpace and OptionSettingName provide a way to configure the environments metadata and update its behaviour.</para>
        /// <para>A comprehensive list of all configurable settings can be found <see href="https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/beanstalk-environment-configuration-advanced.html">here</see></para>
        /// </summary>
        public static List<(string OptionSettingId, string OptionSettingNameSpace, string OptionSettingName)> OptionSettingQueryList = new()
        {
            new (EnhancedHealthReportingOptionId, EnhancedHealthReportingOptionNameSpace, EnhancedHealthReportingOptionName),
            new (XRayTracingOptionId, XRayTracingOptionNameSpace, XRayTracingOptionName),
            new (ProxyOptionId, ProxyOptionNameSpace, ProxyOptionName),
            new (HealthCheckURLOptionId, HealthCheckURLOptionNameSpace, HealthCheckURLOptionName)
        };

        /// <summary>
        /// This is the list of option settings available for Windows Beanstalk deployments.
        /// This list stores a named tuple of OptionSettingId, OptionSettingNameSpace and OptionSettingName.
        /// <para>OptionSettingId refers to the Id property for an option setting item in the recipe file.</para>
        /// <para>OptionSettingNameSpace and OptionSettingName provide a way to configure the environments metadata and update its behaviour.</para>
        /// <para>A comprehensive list of all configurable settings can be found <see href="https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/beanstalk-environment-configuration-advanced.html">here</see></para>
        /// </summary>
        public static List<(string OptionSettingId, string OptionSettingNameSpace, string OptionSettingName)> WindowsOptionSettingQueryList = new()
        {
            new (EnhancedHealthReportingOptionId, EnhancedHealthReportingOptionNameSpace, EnhancedHealthReportingOptionName),
            new (XRayTracingOptionId, XRayTracingOptionNameSpace, XRayTracingOptionName),
            new (HealthCheckURLOptionId, HealthCheckURLOptionNameSpace, HealthCheckURLOptionName)
        };
    }
}
