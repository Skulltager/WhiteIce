
using UnityEngine;

public class WorldChunkData
{
    public readonly ComputeBuffer heightMapBuffer;
    public readonly ComputeBuffer biomeMapBuffer;
    public readonly Mesh[] mipMapMeshes;

    public WorldChunkData(ComputeBuffer heightMapBuffer, ComputeBuffer biomeMapBuffer, int mipMapCount)
    {
        this.heightMapBuffer = heightMapBuffer;
        this.biomeMapBuffer = biomeMapBuffer;
        mipMapMeshes = new Mesh[mipMapCount];
    }

    public Mesh GetMipMapMesh(int mipMapLevel)
    {
        if(mipMapMeshes[mipMapLevel] == null)
        {

        }

        return mipMapMeshes[mipMapLevel];
    }
}