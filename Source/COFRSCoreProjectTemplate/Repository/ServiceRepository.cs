using COFRS;
$if$ ($databaseTechnology$ == SQLServer)using COFRS.SqlServer;
using Microsoft.Extensions.Logging;
using System;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The ServiceRepository
	///	</summary>
	public class ServiceRepository : SqlServerRepository, IServiceRepository
	{
		///	<summary>
		///	Instantiates the ServiceRepository
		///	</summary>
		///	<param name="logger">A generic interface for logging where the category name is derrived from the specified TCategoryName type name.</param>
		///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
		///	<param name="options">The runtime options for this repository</param>
		public ServiceRepository(ILogger<SqlServerRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
		{
		}
	}
}
$endif$$if$ ($databaseTechnology$ == Postgresql)using COFRS.Postgresql;
using Microsoft.Extensions.Logging;
using System;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The ServiceRepository
	///	</summary>
	public class ServiceRepository : PostgresqlRepository, IServiceRepository
{
	///	<summary>
	///	Instantiates the ServiceRepository
	///	</summary>
	///	<param name="logger">A generic interface for logging where the category name is derrived from the specified TCategoryName type name.</param>
	///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
	///	<param name="options">The runtime options for this repository</param>
	public ServiceRepository(ILogger<PostgresqlRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
	{
	}
}
}
$endif$$if$ ($databaseTechnology$ == MySQL)using COFRS.MySql;
using Microsoft.Extensions.Logging;
using System;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The ServiceRepository
	///	</summary>
	public class ServiceRepository : MySqlRepository, IServiceRepository
{
	///	<summary>
	///	Instantiates the ServiceRepository
	///	</summary>
	///	<param name="logger">A generic interface for logging where the category name is derrived from the specified TCategoryName type name.</param>
	///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
	///	<param name="options">The runtime options for this repository</param>
	public ServiceRepository(ILogger<MySqlRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
	{
	}
}
}
$endif$

