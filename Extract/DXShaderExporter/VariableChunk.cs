using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uTinyRipper;
using uTinyRipper.Classes.Shaders;

namespace DXShaderExporter
{
    internal class VariableHeader
    {
        internal uint nameOffset;
        internal uint startOffset;
        internal uint typeOffset;
        internal Variable variable;
    }
    internal class VariableChunk
    {
        private ConstantBuffer constantBuffer;
        private int constantBufferIndex;
        private ShaderGpuProgramType programType;
        List<Variable> variables;
        List<VariableHeader> variableHeaders = new List<VariableHeader>();
        Dictionary<string, uint> variableNameLookup = new Dictionary<string, uint>();
        Dictionary<ShaderType, uint> typeLookup = new Dictionary<ShaderType, uint>();
        int majorVersion;
        uint size;
        internal uint Size => size;
        internal uint Count => (uint)variables.Count;

        public VariableChunk(ConstantBuffer constantBuffer, int constantBufferIndex, uint variableOffset, ShaderGpuProgramType programType)
        {
            this.constantBuffer = constantBuffer;
            this.constantBufferIndex = constantBufferIndex;
            this.programType = programType;
            this.variables = BuildVariables();

            majorVersion = DXShaderObjectExporter.GetMajorVersion(programType);
            uint variableSize = majorVersion >= 5 ? (uint)40 : (uint)24;
            uint variableCount = (uint)variables.Count;
            uint dataOffset = variableOffset + variableCount * variableSize;
            uint startOffset = 0;
            foreach (var variable in variables)
            {
                variableNameLookup[variable.Name] = dataOffset;
                var header = new VariableHeader();
                header.nameOffset = dataOffset;
                dataOffset += (uint)variable.Name.Length + 1;
                header.startOffset = startOffset;
                startOffset += variable.ShaderType.Size();
                header.variable = variable;
                if (!typeLookup.ContainsKey(variable.ShaderType))
                {
                    typeLookup[variable.ShaderType] = dataOffset;
                    dataOffset += variable.ShaderType.Length();
                }
                variableHeaders.Add(header);
            }
            size = dataOffset - variableOffset;
        }
        List<Variable> BuildVariables()
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
                    variables.AddRange(CreateDummyVariables(constantBufferIndex, variables.Count, (int)currentSize, (int)sizeToAdd, programType));
                }
                variables.Add(variable);
                currentSize = (uint)variable.Index + variable.ShaderType.Size();

            }
            if (currentSize < constantBuffer.Size)
            {
                var sizeToAdd = constantBuffer.Size - currentSize;
                variables.AddRange(CreateDummyVariables(constantBufferIndex, variables.Count, (int)currentSize, (int)sizeToAdd, programType));
            }
            return variables;
        }
        List<Variable> CreateDummyVariables(int id1, int id2, int offset, int sizeToAdd, ShaderGpuProgramType programType)
        {
            var result = new List<Variable>();
            while (sizeToAdd > 0)
            {
                if (sizeToAdd > 16)
                {
                    result.Add(new Variable($"Unused_{id1}_{id2}", offset, 16, programType));
                    sizeToAdd -= 16;
                    offset += 16;
                    id2++;
                }
                else
                {
                    result.Add(new Variable($"Unused_{id1}_{id2}", offset, sizeToAdd, programType));
                    sizeToAdd = 0;
                }
            }
            return result;
        }
        internal void Write(EndianWriter writer)
        {
            foreach(var header in variableHeaders)
            {
                WriteVariableHeader(writer, header);
            }
            var seenTypes = new HashSet<ShaderType>();
            foreach (var variable in variables)
            {
                writer.WriteStringZeroTerm(variable.Name);
                if (!seenTypes.Contains(variable.ShaderType))
                {
                    WriteVariableData(writer, variable);
                    seenTypes.Add(variable.ShaderType);
                }
            }
        }
        private void WriteVariableHeader(EndianWriter writer, VariableHeader header)
        {
            //name offset
            writer.Write(header.nameOffset);
            //startOffset
            writer.Write(header.startOffset);
            //Size
            writer.Write(header.variable.ShaderType.Size());
            //flags
            writer.Write((uint)ShaderVariableFlags.Used); //Unity only packs used variables as far as I can tell

            var typeOffset = typeLookup[header.variable.ShaderType];
            //type offset
            writer.Write(typeOffset);
            //default value offset
            writer.Write((uint)0); //Not used
            if (majorVersion >= 5)
            {
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
        private void WriteVariableData(EndianWriter writer, Variable variable)
        {
            writer.Write((ushort)variable.ShaderType.ShaderVariableClass);
            writer.Write((ushort)variable.ShaderType.ShaderVariableType);
            writer.Write(variable.ShaderType.Rows);
            writer.Write(variable.ShaderType.Columns);
            writer.Write(variable.ShaderType.ElementCount);
            writer.Write(variable.ShaderType.MemberCount);
            writer.Write(variable.ShaderType.MemberOffset);
            if (majorVersion >= 5)
            {
                if (variable.ShaderType.parentTypeOffset != 0 ||
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
}
