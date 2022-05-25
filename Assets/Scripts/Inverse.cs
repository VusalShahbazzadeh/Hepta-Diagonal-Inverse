using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inverse
{
	public readonly float[] matrix;
	public readonly int maxPower;
	public readonly int stride;
	public readonly int dimension;

	private readonly float[] inverse;

	private ComputeShader InverseShader;

	private ComputeBuffer matrixBuffer;
	private ComputeBuffer inverseBuffer;

	public Inverse(float[] matrix, int dimension, int stride, int maxPower)
	{
	    this.matrix = matrix;
	    this.maxPower = maxPower;
	    this.dimension = dimension;
	    this.stride = stride;
	   
	    SetShaderData(3);
	}


    private void Execute()
    {
		PopulateInverse();
		
	
    }
	
    private void PopulateInverse()
    {
        for(int i =0; i<dimension;i++)
        {
        	int index = i * dimension + i;
        	inverse[index*stride + maxPower] = 1;
        }
    }
    
    private void SetShaderData(int numberOfKernels)
    {
	    InverseShader = Resources.Load<ComputeShader>("Assets/Shaders/Inverse.compute");
	    matrixBuffer = new ComputeBuffer(matrix.Length, sizeof(float));
	    inverseBuffer = new ComputeBuffer(inverse.Length, sizeof(float));
	    matrixBuffer.SetData(matrix);
	    inverseBuffer.SetData(inverse);
        
	    for(int i = 0; i< numberOfKernels; i++)
	    {
	        InverseShader.SetBuffer(i, "mat", matrixBuffer);
	        InverseShader.SetBuffer(i, "inverse", inverseBuffer);
	    }
    }

}
