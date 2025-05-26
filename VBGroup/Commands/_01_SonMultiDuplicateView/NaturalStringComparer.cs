using System.Collections.Generic;
using System.Text.RegularExpressions;

public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        return NaturalCompare(x, y);
    }

    private static int NaturalCompare(string strA, string strB)
    {
        var regex = new Regex(@"\d+", RegexOptions.Compiled);
        var numA = regex.Matches(strA);
        var numB = regex.Matches(strB);

        int index = 0;
        while (index < numA.Count && index < numB.Count)
        {
            int numberA = int.Parse(numA[index].Value);
            int numberB = int.Parse(numB[index].Value);

            int compareResult = numberA.CompareTo(numberB);
            if (compareResult != 0) return compareResult;

            index++;
        }

        return strA.CompareTo(strB);
    }
}
