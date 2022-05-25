using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiagonalInverse
{
    public readonly float[] matrix;
    public readonly int maxPower;
    public readonly int stride;
    public readonly int dimension;
    
    private readonly float[] inverse;

    private ComputeShader DiagonalInverseShader;

    private ComputeBuffer matrixBuffer;
    private ComputeBuffer inverseBuffer;


    public DiagonalInverse(float[] matrix, int dimension, int stride, int maxPower)
    {
        this.matrix = matrix;
        this.dimension = dimension;
        this.stride = stride;
        this.maxPower = maxPower;

        inverse = new float[matrix.Length];

        SetShaderData();
    }

    public float[] Execute()
    {
        PopulateInverse();
        DiagonalInverseShader.Dispatch(0,dimension, 1,1);
        inverseBuffer.GetData(inverse);
        return inverse;
    }

    void PopulateInverse()
    {
        for (var i = 0; i < dimension; i++)
        {
            int index = i * dimension + i;
            inverse[index * stride + maxPower] = 1;
        }
        
        inverseBuffer.SetData(inverse);
    }
    
    private void SetShaderData()
    {
        DiagonalInverseShader = Resources.Load<ComputeShader>("DiagonalInverse");
        
        matrixBuffer = new ComputeBuffer(matrix.Length, sizeof(float));
        inverseBuffer = new ComputeBuffer(inverse.Length, sizeof(float));
        
        matrixBuffer.SetData(matrix);
        inverseBuffer.SetData(inverse);
        
        DiagonalInverseShader.SetBuffer(0, "mat", matrixBuffer);
        DiagonalInverseShader.SetBuffer(0, "inverse", inverseBuffer);
        
        DiagonalInverseShader.SetInt("dimension",dimension);
        DiagonalInverseShader.SetInt("stride", stride);
        DiagonalInverseShader.SetInt("maxPower",maxPower);
    }
}
