using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extract
{
	public enum ShaderExportMode
	{
		Dummy,
		Asm,
		GLSL,
		Metal,
		Default
	}
	public class ExportSettings
	{
		public ExportSettings()
		{
			foreach(var prop in this.GetType().GetProperties())
			{
				var attr = Attribute.GetCustomAttribute(prop, typeof(OptionAttribute)) as OptionAttribute;
				if(attr != null)
				{
					prop.SetValue(this, attr.Default);
				}
			}
		}
		[Option('g', "gamedir", Required = true, HelpText = "Set the root directiory where unity assets are located.")]
		public string GameDir { get; set; }
		[Option('f', "file", Required = false, HelpText = "Set the file to extract")]
		public string File { get; set; }
		[Option('o', "output", Required = true, HelpText = "Set the directory to output files")]
		public string ExportDir { get; set; }
		[Option('c', "guidbycontent", Required = false, HelpText = "Generate guid by content")]
		public bool GUIDByContent { get; set; }
		[Option('d', "exportdependencies", Default = true, Required = false, HelpText = "Export dependencies")]
		public bool ExportDependencies { get; set; }
		[Option('s', "scripts", Default = false, Required = false, HelpText = "Export scripts")]
		public bool ExportScripts { get; set; }
		[Option('i', "info", Default = false, Required = false, HelpText = "Export asset file info")]
		public bool DumpInfo { get; set; }
		[Option('s', "fixscripts", Default = false, Required = false, HelpText = "Fix exported scripts")]
		public bool FixScripts { get; set; }
		[Option("scriptbyname", Default = true, Required = false, HelpText = "Export script guid by script name")]
		public bool ScriptByName { get; set; }
		[Option("shaderbyname", Default = true, Required = false, HelpText = "Export shader guid by script name")]
		public bool ShaderByName { get; set; }
		[Option("organizescriptableobjects", Default = true, Required = false, HelpText = "Export scriptable objects in seperate folders by type")]
		public bool OrganizeScriptableObjects { get; set; }
		[Option("shadermode", Default = ShaderExportMode.Dummy, Required = false, HelpText = "Shader export mode")]
		public ShaderExportMode ShaderExportMode { get; set; }
		[Option("condenseshadersubprograms", Default = false, Required = false, HelpText = "Condense the number of shader subprograms that are exported")]
		public bool CondenseShaderSubprograms { get; set; }
	}
}
