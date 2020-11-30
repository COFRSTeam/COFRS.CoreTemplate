using AutoMapper;
using COFRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using $safeprojectname$.Orchestration;
using $safeprojectname$.Repository;
$if$ ($framework$ == netcoreapp3.1)using Swashbuckle.AspNetCore.Filters;
$endif$

namespace $safeprojectname$.App_Start
{
	///	<summary>
	///	Services Helper Class
	///	</summary>
	public static class IServiceCollectionExtension
	{
		private static TranslationOptions TranslationOptions { get; set; }
		private static RepositoryOptions RepositoryOptions { get; set; }
		private static ApiOptions ApiOptions { get; set; }

		///	<summary>
		///	Configure Services
		///	</summary>
		///	<param name="services">The service collection</param>
		///	<param name="Configuration">The configuration service</param>
		public static IApiOptions ConfigureServices(this IServiceCollection services, IConfiguration Configuration)
		{
			var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(Configuration);

			services.AddSingleton(new LoggerFactory().AddSerilog(loggerConfig.CreateLogger()));
			services.AddLogging();

			services.AddSingleton<ICacheProvider>(new DefaultCacheProvider(Configuration.GetSection("ApiSettings").GetValue<int>("CacheLimit")));

			//	Configure API Settings
			ApiOptions = ApiOptions.Load(Configuration);
			services.InitializeFactories();
			services.AddSingleton<IApiOptions>(ApiOptions);

			//	Configure Translation options
			TranslationOptions = new TranslationOptions(Configuration.GetSection("ApiSettings").GetValue<string>("RootUrl"));
			services.AddSingleton<ITranslationOptions>(TranslationOptions);

			var myAssembly = Assembly.GetExecutingAssembly();
			AutoMapperFactory.MapperConfiguration = new MapperConfiguration(cfg => cfg.AddMaps(myAssembly));
			AutoMapperFactory.CreateMapper();

			$if$ ($framework$ == netcoreapp3.1)services.AddSwaggerExamplesFromAssemblyOf<Startup>();

			$endif$RepositoryOptions = new RepositoryOptions(Configuration.GetConnectionString("DefaultConnection"),
													  Configuration.GetSection("ApiSettings").GetValue<int>("QueryLimit"),
													  Configuration.GetSection("ApiSettings").GetValue<TimeSpan>("Timeout"));

			services.AddSingleton<IRepositoryOptions>(RepositoryOptions);
			services.AddTransient<IServiceRepository>(sp => new ServiceRepository(sp.GetService<ILogger<ServiceRepository>>(), sp, RepositoryOptions));
			services.AddTransientWithParameters<IServiceOrchestrator, ServiceOrchestrator<IServiceRepository>>();

			return ApiOptions;
		}
	}
}
