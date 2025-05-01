// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAppWithDockerFile.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnvVarController : ControllerBase
    {
        ILogger<EnvVarController> _logger;

        public EnvVarController(ILogger<EnvVarController> logger)
        {
            _logger = logger;
        }

        [HttpGet()]
        public string Default(string name)
        {
            return "Hello";
        }

        [HttpGet("{name}")]
        public IActionResult Get(string name)
        {
            _logger.LogInformation("Fetching environment variable " + name);
            // Only expose environment variables starting with TEST_
            if (!name.StartsWith("TEST_"))
            {
                _logger.LogError("Fetch failed because environment variable name didn't start with TEST_");
                return BadRequest();
            }
            return Ok(Environment.GetEnvironmentVariable(name));
        }
    }
}
