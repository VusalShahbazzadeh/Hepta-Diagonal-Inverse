using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LTInverse
{
    public readonly float[] matrix;
    public readonly int stride;
    public readonly int dimension;
    public readonly int[] diagonalDistances;
    private readonly int maxPower;
    public readonly float[] augmented;
    public readonly float[] inverse;


    private ComputeShader ltInverse;
    private ComputeBuffer matrixBuffer;
    private ComputeBuffer augmentedBuffer;
    private int populateKernel;
    private int normalizeKernel;
    private int inverseKernel;
    private int splitAugmentedKernel;
    private ComputeBuffer inverseBuffer;

    public LTInverse(float[] lTMatrix, int stride, int dimension, int[] diagonalDistances, int maxPower)
    {
        matrix = lTMatrix;
        this.stride = stride;
        this.dimension = dimension;
        this.diagonalDistances = diagonalDistances;
        augmented = new float[lTMatrix.Length * 2];
        inverse = new float[lTMatrix.Length];
        this.maxPower = maxPower;
        ltInverse = Resources.Load<ComputeShader>("Assets/Shaders/LTInverse.compute");

        matrixBuffer = new ComputeBuffer(matrix.Length, sizeof(float));
        augmentedBuffer = new ComputeBuffer(augmented.Length, sizeof(float));
        inverseBuffer = new ComputeBuffer(inverse.Length, sizeof(float));
        
        PopulateAugmentedInverse();
        matrixBuffer.SetData(matrix);
        augmentedBuffer.SetData(augmented);
        inverseBuffer.SetData(inverse);
        SetDataToShader(4);
        PopulateAugmented();
    }

    public float[] Execute()
    {
        for (var i = 0; i < dimension - 1; i++)
        {
            ltInverse.SetInt("currentRow", i);
            Normalize(i);
            Inverse(i);
        }

        Normalize(dimension - 1);

        SplitAugmented();

        ReadFromShader();
        return inverse;
    }

    private void Inverse(int currentRow)
    {
        ltInverse.Dispatch(inverseKernel, 7, 1, 1); // dimension - currentRow - 1
    }

    private void Normalize(int currentRow)
    {
        ltInverse.Dispatch(normalizeKernel, dimension * 2, 1, 1);
    }

    private void PopulateAugmented()
    {
        ltInverse.Dispatch(populateKernel, 7, dimension, stride);
    }

    private void PopulateAugmentedInverse()
    {
	for (int i = 0; i < dimension; i++)
	{
	    int index = i * dimension * 2 + i;
	    augmented[index * stride + maxPower] = 1;
	}
    }

    private void SplitAugmented()
    {
        ltInverse.SetBuffer(splitAugmentedKernel, "inverse", inverseBuffer);
        ltInverse.Dispatch(splitAugmentedKernel, dimension, dimension, stride);
    }

    private void ReadFromShader()
    {
        inverseBuffer.GetData(inverse);
    }

    private void SetDataToShader(int numberOfKernels)
    {
        for (var i = 0; i < numberOfKernels; i++)
        {
            ltInverse.SetBuffer(i, "mat", matrixBuffer);
            ltInverse.SetBuffer(i, "augmented", augmentedBuffer);

        }

        ltInverse.SetInt("currentRow", 0);
        ltInverse.SetInt("dimension", dimension);
        ltInverse.SetInt("stride",stride);
        ltInverse.SetInt("maxPower", maxPower);
        ltInverse.SetInts("diagonalDistances", diagonalDistances);
        normalizeKernel = ltInverse.FindKernel("Normalize");
        populateKernel = ltInverse.FindKernel("PopulateAugmented");
        inverseKernel = ltInverse.FindKernel("Inverse");
        inverseKernel = ltInverse.FindKernel("SplitAugmented");
    }
}