namespace comp_lab1;

public class StateToken
{
    public char Output;
    public TokenType Type;
    public bool IsRepeatable = false;

    public override string ToString()
    {
        if (IsRepeatable)
            return $"{Type}: {Output}*";
        else
            return $"{Type}: {Output}";
    }
}