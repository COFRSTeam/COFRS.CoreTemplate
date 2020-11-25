using System.Net;
using COFRS;
$if$ ( $securitymodel$ == OAuth )using Microsoft.AspNetCore.Authorization;
$endif$using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;
using $safeprojectname$.Models.ResourceModels; 
using $safeprojectname$.Models.SwaggerExamples;

namespace $safeprojectname$.Controllers
{
	///	<summary>
	///	Heartbeat controller
	///	</summary>
	[ApiVersion("1.0")]
	[Produces("application/json")]
	public class HeartbeatController : COFRSController
	{
		private readonly ILogger<HeartbeatController> Logger;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger"></param>
		public HeartbeatController(ILogger<HeartbeatController> logger)
		{
			Logger = logger;
		}

		///	<summary>
		///	Returns a heartbeat message
		///	</summary>
		///	<remarks>This method is used to supply "I am alive" messages to monitoring systems.</remarks>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[Route("heartbeat")]
		$if$ ( $securitymodel$ == OAuth )[AllowAnonymous]
		$endif$[ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Heartbeat))]
		[SwaggerResponseExample((int)HttpStatusCode.OK, typeof(HeartbeatExample))]
		[ProducesResponseType((int)HttpStatusCode.UnsupportedMediaType)]
		[Produces("application/vnd.$companymoniker$.v1+json", "application/json", "text/json")]
		public IActionResult Get()
		{
			Logger.LogInformation("Heartbeat invoked");
			return Ok(new Heartbeat() { Message = "$safeprojectname$ is running" });
		}
	}
}
