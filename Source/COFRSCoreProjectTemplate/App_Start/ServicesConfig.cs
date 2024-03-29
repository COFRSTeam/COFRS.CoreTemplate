﻿using AutoMapper;
using COFRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Reflection;
using $safeprojectname$.Orchestration;
using $safeprojectname$.Repository;

namespace $safeprojectname$.App_Start
{
	///	<summary>
	///	Services Helper Class
	///	</summary>
	public static class ServiceCollectionExtension
	{
		private static TranslationOptions TranslationOptions { get; set; }
		private static RepositoryOptions RepositoryOptions { get; set; }

		///	<summary>
		///	Configure Services
		///	</summary>
		///	<param name="services">The service collection</param>
		///	<param name="Configuration">The configuration service</param>
		public static void ConfigureServices(this IServiceCollection services, IConfiguration Configuration)
		{
			var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(Configuration);

			services.AddSingleton(new LoggerFactory().AddSerilog(loggerConfig.CreateLogger()));
			services.AddLogging();

			//	If you wish to use caching, uncomment out the next line, and replace the DefaultCacheProvider
			//	with a provider to the caching service of your choice. Also, change the -1 to the number of
			//	megabytes the cache should be limited to (-1 = no limit).

			//	services.AddSingleton<ICacheProvider>(new DefaultCacheProvider(-1));

			//	Configure API Settings
			services.InitializeFactories();

			//	Configure Translation options
			TranslationOptions = new TranslationOptions(Configuration.GetSection("ApiSettings").GetValue<string>("RootUrl"));
			services.AddSingleton<ITranslationOptions>(TranslationOptions);

			var myAssembly = Assembly.GetExecutingAssembly();
			AutoMapperFactory.MapperConfiguration = new MapperConfiguration(cfg => cfg.AddMaps(myAssembly));
			AutoMapperFactory.CreateMapper();

			RepositoryOptions = new RepositoryOptions(Configuration.GetConnectionString("DefaultConnection"),
													  Configuration.GetSection("ApiSettings").GetValue<int>("QueryLimit"),
													  Configuration.GetSection("ApiSettings").GetValue<TimeSpan>("Timeout"));

			services.AddSingleton<IRepositoryOptions>(RepositoryOptions);
			services.AddTransient<IServiceRepository, ServiceRepository>();
			services.AddScoped<IServiceOrchestrator, ServiceOrchestrator>();
		}
	}
}
