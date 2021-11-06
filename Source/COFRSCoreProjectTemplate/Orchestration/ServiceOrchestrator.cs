using COFRS;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using $safeprojectname$.Repository;

namespace $safeprojectname$.Orchestration
{
	///	<summary>
	///	The ServiceOrchestrator
	///	</summary>
	public class ServiceOrchestrator : BaseOrchestrator, IServiceOrchestrator
	{
		///	<summary>
		///	Initiates the Service Orchestrator
		///	</summary>
		///	<remarks>Add new, customized functions to the <see cref="IServiceOrchestrator"/> interface, and then add their
		///	implementations in this class, to extend the orchestrator with your custom functions.
		///	</remarks>
		public ServiceOrchestrator(IServiceRepository repository, IRepositoryOptions options) : base(repository, options) 
		{
		}
	}
}
