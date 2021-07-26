using COFRS;
$if$ ($securitymodel$ == OAuth)using COFRS.OAuth;
$endif$using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Swashbuckle.AspNetCore.Filters;

namespace $safeprojectname$.App_Start
{
	/// <summary>
	/// Swagger Config
	/// </summary>
	public static class SwaggerConfig
{
		/// <summary>
		/// Register swagger
		/// </summary>
		/// <param name="services">The service collection</param>
		///	<param name="options">The api options for the service</param>
		$if$ ($securitymodel$ == OAuth)///	<param name="authorityUrl">The base Url for the identity server</param> 
		///	<param name="scopes">The scopes allowed by this service</param>
		$endif$/// <returns></returns>
		$if$ ($securitymodel$ == OAuth)public static IServiceCollection UseSwagger(this IServiceCollection services, string authorityUrl, IApiOptions options, List<Scope> scopes)$else$public static IServiceCollection UseSwagger(this IServiceCollection services, IApiOptions options)$endif$
		{
			$if$ ($securitymodel$ == OAuth)var tokenEndpoint = AuthenticationServices.GetTokenEndpoint(authorityUrl).GetAwaiter().GetResult();
			var authorizationEndpoint = AuthenticationServices.GetAuthorizationEndpoint(authorityUrl).GetAwaiter().GetResult();$endif$
			   
			//	Configure Swagger
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1.0", new OpenApiInfo
				{
					Title = "$safeprojectname$",
					Version = "V1.0",
					Contact = new OpenApiContact { Name = "Author", Email = "author@gmail.com", Url = new Uri("http://author.com") },
					Description = @"
<p style=""font-family:verdana; color:#6495ED;"">A detailed description of the service goes here.The description
should give the reader a good idea of what the service does, and should list any dependencies or
restrictions upon its use. The description is written in HTML, so don't be afraid to use formatting
constructs, such as <b>bold</b> and other HTML attributes to enhance the appearance of your description.</p>
<p style=""font-family:verdana; color:#6495ED;"">A professional Web Service uses detailed descriptions to enhance its usablity, and the descriptions
should be visually appealing as well.</p>"
				});

				c.DocInclusionPredicate((docName, apiDesc) =>
				{
					var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
					//	would mean this action is unversioned and should be included everywhere
					if (actionApiVersionModel == null)
					{
						return true;
					}
					if (actionApiVersionModel.DeclaredApiVersions.Any())
					{
						return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v}" == docName);
					}
					return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v}" == docName);
				});

				
				$if$ ($security$ == OAuth31)var scopesDict = new Dictionary<string, string>();
				var scopeList = new List<string>();

				foreach (var scope in scopes)
				{
					scopesDict.Add(scope.Name, "");
					scopeList.Add(scope.Name);
				}

				var flows = new OpenApiOAuthFlows
				{
					ClientCredentials = new OpenApiOAuthFlow
					{
						AuthorizationUrl = new Uri(authorizationEndpoint),
						RefreshUrl = new Uri(tokenEndpoint),
						Scopes = scopesDict,
						TokenUrl = new Uri(tokenEndpoint)
					}
				};

				c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
				{
                    Type = SecuritySchemeType.OAuth2,
				    Flows = new OpenApiOAuthFlows
					{
						ClientCredentials = new OpenApiOAuthFlow
						{
							AuthorizationUrl = new Uri(authorizationEndpoint),
							RefreshUrl = new Uri(tokenEndpoint),
							Scopes = scopesDict,
							TokenUrl = new Uri(tokenEndpoint)
						}
					},
					Description = "OAuth2 Client Credentials Flow"
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
						},
						scopeList.ToArray()
					}
				});$endif$
				$if$ ($security$ == OAuth21)var scopesDict = new Dictionary<string, string>();

				foreach (var scope in scopes)
				{
					scopesDict.Add(scope.Name, "");
				}

				c.AddSecurityDefinition("oauth2", new OAuth2Scheme()
				{
					Type = "oauth2",
					Flow = "application",
					TokenUrl = tokenEndpoint,
					AuthorizationUrl = authorizationEndpoint,
					Scopes = scopesDict,
					Description = "OAuth2 Client Credentials Flow"
				});

				c.OperationFilter<AssignOAuth2SecurityRequirements>();$endif$
				c.OperationFilter<ApiSwaggerFilter>(options);
				c.ExampleFilters();

				c.IncludeXmlComments(GetXmlPath("$safeprojectname$.xml"));
				c.IncludeXmlComments(GetXmlPath("COFRS.xml"));
				c.IncludeXmlComments(GetXmlPath("COFRS.Common.xml"));
			});

			services.AddSwaggerExamplesFromAssemblies(new Assembly[] { Assembly.GetExecutingAssembly() });
			return services;
		}

		///	<summary>
		///	Converts a file name to a fullly qualified file name
		///	</summary>
		///	<param name="filename"></param>
		///	<returns></returns>
		private static string GetXmlPath(string filename)
		{
			return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), filename);
		}
	}
}
