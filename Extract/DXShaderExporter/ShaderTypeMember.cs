using uTinyRipper.Classes.Shaders;

namespace ExtractDXShaderExporter
{
	class ShaderTypeMember
	{
		public string Name;
		public ShaderType ShaderType;
		ShaderGpuProgramType programType;
		public uint Index;
		public ShaderTypeMember(MatrixParameter param, ShaderGpuProgramType programType)
		{
			this.programType = programType;
			Name = param.Name;
			ShaderType = new ShaderType(param, programType);
			Index = (uint)param.Index;
		}
		public ShaderTypeMember(VectorParameter param, ShaderGpuProgramType programType)
		{
			this.programType = programType;
			Name = param.Name;
			ShaderType = new ShaderType(param, programType);
			Index = (uint)param.Index;
		}
	}
}
