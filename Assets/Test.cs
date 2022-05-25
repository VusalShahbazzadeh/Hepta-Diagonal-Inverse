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

        matrix = new float[valuesStr.Length * valuesStr.Length];

        for (var l = 0; l < valuesStr.Length; l++)
        {
            var line = valuesStr[l];
            for (var v = 0; v < line.Length; v++)
            {
                var value = line[v];
                if (float.TryParse(value, out var f))
                {
                    matrix[l * valuesStr.Length + v] = f;
                }
                // var f = float.Parse(value,CultureInfo.InvariantCulture.NumberFormat);
            }
        }

        var startTime = DateTime.Now;

        var inverter = new HeptaInverse(matrix, new[] {-25, -5, -1, 0, 1, 5, 25});
        var res = inverter.Execute();

        Debug.Log((DateTime.Now - startTime).TotalSeconds);

        var str = "";
        for (var i = 0; i < valuesStr.Length; i++)
        {
            for (var j = 0; j < valuesStr.Length; j++)
            {
                str += res[i * valuesStr.Length + j] + "\t";
            }

            str += "\n";
        }

        var path = Path.Combine(Application.dataPath, "Inverse.txt");
        File.Create(path).Close();
        File.WriteAllText(path, str);
    }
}