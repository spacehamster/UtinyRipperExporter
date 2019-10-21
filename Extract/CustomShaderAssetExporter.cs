using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Classes.Shaders.Exporters;
using uTinyRipper.SerializedFiles;
using uTinyRipperGUI.Exporters;
using Object = uTinyRipper.Classes.Object;
using Version = uTinyRipper.Version;

namespace Extract
{

	public class CustomShaderAssetExporter : IAssetExporter
	{
		ExportSettings settings;
		public CustomShaderAssetExporter()
		{
			settings = new ExportSettings();
		}
		public CustomShaderAssetExporter(ExportSettings settings)
		{
			this.settings = settings;
		}
		public bool IsHandle(Object asset, ExportOptions options)
		{
			return true;
		}

		//Refer ShaderAssetExporter
		public bool Export(IExportContainer container, Object asset, string path)
		{
			using (Stream fileStream = FileUtils.CreateVirtualFile(path))
			{
				Shader shader = (Shader)asset;
				if (settings.ShaderExportMode == ShaderExportMode.Dummy)
				{
					DummyShaderTextExporter.ExportShader(shader, container, fileStream, DefaultShaderExporterInstantiator);
				}
				else if (settings.ShaderExportMode == ShaderExportMode.GLSL || settings.ShaderExportMode == ShaderExportMode.Metal)
				{
					var exporter = new CustomShaderExporter(settings.CondenseShaderSubprograms);
					exporter.ExportShader(shader, container, fileStream, DefaultShaderExporterInstantiator);
				} else if(settings.ShaderExportMode == ShaderExportMode.Asm)
				{
					shader.ExportBinary(container, fileStream, DXShaderExporterInstantiator);
				} else
				{
					shader.ExportBinary(container, fileStream, DefaultShaderExporterInstantiator);
				}
				
			}
			return true;
		}


		public void Export(IExportContainer container, Object asset, string path, Action<IExportContainer, Object, string> callback)
		{
			Export(container, asset, path);
			callback?.Invoke(container, asset, path);
		}

		public bool Export(IExportContainer container, IEnumerable<Object> assets, string path)
		{
			throw new NotSupportedException();
		}

		public void Export(IExportContainer container, IEnumerable<Object> assets, string path, Action<IExportContainer, Object, string> callback)
		{
			throw new NotSupportedException();
		}

		public IExportCollection CreateCollection(VirtualSerializedFile virtualFile, Object asset)
		{
			return new AssetExportCollection(this, asset);
		}

		public AssetType ToExportType(Object asset)
		{
			ToUnknownExportType(asset.ClassID, out AssetType assetType);
			return assetType;
		}

		public bool ToUnknownExportType(ClassIDType classID, out AssetType assetType)
		{
			assetType = AssetType.Meta;
			return true;
		}

		private static ShaderTextExporter DefaultShaderExporterInstantiator(Version version, GPUPlatform graphicApi)
		{
			switch (graphicApi)
			{
				case GPUPlatform.vulkan:
					return new ShaderVulkanExporter();
				default:
					return Shader.DefaultShaderExporterInstantiator(version, graphicApi);
			}
		}
		private static ShaderTextExporter DXShaderExporterInstantiator(Version version, GPUPlatform graphicApi)
		{
			switch (graphicApi)
			{
				case GPUPlatform.d3d9:
				case GPUPlatform.d3d11_9x:
				case GPUPlatform.d3d11:
					return new ShaderDXExporter(graphicApi);
				case GPUPlatform.vulkan:
					return new ShaderVulkanExporter();

				default:
					return Shader.DefaultShaderExporterInstantiator(version, graphicApi);
			}
		}

	}
}
