// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using AWS.Deploy.CLI.ServerMode.Models;
using Microsoft.AspNetCore.Mvc;

namespace AWS.Deploy.CLI.ServerMode.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Gets the health of the deployment tool. Use this API after starting the command line to see if the tool is ready to handle requests.
        /// </summary>
        [HttpGet]
        public HealthStatusOutput Get()
        {
            return new HealthStatusOutput
            {
                Status = SystemStatus.Ready
            };
        }
    }
}
