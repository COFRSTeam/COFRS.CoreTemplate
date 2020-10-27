using COFRS;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;

namespace $safeprojectname$.Orchestration
{
	///	<summary>
	///	The ServiceOrchestrator
	///	</summary>
	///	<typeparam name="T">The type of repository used by the orchestration layer</typeparam>
	public class ServiceOrchestrator<T> : BaseOrchestrator<T>, IServiceOrchestrator
	{
		///	<summary>
		///	Initiates the ServiceOrchestrator
		///	</summary>
		///	<param name="logger">A generic interface for logging where the category name is derrived from the specified TCategoryName type name.</param>
		///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
		public ServiceOrchestrator(ILogger<ServiceOrchestrator<T>> logger, IServiceProvider provider) : base(logger, provider)
		{
		}

		///	<summary>
		///	Initiates the ServiceOrchestrator
		///	</summary>
		///	<param name="user">A System.Security.Principal.IPrincipal implementation that supports multiple claims based identities</param>
		///	<param name="logger">A generic interface for logging where the category name is derrived from the specified TCategoryName type name.</param>
		///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
		public ServiceOrchestrator(ILogger<ServiceOrchestrator<T>> logger, IServiceProvider provider, ClaimsPrincipal user) : base(logger, provider, user)
		{
		}
	}
}
