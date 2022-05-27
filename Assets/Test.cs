using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private TextAsset matrixAsset;

    private float[] matrix;

    private void Start()
    {
        var text = matrixAsset.text;
        var lines = text.Split('\n');
        var valuesStr = lines.Select(x => x.Split('\t')).ToArray();

        int dimension = valuesStr.Length-1;
        matrix = new float[dimension * dimension];

        for (var l = 0; l < dimension; l++)
        {
            var line = valuesStr[l];
            for (var v = 0; v < line.Length; v++)
            {
                var value = line[v];
                if (float.TryParse(value, out var f))
                {
                    matrix[l * dimension + v] = f;
                }
                // var f = float.Parse(value,CultureInfo.InvariantCulture.NumberFormat);
            }
        }

        var startTime = DateTime.Now;

        var size = new Vector3Int(10, 5, 5);
        var inverter = new HeptaInverse(matrix, new[]
        {
            -size.x * size.y, -size.x,-1,0,1,size.x, size.x*size.y
        }, dimension);
        var res = inverter.Execute();

        Debug.Log((DateTime.Now - startTime).TotalSeconds);

        var str = "";
        for (var i = 0; i < dimension; i++)
        {
            for (var j = 0; j < dimension; j++)
            {
                str += res[i * dimension + j] + "\t";
            }

            str += "\n";
        }

        var path = Path.Combine(Application.dataPath, "Inverse.txt");
        File.Create(path).Close();
        File.WriteAllText(path, str);
    }
}