using COFRS;
$if$ ($databaseTechnology$ == SQLServer)using COFRS.SqlServer;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository
	///	</summary>
    public interface IServiceRepository : ISqlServerRepository
	{

	}
}
$endif$$if$ ($databaseTechnology$ == Postgresql)using COFRS.Postgresql;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository
	///	</summary>
    public interface IServiceRepository : IPostgresqlRepository
	{

	}
}
$endif$$if$ ($databaseTechnology$ == MySQL)using COFRS.MySql;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository
	///	</summary>
	public interface IServiceRepository : IMySqlRepository
	{

	}
}
$endif$
