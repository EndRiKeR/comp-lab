namespace comp_lab1;

public class TreeNode
{
    public StateToken Token { get; set; }
    public List<TreeNode> Nodes { get; set; }
    
    public bool IsLeaf => Nodes.Count == 0;

    public TreeNode(StateToken token)
    {
        Token = token;
        Nodes = new List<TreeNode>();
    }
    
    public void PrintTree(int level = 0)
    {
        string indent = new string(' ', level * 4);
        string output = $"{Token.Output}";
        if (Token.IsRepeatable)
            output += "*";
        string tokenInfo = $"{Token.Type} : '{output}'";
        string marker = IsLeaf ? " (лист)" : "";
    
        Console.WriteLine($"{indent}{tokenInfo}{marker}");
    
        foreach (var child in Nodes)
        {
            child.PrintTree(level + 1);
        }
    }

}
