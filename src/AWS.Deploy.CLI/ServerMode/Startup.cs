// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using AWS.Deploy.CLI.ServerMode.Services;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.ServerMode.Hubs;
using Microsoft.AspNetCore.HostFiltering;

namespace AWS.Deploy.CLI.ServerMode
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<HostFilteringOptions>(
                options => options.AllowedHosts = new List<string> { "127.0.0.1", "localhost" });

            services.AddCustomServices();

            services.AddSingleton<IDeploymentSessionStateServer>(new InMemoryDeploymentSessionStateServer());

            services.AddAuthentication(options =>
            {
                options.DefaultScheme
                    = AwsCredentialsAuthenticationHandler.SchemaName;
            })
            .AddScheme<AwsCredentialsAuthenticationSchemeOptions, AwsCredentialsAuthenticationHandler>
                    (AwsCredentialsAuthenticationHandler.SchemaName, _ => { });


            services.AddSignalR();
            services.AddControllers()
                    .AddJsonOptions(opts =>
                    {
                        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "AWS .NET Deploy Tool Server Mode API",
                    Description = "The API exposed by the AWS .NET Deploy tool when started in server mode. This is intended for IDEs like Visual Studio to integrate the deploy tool into the IDE for deployment features.",
                    License = new OpenApiLicense
                    {
                        Name = "Apache 2",
                        Url = new Uri("https://aws.amazon.com/apache-2-0/"),
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AWS .NET Deploy Tool Server Mode API");
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<DeploymentCommunicationHub>("/DeploymentCommunicationHub");
            });
        }
    }
}
