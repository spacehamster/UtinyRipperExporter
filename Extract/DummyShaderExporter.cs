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
using Object = uTinyRipper.Classes.Object;
using Version = uTinyRipper.Version;

namespace Extract
{

    public class DummyShaderExporter : IAssetExporter
    {
        public IExportCollection CreateCollection(VirtualSerializedFile virtualFile, Object asset)
        {
            return new AssetExportCollection(this, asset);
        }

        public bool Export(IExportContainer container, Object asset, string path)
        {
            Export(container, asset, path, null);
            return true;
        }


        public void Export(IExportContainer container, Object asset, string path, Action<IExportContainer, Object, string> callback)
        {
            using (Stream fileStream = FileUtils.CreateVirtualFile(path))
            {
                Shader shader = (Shader)asset;
                DummyShaderTextExporter.ExportShader(shader, container, fileStream, ShaderExporterInstantiator);

            }
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



        public bool IsHandle(Object asset, ExportOptions exportOptions)
        {
            return true;
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

        private static ShaderTextExporter ShaderExporterInstantiator(Version version, GPUPlatform graphicApi)
        {
            switch (graphicApi)
            {
                case GPUPlatform.unknown:
                    return new ShaderTextExporter();

                case GPUPlatform.openGL:
                case GPUPlatform.gles:
                case GPUPlatform.gles3:
                case GPUPlatform.glcore:
                    return new ShaderGLESExporter();

                case GPUPlatform.metal:
                    return new ShaderMetalExporter(version);

                default:
                    return new DummyShaderTextExporter();
            }
        }

    }
}
