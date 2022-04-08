// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.Annotations;

namespace AWS.Deploy.CLI.ServerMode.Controllers
{
    /// <summary>
    /// Contains operations to manage the lifecycle of the server
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ServerController : ControllerBase
    {
        private readonly IHostApplicationLifetime _applicationLifetime;

        public ServerController(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        /// <summary>
        /// Requests to stop the deployment tool. Any open sessions are implicitly closed.
        /// This may return <see cref="OkResult"/> prior to the server being stopped,
        /// clients may need to wait or check the health after requesting shutdown.
        /// </summary>
        [HttpPost("Shutdown")]
        [SwaggerOperation(OperationId = "Shutdown")]
        [Authorize]
        public IActionResult Shutdown()
        {
            _applicationLifetime.StopApplication();
            return Ok();
        }
    }
}
