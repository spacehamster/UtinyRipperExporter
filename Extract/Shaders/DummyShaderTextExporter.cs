using System;
using System.IO;
using System.Text;
using uTinyRipper;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Converters;
using uTinyRipper.Converters.Shaders;
using Version = uTinyRipper.Version;

namespace Extract
{
	class DummyShaderTextExporter : ShaderTextExporter
	{
		static string DummyShader = @"
	//DummyShader
	SubShader{
		Tags { ""RenderType"" = ""Opaque"" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard fullforwardshadows
#pragma target 3.0
		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
		}
		ENDCG
	}
";
		public override void Export(ShaderWriter writer, ref ShaderSubProgram subProgram)
		{
			writer.Write("/*HelloWorld*/");
		}

		public static bool IsSerialized(Version version)
		{
			return version.IsGreaterEqual(5, 5);
		}
		public static bool IsEncoded(Version version)
		{
			return version.IsGreaterEqual(5, 3);
		}
		public static void ExportShader(Shader shader, IExportContainer container, Stream stream,
			Func<Version, GPUPlatform, ShaderTextExporter> exporterInstantiator)
		{
			//Importing Hidden/Internal shaders causes the unity editor screen to turn black
			if (shader.ParsedForm.Name?.StartsWith("Hidden/") == true) return;
			if (Shader.IsSerialized(container.Version))
			{
				using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
				{
					writer.Write("Shader \"{0}\" {{\n", shader.ParsedForm.Name);
					shader.ParsedForm.PropInfo.Export(writer);
					writer.WriteIndent(1);
					writer.Write(DummyShader);
					if (shader.ParsedForm.FallbackName != string.Empty)
					{
						writer.WriteIndent(1);
						writer.Write("Fallback \"{0}\"\n", shader.ParsedForm.FallbackName);
					}
					if (shader.ParsedForm.CustomEditorName != string.Empty)
					{
						writer.WriteIndent(1);
						writer.Write("//CustomEditor \"{0}\"\n", shader.ParsedForm.CustomEditorName);
					}
					writer.Write('}');
				}
			}
			else
			{
				if (IsSerialized(container.Version))
				{
					using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
					{
						shader.ParsedForm.Export(writer);
					}
				}
				else if (IsEncoded(container.Version))
				{
					using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
					{
						string header = Encoding.UTF8.GetString(shader.Script);
						var subshaderIndex = header.IndexOf("SubShader");
						writer.WriteString(header, 0, subshaderIndex);
						writer.WriteIndent(1);
						writer.Write(DummyShader);
						writer.Write('}');
					}
				}
				else
				{
					shader.ExportBinary(container, stream);
				}
			}
		}
	}
}
