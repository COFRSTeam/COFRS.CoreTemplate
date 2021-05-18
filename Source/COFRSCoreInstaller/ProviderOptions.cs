using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace COFRS.Template
{
	public class ProviderOptions : IProviderOptions
	{
		public enum CompilerLanguage
		{
			CSharp,
			VB
		}

		private readonly CompilerLanguage _compilerLang;
		private readonly string _rootDirectory;
		private string _compilerPath => _compilerLang == CompilerLanguage.CSharp
												? @"ItemTemplates\CSharp\ASP.NET Core\Web\ASP.NET\1033\COFRSRoslynCompilerTemplate\csc.exe"
												: @"ItemTemplates\CSharp\ASP.NET Core\Web\ASP.NET\1033\COFRSRoslynCompilerTemplate\vbc.exe";

		public ProviderOptions(CompilerLanguage compiler = CompilerLanguage.CSharp)
		{
			_compilerLang = compiler;
			//_rootDirectory = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
			_rootDirectory = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\IDE\\Extensions";

			var subDirectories = Directory.GetDirectories(_rootDirectory);

			foreach (var subDirectory in subDirectories)
			{
				var files = Directory.GetFiles(subDirectory, "COFRSCoreInstaller.dll");

				if (files.ToList().Count() > 0)
				{
					_rootDirectory = subDirectory;
					break;
				}
			}
		}

		public string CompilerVersion => "15.0";

		public bool WarnAsError => false;

		public bool UseAspNetSettings => false;

		public IDictionary<string, string> AllOptions => throw new NotImplementedException();

		public string CompilerFullPath
		{
			get
			{
				return Path.Combine(_rootDirectory, _compilerPath);
			}
		}

		public int CompilerServerTimeToLive => 60 * 15;
	}
}
