using System.Net;
using COFRS;
$if$ ( $securitymodel$ == OAuth )using Microsoft.AspNetCore.Authorization;
$endif$using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using $safeprojectname$.Models.ResourceModels; 

namespace $safeprojectname$.Controllers
{
	///	<summary>
	///	Heartbeat controller
	///	</summary>
	[ApiVersion("1.0")]
	[Produces("application/json")]
	public class HealthCheckController : COFRSController
	{
		private readonly ILogger<HealthCheckController> Logger;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger"></param>
		public HealthCheckController(ILogger<HealthCheckController> logger)
		{
			Logger = logger;
		}

		///	<summary>
		///	Returns a heartbeat message
		///	</summary>
		///	<remarks>This method is used to supply "I am alive" messages to monitoring systems.</remarks>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[Route("health_check")]
		$if$ ( $securitymodel$ == OAuth )[AllowAnonymous]
		$endif$[ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(HealthCheck))]
		[ProducesResponseType((int)HttpStatusCode.UnsupportedMediaType)]
		[Produces("application/vnd.$companymoniker$.v1+json", "application/json", "text/json")]
		public IActionResult Get()
		{
			Logger.LogInformation("HealthCheck invoked");
			return Ok(new HealthCheck() { Message = "$safeprojectname$ is running" });
		}
	}
}
