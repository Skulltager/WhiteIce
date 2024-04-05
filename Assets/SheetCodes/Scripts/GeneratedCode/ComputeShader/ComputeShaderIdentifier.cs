namespace SheetCodes
{
	//Generated code, do not edit!

	public enum ComputeShaderIdentifier
	{
		[Identifier("None")] None = 0,
		[Identifier("Perlin Noise")] PerlinNoise = 1,
		[Identifier("Chunk Mesh")] ChunkMesh = 2,
		[Identifier("Normal Map")] NormalMap = 3,
	}

	public static class ComputeShaderIdentifierExtension
	{
		public static ComputeShaderRecord GetRecord(this ComputeShaderIdentifier identifier, bool editableRecord = false)
		{
			return ModelManager.ComputeShaderModel.GetRecord(identifier, editableRecord);
		}
	}
}
