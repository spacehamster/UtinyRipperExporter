using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Classes.Shaders;
using Version = uTinyRipper.Version;

namespace Extract
{
    public class DXShaderObjectExporter
    {
        private const uint DXBCFourCC = 0x43425844;
        public static bool IsEncoded(Version version)
        {
            return version.IsGreaterEqual(5, 3);
        }
        public static bool IsSerialized(Version version)
        {
            return version.IsGreaterEqual(5, 5);
        }
        private static bool IsOffset(GPUPlatform graphicApi)
        {
            return graphicApi != GPUPlatform.d3d9;
        }

        private static bool IsOffset5(Version version)
        {
            return version.IsEqual(5, 3);
        }
        public enum ShaderFlags
        {
            None = 0,
            Debug = 1,
            SkipValidation = 2,
            SkipOptimization = 4,
            PackMatrixRowMajor = 8,
            PackMatrixColumnMajor = 16,
            PartialPrecision = 32,
            ForceVsSoftwareNoOpt = 64,
            ForcePsSoftwareNoOpt = 128,
            NoPreshader = 256,
            AvoidFlowControl = 512,
            PreferFlowControl = 1024,
            EnableStrictness = 2048,
            EnableBackwardsCompatibility = 4096,
            IeeeStrictness = 8192,
            OptimizationLevel0 = 16384,
            OptimizationLevel1 = 0,
            OptimizationLevel2 = 49152,
            OptimizationLevel3 = 32768,
            Reserved16 = 65536,
            Reserved17 = 131072,
            WarningsAreErrors = 262144
        }
        public enum DXProgramType
        {
            PixelShader = 0xFFFF,
            VertexShader = 0xFFFE,
            GeometryShader = 0x4753,
            HullShader = 0x4853,
            DomainShader = 0x4453,
            ComputeShader = 0x4353
        }
        public enum ShaderInputType
        {
            CBuffer = 0,
            TBuffer = 1,
            Texture = 2,
            Sampler = 3,
            UavRwTyped = 4,
            Structured = 5,
            UavRwStructured = 6,
            ByteAddress = 7,
            UavRwByteAddress = 8,
            UavAppendStructured = 9,
            UavConsumeStructured = 10,
            UavRwStructuredWithCounter = 11
        }
        public enum ResourceReturnType
        {
            NotApplicable = 0,
            UNorm = 1,
            SNorm = 2,
            SInt = 3,
            UInt = 4,
            Float = 5,
            Mixed = 6,
            Double = 7,
            Continued = 8
        }
        public enum ShaderResourceViewDimension
        {
            Unknown = 0,
            Buffer = 1,
            Texture1D = 2,
            Texture1DArray = 3,
            Texture2D = 4,
            Texture2DArray = 5,
            Texture2DMultiSampled = 6,
            Texture2DMultiSampledArray = 7,
            Texture3D = 8,
            TextureCube = 9,
            TextureCubeArray = 10,
            ExtendedBuffer = 11
        }
        public enum ShaderInputFlags
        {
            None,
            UserPacked = 0x1,
            ComparisonSampler = 0x2,
            TextureComponent0 = 0x4,
            TextureComponent1 = 0x8,
            TextureComponents = 0xc,
            Unused = 0x10
        }
        public enum ConstantBufferFlags
        {
            None = 0,
            UserPacked = 1
        }
        public enum ConstantBufferType
        {
            ConstantBuffer,
            TextureBuffer,
            InterfacePointers,
            ResourceBindInformation
        }
        public enum ShaderVariableClass
        {
            Scalar,
            Vector,
            MatrixRows,
            MatrixColumns,
            Object,
            Struct,
            InterfaceClass,
            InterfacePointer
        }
        public enum ShaderVariableType
        {
            Void = 0,
            Bool = 1,
            Int = 2,
            Float = 3,
            String = 4,
            Texture = 5,
            Texture1D = 6,
            Texture2D = 7,
            Texture3D = 8,
            TextureCube = 9,
            Sampler = 10,
            PixelShader = 15,
            VertexShader = 16,
            UInt = 19,
            UInt8 = 20,
            GeometryShader = 21,
            Rasterizer = 22,
            DepthStencil = 23,
            Blend = 24,
            Buffer = 25,
            CBuffer = 26,
            TBuffer = 27,
            Texture1DArray = 28,
            Texture2DArray = 29,
            RenderTargetView = 30,
            DepthStencilView = 31,
            Texture2DMultiSampled = 32,
            Texture2DMultiSampledArray = 33,
            TextureCubeArray = 34,
            // The following are new in D3D11.
            HullShader = 35,
            DomainShader = 36,
            InterfacePointer = 37,
            ComputeShader = 38,
            Double = 39,
            ReadWriteTexture1D,
            ReadWriteTexture1DArray,
            ReadWriteTexture2D,
            ReadWriteTexture2DArray,
            ReadWriteTexture3D,
            ReadWriteBuffer,
            ByteAddressBuffer,
            ReadWriteByteAddressBuffer,
            StructuredBuffer,
            ReadWriteStructuredBuffer,
            AppendStructuredBuffer,
            ConsumeStructuredBuffer,
            // Only used as a marker when analyzing register types
            ForceInt = 152,
            // Integer that can be either signed or unsigned. Only used as an intermediate step when doing data type analysis
            IntAmbiguous = 153,
            // Partial precision types. Used when doing type analysis
            Float10 = 53,
            Float16 = 54,
            Int16 = 156,
            Int12 = 157,
            Uint16 = 158,
            ForceDWord = 0x7fffffff
        }
        public enum ShaderVariableFlags
        {
            None = 0,
            UserPacked = 1,
            Used = 2,
            InterfacePointer = 4,
            InterfaceParameter = 8
        }
        public class ShaderType
        {
            public ShaderVariableClass ShaderVariableClass;
            public ShaderVariableType ShaderVariableType;
            public ushort Rows;
            public ushort Columns;
            public ushort ElementCount;
            public ushort MemberCount;
            public uint MemberOffset;
            //SM 5.0 Variables
            public uint parentTypeOffset = 0;
            public uint unknown2 = 0;
            public uint unknown4 = 0;
            public uint unknown5 = 0;
            public uint parentNameOffset;

            ShaderGpuProgramType programType;
            public ShaderType(MatrixParameter matrixParam, ShaderGpuProgramType programType)
            {
                ShaderVariableClass = ShaderVariableClass.MatrixColumns; //TODO: matrix colums or rows?
                ShaderVariableType = GetVariableType(matrixParam.Type);
                Rows = matrixParam.RowCount;
                Columns = matrixParam.RowCount;
                ElementCount = 0;
                MemberCount = 0;
                MemberOffset = 0;
                this.programType = programType;
            }
            public ShaderType(VectorParameter vectorParam, ShaderGpuProgramType programType)
            {
                ShaderVariableClass = vectorParam.Dim > 1 ?
                    ShaderVariableClass.Vector :
                    ShaderVariableClass.Scalar;
                ShaderVariableType = GetVariableType(vectorParam.Type);
                Rows = 1;
                Columns = vectorParam.Dim;
                ElementCount = 0;
                MemberCount = 0;
                MemberOffset = 0;
                this.programType = programType;
            }
            static ShaderVariableType GetVariableType(ShaderParamType paramType)
            {
                switch (paramType)
                {
                    case ShaderParamType.Bool:
                        return ShaderVariableType.Bool;
                    case ShaderParamType.Float:
                        return ShaderVariableType.Float;
                    case ShaderParamType.Half:
                        return ShaderVariableType.Float16;
                    case ShaderParamType.Int:
                        return ShaderVariableType.Int;
                    case ShaderParamType.Short:
                        return ShaderVariableType.Int16; //TODO
                    case ShaderParamType.TypeCount:
                        return ShaderVariableType.Int; //TODO
                    case ShaderParamType.UInt:
                        return ShaderVariableType.UInt;
                    default:
                        throw new Exception($"Unexpected param type {paramType}");
                }
            }
            public override bool Equals(object obj)
            {
                var shaderType = obj as ShaderType;
                if (shaderType == null) return false;
                return (ShaderVariableClass == shaderType.ShaderVariableClass &&
                        ShaderVariableType == shaderType.ShaderVariableType &&
                        Rows == shaderType.Rows &&
                        Columns == shaderType.Columns &&
                        ElementCount == shaderType.ElementCount &&
                        MemberCount == shaderType.MemberCount &&
                        MemberOffset == shaderType.MemberOffset);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = -1667745916;
                    hashCode = hashCode * -1521134295 + ShaderVariableClass.GetHashCode();
                    hashCode = hashCode * -1521134295 + ShaderVariableType.GetHashCode();
                    hashCode = hashCode * -1521134295 + Rows.GetHashCode();
                    hashCode = hashCode * -1521134295 + Columns.GetHashCode();
                    hashCode = hashCode * -1521134295 + ElementCount.GetHashCode();
                    hashCode = hashCode * -1521134295 + MemberCount.GetHashCode();
                    hashCode = hashCode * -1521134295 + MemberOffset.GetHashCode();
                    return hashCode;
                }
            }
            public uint Size()
            {
                uint variableSize = 4; //TODO: does this vary with ShaderVariableType? 
                return variableSize * Rows * Columns;
            }
            public uint Length()
            {
                var majorVersion = GetMajorVersion(programType);
                return majorVersion >= 5 ? (uint)36 : (uint)16;
            }
        }
        public class Variable
        {
            public ShaderType ShaderType;
            public Variable(MatrixParameter param, ShaderGpuProgramType prgramType)
            {
                ShaderType = new ShaderType(param, prgramType);
                Name = param.Name;
                NameIndex = param.NameIndex;
                Index = param.Index;
                ArraySize = param.ArraySize;
                Dim = param.RowCount;
                if (Name == null) throw new Exception("Variable name cannot be null");
            }
            public Variable(VectorParameter param, ShaderGpuProgramType prgramType)
            {
                ShaderType = new ShaderType(param, prgramType);
                Name = param.Name;
                NameIndex = param.NameIndex;
                Index = param.Index;
                ArraySize = param.ArraySize;
                Dim = param.Dim;
                if (Name == null) throw new Exception("Variable name cannot be null");
            }
            public Variable(string name, int index, int sizeToAdd, ShaderGpuProgramType prgramType)
            {
                if (sizeToAdd % 4 != 0 || sizeToAdd <= 0) throw new Exception($"Invalid dummy variable size {sizeToAdd}");
                var param = new VectorParameter(name, ShaderParamType.Int, index, sizeToAdd / 4);
                ShaderType = new ShaderType(param, prgramType);
                Name = name;
                NameIndex = -1;
                Index = index;
                ArraySize = 0;
                Type = ShaderParamType.Int;
                Dim = (byte)(sizeToAdd / 4);
                if (Name == null) throw new Exception("Variable name cannot be null");
            }
            public string Name;
            public int NameIndex;
            public int Index;
            public int ArraySize;
            public ShaderParamType Type;
            public byte Dim;
        }
        public static DXProgramType GetDXProgramType(ShaderGpuProgramType prgramType)
        {
            switch (prgramType)
            {
                case ShaderGpuProgramType.DX11PixelSM40:
                case ShaderGpuProgramType.DX11PixelSM50:
                    return DXProgramType.PixelShader;
                case ShaderGpuProgramType.DX11VertexSM40:
                case ShaderGpuProgramType.DX11VertexSM50:
                    return DXProgramType.VertexShader;
                case ShaderGpuProgramType.DX11GeometrySM40:
                case ShaderGpuProgramType.DX11GeometrySM50:
                    return DXProgramType.GeometryShader;
                case ShaderGpuProgramType.DX11HullSM50:
                    return DXProgramType.HullShader;
                case ShaderGpuProgramType.DX11DomainSM50:
                    return DXProgramType.DomainShader;
                default:
                    throw new Exception($"Unexpected program type {prgramType}");
            }
        }
        public static int GetMajorVersion(ShaderGpuProgramType prgramType)
        {
            switch (prgramType)
            {
                case ShaderGpuProgramType.DX11PixelSM40:
                case ShaderGpuProgramType.DX11VertexSM40:
                case ShaderGpuProgramType.DX11GeometrySM40:
                    return 4;
                case ShaderGpuProgramType.DX11PixelSM50:
                case ShaderGpuProgramType.DX11VertexSM50:
                case ShaderGpuProgramType.DX11GeometrySM50:
                case ShaderGpuProgramType.DX11HullSM50:
                case ShaderGpuProgramType.DX11DomainSM50:
                    return 5;
                default:
                    throw new Exception($"Unexpected program type {prgramType}");
            }
        }
        static List<ConstantBuffer> GetConstantBuffers(ShaderSubProgram shaderSubprogram)
        {
            return shaderSubprogram.ConstantBuffers.ToList();
        }

        static uint GetResourceBindingCount(ShaderSubProgram shaderSubprogram)
        {
            var constantBuffers = GetConstantBuffers(shaderSubprogram);
            return (uint)constantBuffers.Count + (uint)shaderSubprogram.TextureParameters.Count * 2 + (uint)shaderSubprogram.BufferParameters.Count;
        }
        /* ResourceBindingFormat
            * Size n * 8
            * uint     nameOffset  from start
            * uint     Type
            * uint     ReturnType
            * uint     Dimension
            * uint     NumSamples
            * uint     BindPoint
            * uint     BindCount
            * uint     Flags
            */
        static byte[] ResourceBindings(ShaderSubProgram shaderSubprogram, uint resourceOffset, Dictionary<string, uint> nameLookup)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new EndianWriter(memoryStream))
            {
                var constantBuffers = GetConstantBuffers(shaderSubprogram);
                var bindingCount = GetResourceBindingCount(shaderSubprogram);
                const uint bindingHeaderSize = 32;
                uint nameOffset = resourceOffset + bindingHeaderSize * (uint)bindingCount;
                //Build Name Lookup. TODO: Use name indices
                foreach (var bufferParam in shaderSubprogram.BufferParameters)
                {
                    nameLookup[bufferParam.Name] = nameOffset;
                    nameOffset += (uint)bufferParam.Name.Length + 1;
                }
                foreach (var textureParam in shaderSubprogram.TextureParameters)
                {
                    nameLookup[textureParam.Name] = nameOffset;
                    nameOffset += (uint)textureParam.Name.Length + 1;
                }
                foreach (var constantBuffer in constantBuffers)
                {
                    nameLookup[constantBuffer.Name] = nameOffset;
                    nameOffset += (uint)constantBuffer.Name.Length + 1;
                }

                uint bindPoint = 0;
                foreach (var bufferParam in shaderSubprogram.BufferParameters)
                {
                    //Resource bindings
                    //nameOffset
                    writer.Write(nameLookup[bufferParam.Name]);
                    //shader input type
                    writer.Write((uint)ShaderInputType.Structured);
                    //Resource return type
                    writer.Write((uint)ResourceReturnType.Mixed);
                    //Resource view dimension
                    writer.Write((uint)ShaderResourceViewDimension.Buffer);
                    //Number of samples
                    writer.Write((uint)56); //TODO: Check this
                    //Bind point
                    writer.Write(bindPoint);
                    bindPoint += 1;
                    //Bind count
                    writer.Write((uint)1);
                    //Shader input flags
                    writer.Write((uint)ShaderInputFlags.None);
                }
                bindPoint = 0;
                foreach (var textureParam in shaderSubprogram.TextureParameters)
                {
                    //Resource bindings
                    //nameOffset
                    writer.Write(nameLookup[textureParam.Name]);
                    //shader input type
                    writer.Write((uint)ShaderInputType.Sampler);
                    //Resource return type
                    writer.Write((uint)ResourceReturnType.NotApplicable);
                    //Resource view dimension
                    writer.Write((uint)ShaderResourceViewDimension.Unknown);
                    //Number of samples
                    writer.Write((uint)0);
                    //Bind point
                    writer.Write(bindPoint);
                    bindPoint += 1;
                    //Bind count
                    writer.Write((uint)1);
                    //Shader input flags
                    writer.Write((uint)ShaderInputFlags.None);
                }
                bindPoint = 0;
                foreach (var textureParam in shaderSubprogram.TextureParameters)
                {
                    //Resource bindings
                    //nameOffset
                    writer.Write(nameLookup[textureParam.Name]);
                    //shader input type
                    writer.Write((uint)ShaderInputType.Texture);
                    //Resource return type
                    writer.Write((uint)ResourceReturnType.NotApplicable);
                    //Resource view dimension
                    //TODO: look into this
                    var viewDimension = textureParam.Dim == 5 ? ShaderResourceViewDimension.Texture2DArray : ShaderResourceViewDimension.Unknown;
                    writer.Write((uint)viewDimension);
                    //Number of samples
                    writer.Write(uint.MaxValue);
                    //Bind point
                    writer.Write(bindPoint);
                    bindPoint += 1;
                    //Bind count
                    writer.Write((uint)1);
                    //Shader input flags
                    writer.Write((uint)ShaderInputFlags.None);
                }
                bindPoint = 0;
                foreach (var constantBuffer in constantBuffers)
                {
                    //Resource bindings
                    //nameOffset
                    writer.Write(nameLookup[constantBuffer.Name]);
                    //shader input type
                    writer.Write((uint)ShaderInputType.CBuffer);
                    //Resource return type
                    writer.Write((uint)ResourceReturnType.NotApplicable);
                    //Resource view dimension
                    writer.Write((uint)ShaderResourceViewDimension.Unknown);
                    //Number of samples
                    writer.Write((uint)0);
                    //Bind point
                    writer.Write(bindPoint);
                    bindPoint += 1;
                    //Bind count
                    writer.Write((uint)1);
                    //Shader input flags
                    writer.Write((uint)ShaderInputFlags.None);
                }
                foreach (var textureParam in shaderSubprogram.TextureParameters)
                {
                    writer.WriteStringZeroTerm(textureParam.Name);
                }
                foreach (var constantBuffer in constantBuffers)
                {
                    writer.WriteStringZeroTerm(constantBuffer.Name);
                }
                return memoryStream.ToArray();
            }
        }
        static List<Variable> CreateDummyVariables(int id1, int id2, int offset, int sizeToAdd, ShaderGpuProgramType programType)
        {
            var result = new List<Variable>();
            while (sizeToAdd > 0)
            {
                if (sizeToAdd > 16) {
                    result.Add(new Variable($"Unused_{id1}_{id2}", offset, 16, programType));
                    sizeToAdd -= 16;
                    offset += 16;
                    id2++;
                } else
                {
                    result.Add(new Variable($"Unused_{id1}_{id2}", offset, sizeToAdd, programType));
                    sizeToAdd = 0;
                }
            }
            return result;
        }
        static List<Variable> BuildVariables(ConstantBuffer constantBuffer, int contantBufferIndex, ShaderGpuProgramType programType)
        {
            List<Variable> usedVariables = new List<Variable>();
            foreach (var param in constantBuffer.MatrixParams) usedVariables.Add(new Variable(param, programType));
            foreach (var param in constantBuffer.VectorParams) usedVariables.Add(new Variable(param, programType));
            usedVariables = usedVariables.OrderBy(v => v.Index).ToList();
            List<Variable> variables = new List<Variable>();
            uint currentSize = 0;
            for (int i = 0; i < usedVariables.Count; i++)
            {
                var variable = usedVariables[i];
                if (variable.Index > currentSize)
                {
                    var sizeToAdd = variable.Index - currentSize;
                    variables.AddRange(CreateDummyVariables(contantBufferIndex, variables.Count, (int)currentSize, (int)sizeToAdd, programType));
                }
                variables.Add(variable);
                currentSize = (uint)variable.Index + variable.ShaderType.Size();

            }
            if (currentSize < constantBuffer.Size)
            {
                var sizeToAdd = constantBuffer.Size - currentSize;
                variables.AddRange(CreateDummyVariables(contantBufferIndex, variables.Count, (int)currentSize, (int)sizeToAdd, programType));
            }
            return variables;
        }
        /* Variable Format
            * uint nameOffset      location of variable name
            * uint startOffset     offset of variable in memory
            * uint size            size of type in memory
            * uint flags
            * uint typeOffset
            * uint defaultValueOffset
            * if sm >= 5
            * uint StartTexture
            * uint TextureSize
            * uint StartSampler
            * uint SamplerSize
            * 
            * Type Format
            * short VariableClass
            * short VariableType
            * short Rows
            * short Columns
            * short ElementCount
            * short memberCount
            * uint memberOffset
            */
        static byte[] BufferVariables(ConstantBuffer constantBuffer, uint contantBufferOffset, List<Variable> variables, int majorVersion)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new EndianWriter(memoryStream))
            {
                var typeLookup = new Dictionary<ShaderType, uint>();
                uint variableSize = majorVersion >= 5 ? (uint)40 : (uint)24;
                uint variableCount = (uint)variables.Count;
                uint dataOffset = contantBufferOffset + variableCount * variableSize;
                uint startOffset = 0;
                var seenTypes = new HashSet<ShaderType>();
                void WriteVariable(Variable variable)
                {
                    //name offset
                    writer.Write(dataOffset);
                    dataOffset += (uint)variable.Name.Length + 1;
                    //startOffset
                    writer.Write(startOffset);
                    startOffset += variable.ShaderType.Size();
                    //Size
                    writer.Write(variable.ShaderType.Size());
                    //flags
                    writer.Write((uint)ShaderVariableFlags.Used); //Unity only packs used variables as far as I can tell

                    var typeOffset = dataOffset;
                    if (typeLookup.ContainsKey(variable.ShaderType))
                    {
                        typeOffset = typeLookup[variable.ShaderType];
                    }
                    else
                    {
                        typeLookup[variable.ShaderType] = dataOffset;
                        dataOffset += variable.ShaderType.Length();
                    }
                    //type offset
                    writer.Write(typeOffset);
                    //default value offset
                    writer.Write((uint)0); //Not used
                    if(majorVersion >= 5)
                    {
                        //TODO
                        //StartTexture
                        writer.Write((uint)0);
                        //TextureSize
                        writer.Write((uint)0);
                        //StartSampler
                        writer.Write((uint)0);
                        //SamplerSize
                        writer.Write((uint)0);
                    }
                }
                void WriteVariableData(Variable variable)
                {
                    writer.WriteStringZeroTerm(variable.Name);
                    if (!seenTypes.Contains(variable.ShaderType))
                    {
                        seenTypes.Add(variable.ShaderType);
                        writer.Write((ushort)variable.ShaderType.ShaderVariableClass);
                        writer.Write((ushort)variable.ShaderType.ShaderVariableType);
                        writer.Write(variable.ShaderType.Rows);
                        writer.Write(variable.ShaderType.Columns);
                        writer.Write(variable.ShaderType.ElementCount);
                        writer.Write(variable.ShaderType.MemberCount);
                        writer.Write(variable.ShaderType.MemberOffset);
                        if(majorVersion >= 5)
                        {
                            if(variable.ShaderType.parentTypeOffset != 0 ||
                               variable.ShaderType.unknown2 != 0 ||
                               variable.ShaderType.unknown5 != 0 ||
                               variable.ShaderType.parentNameOffset != 0)
                            {
                                throw new Exception("Shader variable type has invalid value");
                            }
                            writer.Write(variable.ShaderType.parentTypeOffset);
                            writer.Write(variable.ShaderType.unknown2);
                            writer.Write(variable.ShaderType.unknown4);
                            writer.Write(variable.ShaderType.unknown5);
                            writer.Write(variable.ShaderType.parentNameOffset);
                        }
                    }
                }
                if (constantBuffer.StructParams != null && constantBuffer.StructParams.Count > 0)
                {
                    throw new Exception("Unexpected Struct Params");
                }
                foreach (var variable in variables)
                {
                    WriteVariable(variable);
                }

                foreach (var variable in variables)
                {
                    WriteVariableData(variable);
                }
                return memoryStream.ToArray();
            }
        }
        /* Header Format
            * ConstantBuffer Header Format
            * uint     nameOffset      from start
            * uint     variableCount
            * uint     variableOffset  from start
            * uint     size
            * uint     Flags
            * uint     BufferType  Always ConstantBuffer
            */
        static byte[] ConstantBuffers(ShaderSubProgram shaderSubprogram, uint contantBufferOffset, Dictionary<string, uint> NameLookup)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new EndianWriter(memoryStream))
            {
                var constantBuffers = GetConstantBuffers(shaderSubprogram);
                uint headerSize = (uint)constantBuffers.Count * 24;
                uint variableOffset = contantBufferOffset + headerSize;
                List<byte[]> VariableDataList = new List<byte[]>();
                int constantBufferIndex = 0;
                foreach (var constantBuffer in constantBuffers)
                {
                    var variables = BuildVariables(constantBuffer, constantBufferIndex++, shaderSubprogram.ProgramType);
                    uint nameOffset = NameLookup[constantBuffer.Name];
                    //name offset
                    writer.Write(nameOffset);
                    //Variable count
                    uint variableCount = (uint)variables.Count;
                    writer.Write(variableCount);
                    //variableOffset
                    writer.Write(variableOffset);
                    //Size
                    writer.Write((uint)constantBuffer.Size);
                    //Flags
                    writer.Write((uint)ConstantBufferFlags.None);
                    //ContantBufferType
                    writer.Write((uint)ConstantBufferType.ConstantBuffer);

                    var variableData = BufferVariables(constantBuffer, variableOffset, variables, GetMajorVersion(shaderSubprogram.ProgramType));
                    VariableDataList.Add(variableData);
                    variableOffset += (uint)variableData.Length;
                }
                foreach (var data in VariableDataList)
                {
                    writer.Write(data);
                }
                return memoryStream.ToArray();
            }
        }
        /* Chunk Format
            * byte[4]  ?
            * uint     ?
            * start:                           
            * uint     constantBufferCount     
            * uint     constantBufferOffset    from start
            * uint     resourceBindingCount    
            * uint     resourceBindingOffset   from start
            * uint     target                  
            * uint     flags                   
            * uint     creatorOffset           
            * A[n]     ResourceBindings        size n*8
            * A[n]     ResourceBindingNames    size variable
            * A[n]     ConstantBuffers         
            * strz     createrName             from start
            */
        static byte[] ResourceChunk(ShaderSubProgram shaderSubprogram, uint chunkoffset)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new EndianWriter(memoryStream))
            {
                Dictionary<string, uint> NameLookup = new Dictionary<string, uint>();
                var majorVersion = GetMajorVersion(shaderSubprogram.ProgramType);
                uint startOffset = 0;
                uint headerOffset = majorVersion >= 5 ? (uint)60 : (uint)28;
                uint resourceBindingOffset = startOffset + headerOffset;
                byte[] resourceBindingData = ResourceBindings(shaderSubprogram, resourceBindingOffset, NameLookup);
                uint contantDataOffset = startOffset + headerOffset + (uint)resourceBindingData.Length;
                byte[] contantBufferData = ConstantBuffers(shaderSubprogram, contantDataOffset, NameLookup);
                uint createrStringOffset = contantDataOffset + (uint)contantBufferData.Length;
                string creatorString = "uTinyRipper";

                writer.Write(Encoding.ASCII.GetBytes("RDEF"));
                //Length of chunk
                uint chunkLength = headerOffset + (uint)resourceBindingData.Length + (uint)contantBufferData.Length + (uint)creatorString.Length + 1;
                writer.Write(chunkLength);
                //ConstantBufferCount
                uint contantBufferCount = (uint)shaderSubprogram.ConstantBuffers.Count;
                writer.Write(contantBufferCount);
                //ContantBufferOffset
                writer.Write(contantDataOffset);
                //ResourceBindingCount
                uint resourceBindingCount = GetResourceBindingCount(shaderSubprogram);
                writer.Write(resourceBindingCount);
                //ResourceBindingOffset
                writer.Write(resourceBindingOffset);
                //MinorVersionNumber
                writer.Write((byte)0);
                //MajorVersionNumber
                writer.Write((byte)majorVersion);
                //ProgramType
                writer.Write((ushort)GetDXProgramType(shaderSubprogram.ProgramType));
                //Flags
                writer.Write((uint)ShaderFlags.NoPreshader);
                //creatorOffset
                writer.Write(createrStringOffset);

                if (majorVersion >= 5)
                {
                    //rd11
                    writer.Write(Encoding.ASCII.GetBytes("RD11"));
                    //unknown1
                    writer.Write((uint)60);
                    //unknown2
                    writer.Write((uint)24);
                    //unknown3
                    writer.Write((uint)32);
                    //unknown4
                    writer.Write((uint)40);
                    //unknown5
                    writer.Write((uint)36);
                    //unknown6
                    writer.Write((uint)12);
                    //InterfaceSlotCount
                    writer.Write((uint)0);
                }
                writer.Write(resourceBindingData);
                writer.Write(contantBufferData);
                //Creatorstring
                writer.WriteStringZeroTerm(creatorString);
                return memoryStream.ToArray();
            }
        }
        public static void FixShaderSubProgram(ShaderSubProgram shaderSubProgram, SerializedSubProgram serializedSubProgram)
        {
            /* Note: NameIndex isn't set on ShaderSubProgram parameters, Name is not set on
                * SerializedSubProgram's ConstantBuffer and parameters
                * Is NameIndex even needed? TODO: delete this later
                */
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var contantBuffers = (ConstantBuffer[])typeof(ShaderSubProgram)
                .GetField("m_constantBuffers", bindingFlags)
                .GetValue(shaderSubProgram);
            for (int i = 0; i < contantBuffers.Length; i++)
            {
                {
                    object boxed = contantBuffers[i];
                    var nameIndex = serializedSubProgram.ConstantBuffers[i].NameIndex;
                    typeof(ConstantBuffer)
                        .GetProperty("NameIndex", bindingFlags)
                        .SetValue(boxed, nameIndex);
                    contantBuffers[i] = (ConstantBuffer)boxed;
                }
                var constantBuffer = contantBuffers[i];
                var matrixParams = (MatrixParameter[])typeof(ConstantBuffer)
                    .GetField("m_matrixParams", bindingFlags)
                    .GetValue(constantBuffer);
                var vectorParams = (VectorParameter[])typeof(ConstantBuffer)
                    .GetField("m_vectorParams", bindingFlags)
                    .GetValue(constantBuffer);
                for (int j = 0; j < matrixParams.Length; j++)
                {

                    var serializedParam = serializedSubProgram.ConstantBuffers[i].MatrixParams[j];
                    object boxed = matrixParams[j];
                    var nameIndex = serializedParam.NameIndex;
                    typeof(MatrixParameter)
                        .GetProperty("NameIndex", bindingFlags)
                        .SetValue(boxed, nameIndex);
                    matrixParams[j] = (MatrixParameter)boxed;
                    var shaderParam = matrixParams[j];
                    if (shaderParam.Name == null) throw new Exception("Name is null");
                    if (shaderParam.Index < 0) throw new Exception("Index is out of bounds");
                    if (shaderParam.NameIndex < 0) throw new Exception("NameIndex is out of bounds");

                }
                for (int j = 0; j < constantBuffer.VectorParams.Count; j++)
                {
                    var serializedParam = serializedSubProgram.ConstantBuffers[i].VectorParams[j];
                    object boxed = vectorParams[j];
                    var nameIndex = serializedParam.NameIndex;
                    typeof(VectorParameter)
                        .GetProperty("NameIndex", bindingFlags)
                        .SetValue(boxed, nameIndex);
                    vectorParams[j] = (VectorParameter)boxed;
                    var shaderParam = vectorParams[j];
                    if (shaderParam.Name == null) throw new Exception("Name is null");
                    if (shaderParam.Index < 0) throw new Exception("Index is out of bounds");
                    if (shaderParam.NameIndex < 0) throw new Exception("NameIndex is out of bounds");
                }
            }
        }
        public static byte[] GetObjectData(Version m_version, GPUPlatform m_graphicApi, ShaderSubProgram shaderSubProgram)
        {
            var shaderData = shaderSubProgram.ProgramData.ToArray();

            int dataOffset = 0;
            if (IsOffset(m_graphicApi))
            {
                dataOffset = IsOffset5(m_version) ? 5 : 6;
                uint fourCC = BitConverter.ToUInt32(shaderData, dataOffset);
                if (fourCC != DXBCFourCC)
                {
                    throw new Exception("Magic number doesn't match");
                }
            }
            int length = shaderData.Length - dataOffset;

            var memoryStream = new MemoryStream(shaderData, dataOffset, length);
            var outStream = new MemoryStream();
            using (var reader = new EndianReader(memoryStream))
            using (var writer = new EndianWriter(outStream))
            {
                var magicBytes = reader.ReadBytes(4);
                var checksum = reader.ReadBytes(16);
                var unknown0 = reader.ReadUInt32();
                var totalSize = reader.ReadUInt32();
                var chunkCount = reader.ReadUInt32();
                var chunkOffsets = new List<uint>();
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkOffsets.Add(reader.ReadUInt32());
                }
                var offset = (uint)memoryStream.Position + 4;
                var resourceChunk = ResourceChunk(shaderSubProgram, offset);
                //Adjust for new chunk
                totalSize += (uint)resourceChunk.Length;
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkOffsets[i] += (uint)resourceChunk.Length + 4;
                }
                chunkOffsets.Insert(0, offset);
                chunkCount += 1;
                totalSize += (uint)resourceChunk.Length;

                writer.Write(magicBytes);
                writer.Write(checksum);
                writer.Write(unknown0);
                writer.Write(totalSize);
                writer.Write(chunkCount);
                foreach (var chunkOffset in chunkOffsets)
                {
                    writer.Write(chunkOffset);
                }
                writer.Write(resourceChunk);
                var rest = reader.ReadBytes((int)memoryStream.Length - (int)memoryStream.Position);
                writer.Write(rest);
                return outStream.ToArray();
            }
        }
    }
}
