using HLSLccCLR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes;
using uTinyRipper.Classes.Shaders;
using uTinyRipper.Classes.Shaders.Exporters;
using Shader = uTinyRipper.Classes.Shader;
using Version = uTinyRipper.Version;

namespace Extract
{
    public class CustomShaderExporter
    {
        bool FilterSubPrograms;
        public CustomShaderExporter(bool filterSubPrograms = true)
        {
            FilterSubPrograms = filterSubPrograms;
        }
        public static bool IsSerialized(Version version)
        {
            return version.IsGreaterEqual(5, 5);
        }
        public static bool IsEncoded(Version version)
        {
            return version.IsGreaterEqual(5, 3);
        }
        //See Shader.ExportBinary
        public void ExportShader(Shader shader, IExportContainer container, Stream stream,
            Func<Version, GPUPlatform, ShaderTextExporter> exporterInstantiator)
        {
            if (IsSerialized(container.Version))
            {
                using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
                {
                    ExportParsedForm(shader.ParsedForm, writer);
                }
            }
            else if (IsEncoded(container.Version))
            {
                using (ShaderWriter writer = new ShaderWriter(stream, shader, exporterInstantiator))
                {
                    string header = Encoding.UTF8.GetString(shader.Script);
                    shader.SubProgramBlob.Export(writer, header);
                }
            }
            else
            {
                var bytes = Encoding.ASCII.GetBytes("/*Default*/\n");
                stream.Write(bytes, 0, bytes.Length);
                shader.ExportBinary(container, stream);
            }
        }
        //See ParsedForm.Export
        void ExportParsedForm(SerializedShader parsedForm, ShaderWriter writer)
        {
            writer.Write("Shader \"{0}\" {{\n", parsedForm.Name);

            parsedForm.PropInfo.Export(writer);

            foreach (SerializedSubShader subShader in parsedForm.SubShaders)
            {
                ExportSerializedSubShader(subShader, writer);
            }

            if (parsedForm.FallbackName != string.Empty)
            {
                writer.WriteIndent(1);
                writer.Write("Fallback \"{0}\"\n", parsedForm.FallbackName);
            }

            if (parsedForm.CustomEditorName != string.Empty)
            {
                writer.WriteIndent(1);
                writer.Write("CustomEditor \"{0}\"\n", parsedForm.CustomEditorName);
            }
            writer.Write('}');
        }
        //See SubShader.Export
        void ExportSerializedSubShader(SerializedSubShader subShader, ShaderWriter writer)
        {
            writer.WriteIndent(1);
            writer.Write("SubShader {\n");
            if (subShader.LOD != 0)
            {
                writer.WriteIndent(2);
                writer.Write("LOD {0}\n", subShader.LOD);
            }
            subShader.Tags.Export(writer, 2);
            foreach (SerializedPass pass in subShader.Passes)
            {
                ExportSerializedPass(pass, writer);
            }
            writer.WriteIndent(1);
            writer.Write("}\n");
        }
        void ExportSerializedPass(SerializedPass pass, ShaderWriter writer)
        {
            writer.WriteIndent(2);
            writer.Write("{0} ", pass.Type.ToString());

            if (pass.Type == SerializedPassType.UsePass)
            {
                writer.Write("\"{0}\"\n", pass.UseName);
            }
            else
            {
                writer.Write("{\n");

                if (pass.Type == SerializedPassType.GrabPass)
                {
                    if (pass.TextureName != string.Empty)
                    {
                        writer.WriteIndent(3);
                        writer.Write("\"{0}\"\n", pass.TextureName);
                    }
                }
                else if (pass.Type == SerializedPassType.Pass)
                {
                    pass.State.Export(writer);

                    ExportSerializedProgram(pass.ProgVertex, writer, ShaderType.Vertex);
                    ExportSerializedProgram(pass.ProgFragment, writer, ShaderType.Fragment);
                    ExportSerializedProgram(pass.ProgGeometry, writer, ShaderType.Geometry);
                    ExportSerializedProgram(pass.ProgHull, writer, ShaderType.Hull);
                    ExportSerializedProgram(pass.ProgDomain, writer, ShaderType.Domain);

#warning ProgramMask?
#warning HasInstancingVariant?
                }
                else
                {
                    throw new NotSupportedException($"Unsupported pass type {pass.Type}");
                }

                writer.WriteIndent(2);
                writer.Write("}\n");
            }
        }
        private static HashSet<ShaderGpuProgramType> GetIsTierLookup(IReadOnlyList<SerializedSubProgram> subPrograms)
        {
            HashSet<ShaderGpuProgramType> lookup = new HashSet<ShaderGpuProgramType>();
            Dictionary<ShaderGpuProgramType, byte> seen = new Dictionary<ShaderGpuProgramType, byte>();
            foreach (SerializedSubProgram subProgram in subPrograms)
            {
                if (seen.ContainsKey(subProgram.GpuProgramType))
                {
                    if (seen[subProgram.GpuProgramType] != subProgram.ShaderHardwareTier)
                    {
                        lookup.Add(subProgram.GpuProgramType);
                    }
                }
                else
                {
                    seen[subProgram.GpuProgramType] = subProgram.ShaderHardwareTier;
                }
            }
            return lookup;
        }
        void ExportSerializedProgram(SerializedProgram serializedProgram, ShaderWriter writer, ShaderType type)
        {
            if (serializedProgram.SubPrograms.Count > 0)
            {
                writer.WriteIndent(3);
                writer.Write("Program \"{0}\" {{\n", type.ToProgramTypeString());
                HashSet<ShaderGpuProgramType> isTierLookup = GetIsTierLookup(serializedProgram.SubPrograms);
                var subprograms = serializedProgram.SubPrograms;
                var best = serializedProgram.SubPrograms
                    .OrderByDescending(subProgram =>
                    {
                        GPUPlatform platform = subProgram.GpuProgramType.ToGPUPlatform(writer.Platform);
                        int index = writer.Shader.Platforms.IndexOf(platform);
                        ShaderSubProgramBlob blob = writer.Shader.SubProgramBlobs[index];
                        var sp = blob.SubPrograms[(int)subProgram.BlobIndex];
                        return sp.ProgramData.Count;
                        //return sp.GlobalKeywords.Sum(keyword => keyword.Contains("ON") ? 2 : 1);
                    }).FirstOrDefault();
                if (FilterSubPrograms)
                {
                    subprograms = new SerializedSubProgram[] { best };
                }
                foreach (SerializedSubProgram subProgram in subprograms)
                {
                    Platform uplatform = writer.Platform;
                    GPUPlatform platform = subProgram.GpuProgramType.ToGPUPlatform(uplatform);
                    int index = writer.Shader.Platforms.IndexOf(platform);
                    ShaderSubProgramBlob blob = writer.Shader.SubProgramBlobs[index];
                    bool isTier = isTierLookup.Contains(subProgram.GpuProgramType);
                    ExportSerializedSubProgram(subProgram, writer, blob, type, isTier, best.BlobIndex == subProgram.BlobIndex);
                }
                writer.WriteIndent(3);
                writer.Write("}\n");
            }
        }

        void ExportSerializedSubProgram(SerializedSubProgram subProgram, ShaderWriter writer, ShaderSubProgramBlob blob, ShaderType type, bool isTier, bool isBest)
        {
            writer.WriteIndent(4);
            writer.Write("SubProgram \"{0} ", subProgram.GpuProgramType.ToGPUPlatform(writer.Platform));
            if (isTier)
            {
                writer.Write("hw_tier{0} ", subProgram.ShaderHardwareTier.ToString("00"));
            }
            writer.Write("\" {\n");
            writer.WriteIndent(5);

            var shaderSubProgram = blob.SubPrograms[(int)subProgram.BlobIndex];
            string hash = Hash(shaderSubProgram.ProgramData.ToArray());
            var filesteam = writer.BaseStream as FileStream;
            var folder = Path.GetDirectoryName(filesteam.Name);

            DXShaderObjectExporter.FixShaderSubProgram(shaderSubProgram,
                subProgram);
            ExportShaderSubProgram(shaderSubProgram, writer, type, isBest);

            writer.Write('\n');
            writer.WriteIndent(4);
            writer.Write("}\n");
        }
        static bool IsReadLocalKeywords(Version version)
        {
            return version.IsGreaterEqual(2019);
        }
        static string Hash(byte[] data)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(data);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        //Refer ShaderSubProgram.Export
        void ExportShaderSubProgram(ShaderSubProgram subProgram, ShaderWriter writer, ShaderType type, bool isBest)
        {
            if (subProgram.GlobalKeywords.Count > 0)
            {
                writer.Write("Keywords { ");
                foreach (string keyword in subProgram.GlobalKeywords)
                {
                    writer.Write("\"{0}\" ", keyword);
                }
                if (IsReadLocalKeywords(writer.Version))
                {
                    foreach (string keyword in subProgram.LocalKeywords)
                    {
                        writer.Write("\"{0}\" ", keyword);
                    }
                }
                writer.Write("}\n");
                writer.WriteIndent(5);
            }

            writer.Write("\"!!{0}", subProgram.ProgramType.ToShaderName(writer.Platform, type));
            if (subProgram.ProgramData.Count > 0)
            {
                writer.Write("\n");
                writer.WriteIndent(5);
                switch (subProgram.ProgramType.ToGPUPlatform(writer.Platform))
                {
                    case GPUPlatform.d3d11:
                    case GPUPlatform.d3d11_9x:
                    case GPUPlatform.d3d9:
                        ExportDebug(subProgram, writer, type, isBest);
                        break;
                    default:
                        writer.WriteShaderData(
                            subProgram.ProgramType.ToGPUPlatform(writer.Platform),
                            subProgram.ProgramData.ToArray());
                        break;
                }

            }
            writer.Write('"');
        }
        [HandleProcessCorruptedStateExceptions]
        void ExportGLSL(ShaderSubProgram subProgram, ShaderWriter writer, ShaderType type, bool isBest)
        {

        }
        [HandleProcessCorruptedStateExceptions]
        void ExportDebug(ShaderSubProgram subProgram, ShaderWriter writer, ShaderType type, bool isBest)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            string hash = Hash(subProgram.ProgramData.ToArray());

            Logger.Log(LogType.Debug, LogCategory.Export, $"Exporting Subprogram {hash}");

            var data = DXShaderObjectExporter.GetObjectData(writer.Version, subProgram.ProgramType.ToGPUPlatform(writer.Platform), subProgram);

            var filesteam = writer.BaseStream as FileStream;
            var folder = Path.GetDirectoryName(filesteam.Name);

            var dxExporter = new uTinyRipperGUI.Exporters.ShaderDXExporter(
                writer.Shader.File.Version, subProgram.ProgramType.ToGPUPlatform(writer.Platform));
            string asmPath = $"{folder}/{hash}.dxasm";
            string objPath = $"{folder}/{hash}.o";
            string glslPath = $"{folder}/{hash}.glsl";
            writer.WriteLine($"// {hash}.glsl");
            writer.WriteLine($"// Objfile Length {data.Length}");
            var variableCount = subProgram.ConstantBuffers
                .Select(cb => cb.MatrixParams.Count + cb.VectorParams.Count)
                .Sum();
            writer.WriteLine($"// Variables {variableCount}");
            var keyWordScore = 0;
            keyWordScore += subProgram.GlobalKeywords.Sum(keyword => keyword.Contains("ON") ? 2 : 1);
            keyWordScore += subProgram.LocalKeywords == null ? 0 : subProgram.LocalKeywords.Sum(keyword => keyword.Contains("ON") ? 2 : 1);
            writer.WriteLine($"// KeywordScore {keyWordScore}");
            writer.WriteLine($"// Best {isBest}");

            if (!File.Exists(glslPath))
            {
                try
                {
                    var ext = new CLRGlExtensions();
                    ext.ARB_explicit_attrib_location = 1;
                    ext.ARB_explicit_uniform_location = 1;
                    ext.ARB_shading_language_420pack = 0;
                    ext.OVR_multiview = 0;
                    ext.EXT_shader_framebuffer_fetch = 0;
                    var shader = HLSLccWrapper.TranslateFromMem(data,
                        CLRGLLang.LANG_DEFAULT, ext);
                    if (shader.OK != 0)
                    {
                        File.WriteAllText(glslPath, "OK");
                    }
                    else
                    {
                        File.WriteAllText(glslPath, $"FailCode {shader.OK}");
                        writer.WriteLine($"//Error with {hash}");
                        writer.WriteLine($"FailCode {shader.OK}");
                    }
                    Logger.Log(LogType.Debug, LogCategory.Export, $"Cross Compiled HLSL {stopWatch.ElapsedMilliseconds} ms");
                    stopWatch.Restart();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Debug, LogCategory.Export, $"Cross Compiled HLSL {stopWatch.ElapsedMilliseconds} ms");
                    stopWatch.Restart();
                    writer.WriteLine($"//Error with {hash}");
                    writer.WriteLine($"//{ex.ToString()}");
                    File.WriteAllBytes(objPath, data);
                    File.WriteAllText(glslPath, "Exception");

                    using (var sw = new StreamWriter(asmPath))
                    {
                        dxExporter.Export(subProgram.ProgramData.ToArray(), sw);
                    }
                    Logger.Log(LogType.Debug, LogCategory.Export, $"Exported asm {stopWatch.ElapsedMilliseconds} ms");
                    stopWatch.Restart();
                }
                
            }
            stopWatch.Stop();
            //File.Delete(objPath);
        }
    }
}
