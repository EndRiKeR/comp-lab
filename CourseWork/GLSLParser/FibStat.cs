namespace comp_lab.CourseWork.GLSLParser;

public static class FibStat
{
    public static uint[] Get(uint n)
    {
        uint[] result = new uint[n];

        if (n > 0) result[0] = 0;
        if (n > 1) result[1] = 1;

        for (uint i = 2; i < n; i++)
        {
            result[i] = result[i - 1] + result[i - 2];
        }
        
        return result;
    }
}