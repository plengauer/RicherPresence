using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class FuzzyStringOperations
{

    public static bool FuzzyEquals(this string s1, string s2, double similarity)
    {
        return s1.FuzzyEquals(s2) >= similarity;
    }

    public static double FuzzyEquals(this string s1, string s2)
    {
        return s1 == s2 ? 1 : NeedlemanWunsch.ComputeRelativeScore(s1, s2, false, Similarity);
    }

    public static bool FuzzyContains(this string haystack, string needle, double similarity)
    {
        return FuzzyContains(haystack, needle) >= similarity;
    }

    public static double FuzzyContains(this string haystack, string needle)
    {
        return haystack.Contains(needle) ? 1 : NeedlemanWunsch.ComputeRelativeScore(haystack, needle, true, Similarity);
    }

    public static (int index, int length) FuzzyIndexOf(this string haystack, string needle, double similarity)
    {
        if (needle.Length == 0) return (0, 0);
        if (haystack.Contains(needle)) return (haystack.IndexOf(needle), needle.Length);
        if (haystack.FuzzyContains(needle) < similarity) return (-1, 0);

        // could we accelerate this by binary searching to a certain degree with fuzzy contains?
        // could we accelerate this by getting the score matrix directly, searching the max value, and then starting the procedure in that area? (in both loops, similarties will only have one peek i hope)

        double score = double.MinValue;
        int index = -1, length = 0;
        for (int from = 0; from < haystack.Length; from++)
        {
            double sf = double.MinValue;
            int lf = 0;
            for (int l = 1; l < Math.Min(haystack.Length - from, needle.Length * 2); l++)
            {
                double sl = haystack.Substring(from, l).FuzzyEquals(needle);
                if (sl < sf) continue;
                sf = sl;
                lf = l;
            }
            if (sf < score) continue;
            score = sf;
            index = from;
            length = lf;
        }
        return score >= similarity ? (index, length) : (-1, 0);
    }

    private static double Similarity(char c1, char c2)
    {
        if (are(c1, c2, 'I', 'l')) return 0.8;
        if (are(c1, c2, 'Z', '2')) return 0.8;
        if (are(c1, c2, '7', 'T')) return 0.8;
        if (are(c1, c2, 'c', 'C')) return 0.9;
        if (are(c1, c2, 'p', 'P')) return 0.9;
        if (are(c1, c2, 'v', 'y')) return 0.9;
        if (are(c1, c2, 'v', 'V')) return 1;
        if (are(c1, c2, 'y', 'Y')) return 1;
        if (are(c1, c2, 'x', 'X')) return 1;
        if (are(c1, c2, 'o', 'O')) return 1;
        if (are(c1, c2, '0', 'o')) return 1;
        if (are(c1, c2, '0', 'O')) return 1;
        if (are(c1, c2, '1', 'l')) return 1;
        if (are(c1, c2, '1', 'I')) return 1;
        if (are(c1, c2, '5', 's')) return 0.9;
        if (are(c1, c2, '5', 'S')) return 1;
        if (are(c1, c2, '6', 'b')) return 1;
        if (are(c1, c2, '8', 'B')) return 1;
        if (are(c1, c2, '.', ':')) return 0.9;
        if (are(c1, c2, 'a', 'ä')) return 0.9;
        if (are(c1, c2, 'o', 'ö')) return 0.9;
        if (are(c1, c2, 'u', 'ü')) return 0.9;
        if (are(c1, c2, 'A', 'Ä')) return 0.9;
        if (are(c1, c2, 'O', 'Ö')) return 0.9;
        if (are(c1, c2, 'U', 'Ü')) return 0.9;
        else return 0;
    }

    private static bool are(char c1, char c2, char c3, char c4)
    {
        return (c1 == c3 && c2 == c4) || (c1 == c4 && c2 == c3);
    }

    public static string Capitalize(this string str)
    {
        char[] chars = str.ToCharArray();
        for (int i = 0; i < str.Length; i++)
        {
            if (i == 0 || char.IsWhiteSpace(chars[i - 1]))
            {
                chars[i] = ("" + chars[i]).ToUpper()[0];
            }
        }
        return new string(chars);
    }

}
