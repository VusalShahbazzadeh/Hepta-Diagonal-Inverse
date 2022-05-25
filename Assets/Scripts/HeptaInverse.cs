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
    private float[] BInverse;
    private float[] determinant;
    private float[] D;
    private float[] X;
    private float[] Y;
    private float[] Z;
    private float[] inverse;

    private ComputeShader HInverse;
    private ComputeShader LastMOfH;


    private int GetAKernel;
    private int GetBKernel;
    private int GetDKernel;
    private int GetYKernel;
    private int GetZKernel;
    private int SetLastColumnsKernel;
    private int FinalizeInverseKernel;
    private ComputeBuffer matrixBuffer;
    private ComputeBuffer extendedInverseBuffer;
    private ComputeBuffer ABuffer;
    private ComputeBuffer BBuffer;
    private ComputeBuffer BInverseBuffer;
    private ComputeBuffer determinantBuffer;
    private ComputeBuffer DBuffer;
    private ComputeBuffer XBuffer;
    private ComputeBuffer YBuffer;
    private ComputeBuffer ZBuffer;
    private ComputeBuffer InverseBuffer;

    public HeptaInverse(float[] matrix, int[] diagonalDistances)
    {
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

    public float[] Execute()
    {
        PopulateExtendedMatrix();

        InverseExtendedMatrix();
        
        GetA();
        GetB();
        GetD();
        GetX();
        GetY();
        GetZ();
        LastMColumns();

        FinalizeInverse();

        return inverse;
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
        var inverter = new Inverse(B,diagonalDistances[6], stride, maxPower);
        BInverse = inverter.Execute();
        BInverseBuffer.SetData(BInverse);
        determinant = inverter.determinant;
        determinantBuffer.SetData(determinant);
        
        HInverse.Dispatch(GetDKernel, diagonalDistances[6], diagonalDistances[6],1);

    }

    private void GetX()
    {
        ABuffer.GetData(A);
        DBuffer.GetData(D);

        X = MatrixExtensions.Multiply(A, D, stride, (dimension + diagonalDistances[6], diagonalDistances[6]), (diagonalDistances[6], diagonalDistances[6]));
    }

    private void GetY()
    {
        LastMOfH.Dispatch(GetYKernel, dimension, diagonalDistances[6],stride);
    }
    private void GetZ()
    {
        LastMOfH.Dispatch(GetZKernel, diagonalDistances[6], diagonalDistances[6],stride);
    }

    private void LastMColumns()
    {
        YBuffer.GetData(Y);
        ZBuffer.GetData(Z);
        var dInverter = new DiagonalInverse(Z, diagonalDistances[6], stride, maxPower);
        var zInverse = dInverter.Execute();
       
        var lastMColumns = MatrixExtensions.Multiply(Y, zInverse, stride, (dimension, diagonalDistances[6]),
            (diagonalDistances[6], diagonalDistances[6]));

        lastMColumns = MatrixExtensions.Multiply(lastMColumns, -1, stride, (dimension, diagonalDistances[6]));


        var lastMColumnsBuffer = new ComputeBuffer(lastMColumns.Length, sizeof(float));
        lastMColumnsBuffer.SetData(lastMColumns);
        LastMOfH.SetBuffer(SetLastColumnsKernel,"LastMofH",lastMColumnsBuffer);
        LastMOfH.Dispatch(SetLastColumnsKernel,dimension, diagonalDistances[6],stride);
        
        lastMColumnsBuffer.Release();
    }

    private void FinalizeInverse()
    {
        for (var i = dimension - diagonalDistances[6]; i >= 0; i--)
        {
            LastMOfH.SetInt("currentColumn", i);
            LastMOfH.Dispatch(FinalizeInverseKernel, dimension,1,1);
        }
        
        InverseBuffer.GetData(inverse);
    }



    #region Shader


    private void SetShaderData()
    {
        HInverse = Resources.Load<ComputeShader>("Assets/Shaders/HeptaInverse.compute");
        LastMOfH = Resources.Load<ComputeShader>("Assets/Shaders/LastMofH.compute");

        if (HInverse is null)
        {
            throw new Exception("Shader for inversion is missing");
        }
        A = new float[(dimension + diagonalDistances[6]) * diagonalDistances[6] * stride];
        B = new float[diagonalDistances[6] * diagonalDistances[6] * stride];
        D = new float[diagonalDistances[6] * diagonalDistances[6] * stride];
        X = new float[(dimension + diagonalDistances[6]) * diagonalDistances[6] * stride];
        Y = new float[dimension * diagonalDistances[6] * stride];
        Z = new float[diagonalDistances[6] * diagonalDistances[6] * stride];
        inverse = new float[dimension * dimension * stride];
        GetAKernel = HInverse.FindKernel("GetA");
        GetBKernel = HInverse.FindKernel("GetB");
        GetDKernel = HInverse.FindKernel("GetD");
        GetYKernel = LastMOfH.FindKernel("GetY");
        GetZKernel = LastMOfH.FindKernel("GetZ");
        SetLastColumnsKernel = LastMOfH.FindKernel("SetLastColumns");
        FinalizeInverseKernel = LastMOfH.FindKernel("FinalizeInverse");

        HInverse.SetInt("stride", stride);
        HInverse.SetInt("dimension", dimension);
        HInverse.SetInt("maxPower",maxPower);
        HInverse.SetInts("diagonalDistances", diagonalDistances);
        
        LastMOfH.SetInt("stride", stride);
        LastMOfH.SetVector("dimension", new Vector4(diagonalDistances[6], dimension));
        LastMOfH.SetInt("maxPower",maxPower);
        LastMOfH.SetInts("diagonalDistances", diagonalDistances);

        matrixBuffer = new ComputeBuffer(matrix.Length, sizeof(float));
        extendedInverseBuffer = new ComputeBuffer(extendedInverse.Length, sizeof(float));
        ABuffer = new ComputeBuffer(A.Length, sizeof(float));
        BBuffer = new ComputeBuffer(B.Length, sizeof(float));
        BInverseBuffer = new ComputeBuffer(BInverse.Length, sizeof(float));
        DBuffer = new ComputeBuffer(D.Length, sizeof(float));
        determinantBuffer = new ComputeBuffer(stride, sizeof(float));
        XBuffer = new ComputeBuffer(X.Length, sizeof(float));
        YBuffer = new ComputeBuffer(Y.Length, sizeof(float));
        ZBuffer = new ComputeBuffer(Z.Length, sizeof(float));
        InverseBuffer = new ComputeBuffer(inverse.Length, sizeof(float));
        
        
        matrixBuffer.SetData(matrix);
        extendedInverseBuffer.SetData(extendedInverse);
        ABuffer.SetData(A);
        BBuffer.SetData(B);
        BInverseBuffer.SetData(BInverse);
        determinantBuffer.SetData(determinant);
        DBuffer.SetData(D);
        XBuffer.SetData(X);
        YBuffer.SetData(Y);
        ZBuffer.SetData(Z);
        InverseBuffer.SetData(inverse);
        
        
        HInverse.SetBuffer(GetAKernel, "extendedInverse", extendedInverseBuffer);
        HInverse.SetBuffer(GetAKernel, "A", ABuffer);
        HInverse.SetBuffer(GetBKernel, "A", ABuffer);
        HInverse.SetBuffer(GetBKernel, "B", BBuffer);
        HInverse.SetBuffer(GetDKernel, "BInverse", BInverseBuffer);
        HInverse.SetBuffer(GetDKernel, "det", determinantBuffer);
        HInverse.SetBuffer(GetDKernel, "D", DBuffer);
        
        LastMOfH.SetBuffer(GetYKernel, "X", XBuffer);
        LastMOfH.SetBuffer(GetYKernel, "Y", YBuffer);
        
        LastMOfH.SetBuffer(GetZKernel, "X", XBuffer);
        LastMOfH.SetBuffer(GetZKernel, "Z", ZBuffer);
        
        LastMOfH.SetBuffer(SetLastColumnsKernel, "HInverse", InverseBuffer);
        LastMOfH.SetBuffer(SetLastColumnsKernel, "H", matrixBuffer);
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