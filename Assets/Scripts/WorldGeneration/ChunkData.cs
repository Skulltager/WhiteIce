
using System.Runtime.InteropServices;
using UnityEngine;

public class ChunkData
{
    public readonly ComputeBuffer biomeBuffer;
    public readonly ComputeBuffer heightMapBuffer;
    public readonly ComputeBuffer normalMapBuffer;

    public ChunkData(ComputeBuffer biomeBuffer, ComputeBuffer heightMapBuffer)
    {
        this.biomeBuffer = biomeBuffer;
        this.heightMapBuffer = heightMapBuffer;
        normalMapBuffer = new ComputeBuffer(heightMapBuffer.count, Marshal.SizeOf<Vector3>());
    }
}