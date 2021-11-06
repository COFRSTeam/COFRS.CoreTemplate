using COFRS;

namespace $safeprojectname$.Orchestration
{
	///	<summary>
	///	The IServiceOrchestrator interface, derrived from <see cref="IOrchestrator"/>. The default orchestrator 
    ///	provides means for the developer to read, add, update and delete items from the datastore. 
	///	</summary>
    ///	<remarks>The developer is free to add new functions to the orchestrator, in order to accomplish orchestrations
    ///	combining the base functions, or to invent entirely new functions, as needs arise.</remarks>
	public interface IServiceOrchestrator : IOrchestrator
	{
	}
}
