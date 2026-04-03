using System.Text;

namespace comp_lab1;

public class PostfixConverter
{
    public string InfixExpr {get; private set; }
    public string PostfixExpr { get; private set; }

    private Dictionary<char, int> operationPriority = new() {
        {'(', 0},
        {'|', 1},
        {'_', 2},
        {'*', 3},
        {'+', 3}
    };

    public PostfixConverter(string expression)
    {
        InfixExpr = expression;
        PostfixExpr = ToPostfix(expression + "\r");
    }

    private string PrepareGrammar(string infixExpression)
    {
        var prepared = new StringBuilder();
        bool needsConcat = false;

        foreach (char ch in infixExpression)
        {
            bool isOperand = char.IsLetterOrDigit(ch);
            bool isUnary = ch == '*' || ch == '+';
            bool isOpenParen = ch == '(';
            bool isCloseParen = ch == ')';
            bool isLowPrecOp = ch == '|';

            if (needsConcat && (isOperand || isOpenParen))
            {
                prepared.Append('_');
            }

            prepared.Append(ch);

            needsConcat = isOperand || isUnary || isCloseParen;

            if (isLowPrecOp)
            {
                needsConcat = false;
            }
        }

        return prepared.ToString();
    }
    
    private string ToPostfix(string expression)
    {
        var infixExpr = PrepareGrammar(expression);
        //Console.WriteLine(infixExpr);
        
        string postfixExpr = "";
        Stack<char> stack = new();
        
        for (int i = 0; i < infixExpr.Length; i++)
        {
            char c = infixExpr[i];

            if (Char.IsLetter(c))
            {
                postfixExpr += GetChar(infixExpr, ref i) + " ";
            }
            else if (c == '(')
            {
                stack.Push(c);
            }
            else if (c == ')')
            {
                while (stack.Count > 0 && stack.Peek() != '(')
                    postfixExpr += stack.Pop();
                stack.Pop();
            }
            else if (operationPriority.ContainsKey(c))
            {
                char op = c;
                
                while (stack.Count > 0 && ( operationPriority[stack.Peek()] >= operationPriority[op]))
                    postfixExpr += stack.Pop();
                stack.Push(op);
            }
        }
        foreach (char op in stack)
            postfixExpr += op;

        return postfixExpr;
    }

    private string GetChar(string expr, ref int pos)
    {
        string strNumber = "";
        
        for (; pos < expr.Length; pos++)
        {
            char num = expr[pos];
            
            if (Char.IsLetter(num))
                strNumber += num;
            else
            {
                pos--;
                break;
            }
        }

        return strNumber;
    }
}

