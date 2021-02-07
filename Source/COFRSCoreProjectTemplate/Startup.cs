using System;
using System.Collections.Generic;
$if$ ($framework$ == net5.0 )using System.Text.Json;
using System.Text.Json.Serialization;
$endif$using COFRS;
$if$ ($securitymodel$ == OAuth)using COFRS.OAuth;
$endif$using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using $safeprojectname$.App_Start;

namespace $safeprojectname$
{
	///	<summary>
	///	Startup
	///	</summary>
	public class Startup
	{
		///	<summary>
		///	Gets or sets the allowed cors origins for the api
		///	</summary>
		private string[] AllowedCorsOrigins { get; set; }

		///	<summary>
		///	Represents a set of key/value application configuration properties.
		///	</summary>
		public static IConfiguration AppConfig { get; private set; }

		///	<summary>
		///	Initializes the Startup class
		///	</summary>
		///	<param name="configuration">Represents a set of key/value application configuration properties.</param>
		public Startup(IConfiguration configuration)
		{
			AppConfig = configuration;
		}

		///	<summary>
		///	This method gets called by the runtime. Use this method to add services to the container.
		///	</summary>
		///	<param name="services"></param>
		public void ConfigureServices(IServiceCollection services)
		{
			$if$ ($securitymodel$ == OAuth)var authorityUrl = AppConfig["OAuth2:AuthorityURL"];
			var scopes = Scope.Load(AppConfig.GetSection("OAuth2:Scopes"));
			var policies = Policy.Load(AppConfig.GetSection("OAuth2:Policies"));

			$endif$//	Configure services
			var options = services.ConfigureServices(AppConfig);

			$if$ ($framework$ == net5.0)
 			//	Configure JSON formatting
            var defaultSettings = new JsonSerializerOptions
			{
				WriteIndented = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			defaultSettings.Converters.Add(new ApiJsonBitArrayConverter());
			defaultSettings.Converters.Add(new ApiJsonPhysicalAddressConverter());
			defaultSettings.Converters.Add(new ApiJsonIPAddressConverter());
			defaultSettings.Converters.Add(new ApiJsonByteArrayConverter());
			defaultSettings.Converters.Add(new ApiJsonImageConverter());
			defaultSettings.Converters.Add(new ApiJsonBitmapConverter());
			defaultSettings.Converters.Add(new ApiJsonTimeSpanConverter());
			defaultSettings.Converters.Add(new ApiEnumConverterFactory());

			services.Configure<IISServerOptions>(options =>
			{
				options.AllowSynchronousIO = true;
			});

			services.AddSingleton<JsonSerializerOptions>(defaultSettings);

			$endif$$if$ ($framework$ == netcoreapp2.1 )
			//	Configure JSON formatting
			var defaultSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,   //	Null values are omitted from JSON output 
				Formatting = Formatting.Indented,
				DateParseHandling = DateParseHandling.DateTimeOffset,
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				Converters = new List<JsonConverter>
					{
						new ApiJsonSpecialTypesConverter()
					}
			};

			JsonConvert.DefaultSettings = () => { return defaultSettings; };
			services.AddSingleton<JsonSerializerSettings>(defaultSettings);

			$endif$$if$ ($framework$ == netcoreapp3.1 )
			//	Configure JSON formatting
			var defaultSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,   //	Null values are omitted from JSON output 
				Formatting = Formatting.Indented,
				DateParseHandling = DateParseHandling.DateTimeOffset,
				DateFormatHandling = DateFormatHandling.IsoDateFormat,
				Converters = new List<JsonConverter>
						{
							new ApiJsonSpecialTypesConverter()
						}
			};

			JsonConvert.DefaultSettings = () => { return defaultSettings; };
			services.AddSingleton<JsonSerializerSettings>(defaultSettings);

			$endif$//	Configure CORS Origins
			AllowedCorsOrigins = AppConfig["ApiSettings:AllowedCors"].Split(", ");

			services.AddCors(o =>
			{
				o.AddPolicy("default", builder =>
				{
					builder.WithOrigins(AllowedCorsOrigins)
						.AllowAnyHeader()
						.AllowAnyMethod();
				});
			});

			//	Add API versioning
			services.AddApiVersioning(o =>
			{
				o.ApiVersionReader = new ApiVersionReader();
				o.AssumeDefaultVersionWhenUnspecified = true;
				o.DefaultApiVersion = new ApiVersion(1, 0);
			});

			$if$ ( $securitymodel$ == OAuth )var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			//	Only set the requireSSL flag to false in development environments where the Identity Server does not use SSL.
			//	This flag should never be set to false in production environments.
			if (string.Equals(env, "local", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(env, "development", StringComparison.OrdinalIgnoreCase))
			{
				services.AddApiAuthentication(authorityUrl, scopes, policies, false);
			}
			else
			{
				services.AddApiAuthentication(authorityUrl, scopes, policies);
			}

			$endif$$if$ ( $framework$ == netcoreapp3.1 )services.Configure<IISServerOptions>(options =>
			 {
				 options.AllowSynchronousIO = true;
			 });$endif$$if$ ( $framework$ == net5.0 )services.Configure<IISServerOptions>(options =>
			 {
				 options.AllowSynchronousIO = true;
			 });$endif$

			//	Configure Swagger
			$if$ ( $securitymodel$ == OAuth )services.UseSwagger(authorityUrl, options, scopes);$else$services.UseSwagger(options);$endif$

			var supportedJsonTypes = new string[] { "application/json", "text/json", "application/vnd.$companymoniker$.v1+json" };

			$if$ ($framework$ == netcoreapp2.1)services.AddMvc(mvcOptions =>
			{
				var serviceProvider = services.BuildServiceProvider();

				mvcOptions.RespectBrowserAcceptHeader = true; // false by default
				mvcOptions.OutputFormatters.Clear();
				mvcOptions.OutputFormatters.Insert(0, new COFRSJsonFormatter(supportedJsonTypes));
				mvcOptions.InputFormatters.Clear();
				mvcOptions.InputFormatters.Insert(0, new COFRSJsonFormatter(supportedJsonTypes));
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
			$else$services.AddMvc();

			services.AddControllers(o => 
			{
				o.EnableEndpointRouting = false;
				o.OutputFormatters.Clear();
				o.OutputFormatters.Insert(0, new COFRSJsonFormatter(supportedJsonTypes));
				o.InputFormatters.Clear();
				o.InputFormatters.Insert(0, new COFRSJsonFormatter(supportedJsonTypes));
			});
		$endif$}

		///	<summary>
		///	This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		///	</summary>
		///	<param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline</param>
		///	<param name="env">Provides information about the web hosting environment an application is running in</param>
		$if$ ( $framework$ == netcoreapp2.1 )public void Configure(IApplicationBuilder app, IHostingEnvironment env)$else$public void Configure(IApplicationBuilder app, IWebHostEnvironment env)$endif$
		{
			$if$ ( $framework$ == netcoreapp2.1 )if (env.IsDevelopment())$else$if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(env.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase))$endif$
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}

			app.UseCors("default");
			app.ConfigureExceptionHandler(AllowedCorsOrigins);
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRqlHandler();
			$if$ ( $framework$ == netcoreapp3.1 )app.UseRouting();
			$endif$$if$ ( $framework$ == net5.0 )app.UseRouting();
			$endif$app.UseSwagger();


			//	To do: change the client-id and client-secret to something appropriate to your application
			//		   you can obtain this information from your security provider when you get the 
			//		   OAuth client that will be used to call into your service
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "$safeprojectname$ 1.0");
				c.RoutePrefix = string.Empty;
				$if$ ( $securitymodel$ == OAuth )c.OAuthClientId("client-id");
				c.OAuthClientSecret("client-secret");
				c.OAuthScopeSeparator(" ");
				c.OAuthAppName("$safeprojectname$");
			$endif$});

			$if$ ( $security$ == OAuth31)app.UseAuthentication();
			app.UseAuthorization();$endif$
			$if$ ( $security$ == OAuth21)app.UseAuthentication();$endif$

			$if$ ( $framework$ == netcoreapp2.1 )app.UseMvc();$else$app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});$endif$
		}
	}
}
