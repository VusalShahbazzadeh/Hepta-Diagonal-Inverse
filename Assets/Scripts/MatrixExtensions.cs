using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MatrixExtensions
{
    public static float GetVal(this float[] matrix, int stride, int index, int internalIndex = 0)
    {
        try
        {
            return matrix[index * stride + internalIndex];
        }
        catch
        {
            Debug.Log(index);
            Debug.Log(stride);
            Debug.Log(internalIndex);
            Debug.Log(index * stride + internalIndex);
            Debug.Log(matrix.Length);
            throw;
        }
    }

    public static void SetVal(this float[] matrix, int stride, int index, float value, int internalIndex = 0)
    {
        matrix[index * stride + internalIndex] = value;
    }

    public static float[] Multiply(float[] m, float b, int stride, (int height, int width) dimensions)
    {
        var computeShader = Resources.Load<ComputeShader>("MatrixMultiplication");
        var mRes = new float[m.Length];

        var mBuffer = new ComputeBuffer(m.Length, sizeof(float));
        var ResBuffer = new ComputeBuffer(mRes.Length, sizeof(float));

        mBuffer.SetData(m);
        ResBuffer.SetData(mRes);

        computeShader.SetBuffer(1, "A", mBuffer);
        computeShader.SetFloat("scalar", b);
        computeShader.SetBuffer(1, "Res", ResBuffer);

        computeShader.SetVector("dimension", new Vector2(dimensions.height, dimensions.width));
        computeShader.SetInt("stride", stride);

        // computeShader.Dispatch(1, dimensions.height, dimensions.width, stride);

        mBuffer.Release();
        ResBuffer.GetData(mRes);
        ResBuffer.Release();
        return mRes;
    }

    public static float[] Multiply(float[] mA, float[] mB, int stride, (int height, int width) dimensionsA,
        (int height, int width) dimensionsB)
    {
        if (dimensionsA.width != dimensionsB.height)
            throw new Exception("Dimensions of matrices do not match");

        var computeShader = Resources.Load<ComputeShader>("MatrixMultiplication");

        var mRes = new float[dimensionsA.height * dimensionsB.width * stride];

        var ABuffer = new ComputeBuffer(mA.Length, sizeof(float));
        var BBuffer = new ComputeBuffer(mB.Length, sizeof(float));
        var ResBuffer = new ComputeBuffer(mRes.Length, sizeof(float));

        computeShader.SetBuffer(0, "A", ABuffer);
        computeShader.SetBuffer(0, "B", BBuffer);
        computeShader.SetBuffer(0, "Res", ResBuffer);


        computeShader.SetVector("dimension", new Vector2(dimensionsB.height, dimensionsB.width));
        computeShader.SetInt("stride", stride);
        computeShader.SetVector("trueDimension", new Vector3(dimensionsA.height, dimensionsB.width, 1));

        // computeShader.Dispatch(0, Mathf.CeilToInt(dimensionsA.height / 32f), Mathf.CeilToInt(dimensionsB.width / 32f), 1);
        
        ResBuffer.GetData(mRes);

        ABuffer.Release();
        BBuffer.Release();
        ResBuffer.Release();
        return mRes;
    }
    
    
    public static float[] MultiplyToDiagonal(float[] mA, float[] mB, int stride, (int height, int width) dimensionsA,
        (int height, int width) dimensionsB)
    {
        if (dimensionsA.width != dimensionsB.height)
            throw new Exception("Dimensions of matrices do not match");

        var computeShader = Resources.Load<ComputeShader>("MatrixMultiplication");

        var mRes = new float[dimensionsA.height * dimensionsB.width * stride];

        var ABuffer = new ComputeBuffer(mA.Length, sizeof(float));
        var BBuffer = new ComputeBuffer(mB.Length, sizeof(float));
        var ResBuffer = new ComputeBuffer(mRes.Length, sizeof(float));

        computeShader.SetBuffer(2, "A", ABuffer);
        computeShader.SetBuffer(2, "B", BBuffer);
        computeShader.SetBuffer(2, "Res", ResBuffer);


        computeShader.SetVector("dimension", new Vector2(dimensionsB.height, dimensionsB.width));
        computeShader.SetInt("stride", stride);
        computeShader.SetVector("trueDimension", new Vector3(dimensionsA.height, dimensionsB.width, 1));
        // computeShader.Dispatch(2, Mathf.CeilToInt(dimensionsA.height / 32f), Mathf.CeilToInt(dimensionsB.width / 32f), 1);
        ResBuffer.GetData(mRes);

        ABuffer.Release();
        BBuffer.Release();
        ResBuffer.Release();
        return mRes;
    }
}