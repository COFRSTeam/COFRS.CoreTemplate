using COFRS;
$if$ ($databaseTechnology$ == SQLServer)using COFRS.SqlServer;
$else$$if$ ($databaseTechnology$ == Postgresql)using COFRS.Postgresql;
$else$$if$ ($databaseTechnology$ == MySQL)using COFRS.MySql;
$endif$
namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository
	///	</summary>
    $if$ ($databaseTechnology$ == SQLServer)public interface IServiceRepository : ISqlServerRepository
	$else$$if$ ($databaseTechnology$ == Postgresql)public interface IServiceRepository : IProstregsqlRepository
	$else$$if$ ($databaseTechnology$ == MySQL)public interface IServiceRepository : IMySqlRepository
	$endif${
	}
}
