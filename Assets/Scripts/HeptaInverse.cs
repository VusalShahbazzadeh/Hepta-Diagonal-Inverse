using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeptaInverse
{
    public readonly int maxPower;
    public readonly int stride;
    public readonly int dimension;
    public readonly int[] diagonalDistances;
    public float[] matrix;


    private float[] extendedMatrix;
    private float[] extendedInverse;
    private float[] A;
    private float[] B;

    private ComputeShader HInverse;


    private int GetAKernel;
    private int GetBKernel;
    private ComputeBuffer extendedInverseBuffer;
    private ComputeBuffer ABuffer;
    private ComputeBuffer BBuffer;

    public HeptaInverse(float[] matrix, int[] diagonalDistances)
    {
        if (HInverse is null)
        {
            throw new Exception("Shader for inversion is missing");
        }
        if (diagonalDistances.Length != 7)
            throw new Exception("7 Diagonal distances required for hepta-diagonal matrix");

        var dimensionFloat = Mathf.Sqrt(matrix.Length);
        dimension = (int) dimensionFloat;
        if (dimensionFloat != dimension)
            throw new Exception("Matrix should be square");

        maxPower = NumberOfZeros(matrix, 1, diagonalDistances[6]);
        this.diagonalDistances = diagonalDistances;
        stride = maxPower * 2 + 1;

        PopulateMatrix(matrix);

        SetShaderData();
    }

    public void Execute()
    {
        PopulateExtendedMatrix();

        InverseExtendedMatrix();
        
        GetA();
    }

    private void InverseExtendedMatrix()
    {
        int[] extendedDiagonalDistances = new int[7];

        for (var i = 0; i < 7; i++)
        {
            extendedDiagonalDistances[i] = diagonalDistances[i] - diagonalDistances[6];
        }

        var ltInverse = new LTInverse(extendedMatrix, stride, dimension + diagonalDistances[6],
            extendedDiagonalDistances, maxPower);
        extendedInverse = ltInverse.Execute();

        GetA();
        GetB();
        GetD();
    }

    private void GetA()
    {
        HInverse.Dispatch(GetAKernel,dimension + diagonalDistances[6], diagonalDistances[6],stride);
    }

    private void GetB()
    {
        HInverse.Dispatch(GetBKernel, diagonalDistances[6], diagonalDistances[6],stride);
    }

    private void GetD()
    {
        HInverse.Dispatch(GetDKernel, diagonalDistances[6], diagonalDistances[6],1);
    }

    #region Shader


    private void SetShaderData()
    {
        HInverse = Resources.Load<ComputeShader>("Assets/Shaders/HeptaInverse.compute");

        A = new float[(dimension + diagonalDistances[6]) * diagonalDistances[6] * stride];
        B = new float[diagonalDistances[6] * diagonalDistances[6] * stride];
        GetAKernel = HInverse.FindKernel("GetA");
        GetBKernel = HInverse.FindKernel("GetB");

        HInverse.SetInt("stride", stride);
        HInverse.SetInt("dimension", dimension);
        HInverse.SetInt("maxPower",maxPower);
        HInverse.SetInts("diagonalDistances", diagonalDistances);

        extendedInverseBuffer = new ComputeBuffer(extendedInverse.Length, stride);
        ABuffer = new ComputeBuffer(A.Length, stride);
        BBuffer = new ComputeBuffer(B.Length, stride);
        HInverse.SetBuffer(GetAKernel, "extendedInverse", extendedInverseBuffer);
        HInverse.SetBuffer(GetAKernel, "A", ABuffer);
        HInverse.SetBuffer(GetBKernel, "A", ABuffer);
        HInverse.SetBuffer(GetBKernel, "B", BBuffer);
    }

    #endregion


    #region ReadWriteToMatrix

    private void PopulateExtendedMatrix()
    {
        int m = diagonalDistances[6];
        int extendedDimension = dimension + m;
        extendedMatrix = new float[extendedDimension * extendedDimension * stride];

        for (var i = 0; i < dimension; i++)
        {
            for (int j = 0; j < dimension; j++)
            {
                var val = matrix.GetVal(stride, i * dimension + j, maxPower);
                var valT = matrix.GetVal(stride, i * dimension + j, maxPower+1);
                
                extendedMatrix.SetVal(stride, (i + m) * extendedDimension + j, val, maxPower);
                extendedMatrix.SetVal(stride, (i + m) * extendedDimension + j, valT, maxPower+1);
            }
        }

        for (int i = 0; i < m; i++)
        {
            extendedMatrix.SetVal(stride, i * dimension + i, 1, maxPower);
            extendedMatrix.SetVal(stride, (extendedDimension - i) * extendedDimension + extendedDimension - i, 1,
                maxPower);
        }
    }

    private void PopulateMatrix(float[] source)
    {
        matrix = new float[source.Length * stride];

        for (var i = 0; i < source.Length; i++)
        {
            var val = source.GetVal(1, i);

            if (val == 0)
                matrix.SetValue(stride, i, 1, maxPower+1);
            else
                matrix.SetVal(stride, i, val, maxPower);
        }
    }

    private int NumberOfZeros(float[] matrix, int stride, int diagonalDistance)
    {
        int res = 0;
        for (int i = 0; i < dimension - diagonalDistance; i++)
        {
            var val = matrix.GetVal(1, i * dimension + i + diagonalDistance);
            if (val == 0)
            {
                res++;
            }
        }

        return res;
    }

    #endregion
}