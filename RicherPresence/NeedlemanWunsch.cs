//#define LOG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NeedlemanWunsch
{

    public delegate double w(char c1, char c2);

    public static double ComputeScore(string s1, string s2, bool freeshift)
    {
        return ComputeScore(s1, s2, freeshift, -1, (c1, c2) => c1 == c2 ? 1 : -1);
    }

    public static double ComputeScore(string s1, string s2, bool freeshift, double gap, w w)
    {
        double[,] matrix = new double[1 + s1.Length, 1 + s2.Length];
        matrix[0, 0] = 0;
        for (int i = 1; i < 1 + s1.Length; i++) matrix[i, 0] = freeshift ? 0 : matrix[i - 1, 0] + gap;
        for (int j = 1; j < 1 + s2.Length; j++) matrix[0, j] = freeshift ? 0 : matrix[0, j - 1] + gap;
        for (int i = 1; i < 1 + s1.Length; i++)
        {
            for (int j = 1; j < 1 + s2.Length; j++)
            {
                matrix[i, j] = Math.Max(
                    matrix[i - 1, j - 1] + w(s1[i - 1], s2[j - 1]),
                    freeshift && ((i == 0 && j == s2.Length) || (i == s1.Length && j == 0)) ? int.MinValue : Math.Max(
                        matrix[i - 1, j] + (freeshift && j == s2.Length ? 0 : gap),
                        matrix[i, j - 1] + (freeshift && i == s1.Length ? 0 : gap)
                    )
                );
            }
        }
#if LOG
        Console.WriteLine("\"" + s1 + "\" = \"" + s2 + "\"");
        Console.Write("  ");
        for (int j = 0; j < 1 + s2.Length; j++) Console.Write("      " + (j == 0 ? "- " : ("" + s2[j - 1] + " ")));
        Console.WriteLine();
        for (int i = 0; i < 1 + s1.Length; i++)
        {
            Console.Write(i == 0 ? "- " : ("" + s1[i-1] + " "));
            for (int j = 0; j < 1 + s2.Length; j++)
            {
                string str = String.Format("{0:F2} ", matrix[i, j]);
                bool minus = str[0] == '-';
                if (minus) str = str.Substring(1);
                while (str.Length < 7) str = "0" + str;
                str = (minus ? "-" : "+") + str;
                Console.Write(str);
            }
            Console.WriteLine();
        }
#endif
        return matrix[s1.Length, s2.Length];
    }


    public delegate double s(char c1, char c2);

    public static double ComputeRelativeScore(string s1, string s2, bool freeshift, s s)
    {
        double score = ComputeScore(s1, s2, freeshift, -1, (c1, c2) => c1 == c2 ? 1 : (s(c1, c2) - 1));
        double worst = freeshift ? -Math.Min(s1.Length, s2.Length) : -(s1.Length + s2.Length);
        double best = Math.Min(s1.Length, s2.Length);
        return (-worst + score) / (-worst + best);
    }

}
