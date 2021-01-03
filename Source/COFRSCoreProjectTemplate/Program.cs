using COFRS;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace $safeprojectname$
{
	///	<summary>
	///	The system entry point
	///	</summary>
	public class Program
	{
		///	<summary>
		///	The main entry point into the system
		///	</summary>
		///	<param name="args">The list of command line arguments</param>
		public static void Main(string[] args)
		{
			try
			{
				var host = CreateWebHostBuilder(args).Build();

				using (var serviceScope = host.Services.CreateScope())
				{
					ServiceContainer.RequestServices = serviceScope.ServiceProvider;
					host.Run();
				}
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		///	<summary>
		///	Helper function to create the web host builder
		///	</summary>
		///	<param name="args"></param>
		///	<returns></returns>
		public static IWebHostBuilder CreateWebHostBuilder(string[] args)
		{
			new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{ Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables().Build();

			//	In this fuction, we will configure logging using Serilog. See LoggingOptions under
			//	logging for details.
			return WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>();
		}
	}
}
