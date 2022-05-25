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
	
	private readonly float[] augmented;
	public readonly float[] determinant;
	private readonly float[] inverse;

	private ComputeShader InverseShader;

	private ComputeBuffer matrixBuffer;
	private ComputeBuffer augmentedBuffer;
	private ComputeBuffer inverseBuffer;
	private ComputeBuffer determinantBuffer;
	
	
	private int populateKernel;
	private int normalizeKernel;
	private int inverseKernel;
	private int splitAugmentedKernel;

	public Inverse(float[] matrix, int dimension, int stride, int maxPower)
	{
	    this.matrix = matrix;
	    this.maxPower = maxPower;
	    this.dimension = dimension;
	    this.stride = stride;

	    inverse = new float[matrix.Length];
	    augmented = new float[matrix.Length * 2];

	    determinant = new float[stride];
	    determinant[maxPower] = 1;
	    
	    SetShaderData(4);
	    PopulateInverse();
	    PopulateAugmented();
	}


	public float[] Execute()
    {
	    for (int i = 0; i < dimension -1; i++)
	    {
		    InverseShader.SetInt("currentRow",i);
		    Normalize(i);
		    InverseRow(i);
	    }
	    
	    Normalize(dimension-1);
	    
	    SplitAugmented();

	    ReadFromShader();

	    return inverse;

    }

    private void ReadFromShader()
    {
	    inverseBuffer.GetData(inverse);
	    determinantBuffer.GetData(determinant);
    }

    private void InverseRow(int currentRow)
    {
	    InverseShader.Dispatch(inverseKernel,dimension - currentRow - 1 , 1, 1); // 
    }

    private void Normalize(int currentRow)
    {
	    InverseShader.Dispatch(normalizeKernel, dimension * 2, 1, 1);
    }

    private void PopulateAugmented()
    {
	    InverseShader.Dispatch(populateKernel, dimension, dimension, stride);
    }
    
    private void SplitAugmented()
    {
	    InverseShader.SetBuffer(splitAugmentedKernel, "inverse", inverseBuffer);
	    InverseShader.Dispatch(splitAugmentedKernel, dimension, dimension, stride);
    }
	
    private void PopulateInverse()
    {
        for(int i =0; i<dimension;i++)
        {
        	int index = i * dimension*2 + i;
        	inverse[index*stride + maxPower] = 1;
        }
    }
    
    private void SetShaderData(int numberOfKernels)
    {
	    InverseShader = Resources.Load<ComputeShader>("Assets/Shaders/Inverse.compute");
	    matrixBuffer = new ComputeBuffer(matrix.Length, sizeof(float));
	    inverseBuffer = new ComputeBuffer(inverse.Length, sizeof(float));
	    augmentedBuffer = new ComputeBuffer(augmented.Length, sizeof(float));
	    determinantBuffer = new ComputeBuffer(determinant.Length, sizeof(float));
	    matrixBuffer.SetData(matrix);
	    inverseBuffer.SetData(inverse);
	    augmentedBuffer.SetData(augmented);
	    determinantBuffer.SetData(determinant);
        
	    for(int i = 0; i< numberOfKernels; i++)
	    {
	        InverseShader.SetBuffer(i, "mat", matrixBuffer);
	        InverseShader.SetBuffer(i, "inverse", inverseBuffer);
	        InverseShader.SetBuffer(i, "augmented", augmentedBuffer);
	        InverseShader.SetBuffer(i, "det", determinantBuffer);
	    }
	    
	    InverseShader.SetInt("dimension",dimension);
	    InverseShader.SetInt("stride", stride);
	    InverseShader.SetInt("maxPower",maxPower);
    }

}
