using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MatrixExtensions
{
    public static float GetVal(this float[] matrix, int stride, int index, int internalIndex = 0)
    {
        return matrix[index * stride + internalIndex];
    }

    public static void SetVal(this float[] matrix, int stride, int index, float value, int internalIndex = 0)
    {
        matrix[index * stride + internalIndex] = value;
    }

    public static float[] Multiply(float[] mA, float[] mB, int stride, (int height,int width) dimensionsA, (int height, int width) dimensionsB)
    {
        if (dimensionsA.width != dimensionsB.height)
            throw new Exception("Dimensions of matrices do not match");

        var computeShader = Resources.Load<ComputeShader>("Assets/Shaders/MatrixMultiplication.compute");

        var mRes = new float[dimensionsA.height * dimensionsB.width * stride];

        var ABuffer = new ComputeBuffer(mA.Length, sizeof(float));
        var BBuffer = new ComputeBuffer(mB.Length, sizeof(float));
        var ResBuffer = new ComputeBuffer(mRes.Length, sizeof(float));

        computeShader.SetBuffer(0, "A", ABuffer);
        computeShader.SetBuffer(0, "B", BBuffer);
        computeShader.SetBuffer(0, "Res", ResBuffer);


        computeShader.SetVector("dimension", new Vector2(dimensionsB.height, dimensionsB.width));
        computeShader.SetInt("stride", stride);

        computeShader.Dispatch(0,dimensionsA.height, dimensionsB.width,1);

        ABuffer.Release();
        BBuffer.Release();
        ResBuffer.GetData(mRes);
        ResBuffer.Release();
        return mRes;
    }
}
