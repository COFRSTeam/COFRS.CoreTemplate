﻿using COFRS;
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
		///	<param name="options">The runtime <see cref="IRepositoryOptions"/> for this repository</param>
		///	<param name="translationOptions">The runtime <see cref="ITranslationOptions"/> options for the service</param>
		public ServiceRepository(ILogger<SqlServerRepository> logger, IRepositoryOptions options, ITranslationOptions translationOptions) : base(logger, options, translationOptions)
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
		///	<param name="options">The runtime <see cref="IRepositoryOptions"/> for this repository</param>
		///	<param name="translationOptions">The runtime <see cref="ITranslationOptions"/> options for the service</param>
		public ServiceRepository(ILogger<PostgresqlRepository> logger, IRepositoryOptions options, ITranslationOptions translationOptions) : base(logger, options, translationOptions)
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
		///	<param name="options">The runtime <see cref="IRepositoryOptions"/> for this repository</param>
		///	<param name="translationOptions">The runtime <see cref="ITranslationOptions"/> options for the service</param>
		public ServiceRepository(ILogger<MySqlRepository> logger, IRepositoryOptions options, ITranslationOptions translationOptions) : base(logger, options, translationOptions)
		{

		}
	}
}
$endif$

