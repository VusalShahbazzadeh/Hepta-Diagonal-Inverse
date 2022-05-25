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
}
