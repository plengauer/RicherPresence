using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FuzzyEnum
{

    private readonly string[] dictionary;

    private readonly Dictionary<string, double> values;

    public FuzzyEnum(params string[] dictionary)
    {
        this.dictionary = dictionary;
        this.values = new Dictionary<string, double>();
    }

    public void Reset()
    {
        values.Clear();
    }

    public bool IsValid()
    {
        return values.Count > 0;
    }

    public double Parse(string? value)
    {
        if (value == null) return double.MinValue;
        int index = -1;
        double score = double.MinValue;
        for (int i = 0; i < dictionary.Length; i++)
        {
            double s = NeedlemanWunsch.ComputeScore(dictionary[i], value, false);
            if (s < score) continue;
            index = i;
            score = s;
        }
        value = dictionary[index];
        double prevScore = 0;
        values.TryGetValue(value, out prevScore);
        values[value] = prevScore + score;
        return values[value];
    }

    public string? Get()
    {
        string? result = null;
        foreach (string value in values.Keys) if (result == null || values[value] > values[result]) result = value;
        return result;
    }

    public override string ToString()
    {
        return Get() ?? "";
    }

}

