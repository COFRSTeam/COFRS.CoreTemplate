using $safeprojectname$.Models.ResourceModels;
using Swashbuckle.AspNetCore.Filters;

namespace $safeprojectname$.Models.SwaggerExamples
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
				Message = "$safeprojectname$ is running"
			};
		}
	}
}
