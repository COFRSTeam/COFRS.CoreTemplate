﻿using $safeprojectname$.Orchestration.ResourceModels;
using Swashbuckle.AspNetCore.Filters;

namespace $safeprojectname$.SwaggerExamples
{
	/// <summary>
	/// Example of Heartbeat object
	/// </summary>
	public class HeartbeatExample : IExamplesProvider<Heartbeat>
	{
		/// <summary>
		/// Gets an example of the heartbeat object
		/// </summary>
		/// <returns></returns>
		public Heartbeat GetExamples()
		{
			return new Heartbeat()
			{
				Message = "InventoryService is running"
			};
		}
	}
}
