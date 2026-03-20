namespace comp_lab1;

public class PostfixConverter
{
    //	Хранит инфиксное выражение
    public string InfixExpr {get; private set; }
    //	Хранит постфиксное выражение
    public string PostfixExpr { get; private set; }

    //	Список и приоритет операторов
    private Dictionary<char, int> operationPriority = new() {
        {'(', 0},
        {'+', 1},
        {'_', 2},
        {'*', 3}
    };

    //	Конструктор класса
    public PostfixConverter(string expression)
    {
        //	Инициализируем поля
        InfixExpr = expression;
        PostfixExpr = ToPostfix(expression + "\r");
    }
    
    private string ToPostfix(string infixExpr)
    {
        //	Выходная строка, содержащая постфиксную запись
        string postfixExpr = "";
        //	Инициализация стека, содержащий операторы в виде символов
        Stack<char> stack = new();

        //	Перебираем строку
        for (int i = 0; i < infixExpr.Length; i++)
        {
            //	Текущий символ
            char c = infixExpr[i];
      
            //	Если симовол - цифра
            if (Char.IsLetter(c))
            {
                //	Парсии его, передав строку и текущую позицию, и заносим в выходную строку
                postfixExpr += GetStringNumber(infixExpr, ref i) + " ";
            }
            //	Если открывающаяся скобка 
            else if (c == '(')
            {
                //	Заносим её в стек
                stack.Push(c);
            }
            //	Если закрывающая скобка
            else if (c == ')')
            {
                //	Заносим в выходную строку из стека всё вплоть до открывающей скобки
                while (stack.Count > 0 && stack.Peek() != '(')
                    postfixExpr += stack.Pop();
                //	Удаляем открывающуюся скобку из стека
                stack.Pop();
            }
            //	Проверяем, содержится ли символ в списке операторов
            else if (operationPriority.ContainsKey(c))
            {
                //	Если да, то сначала проверяем
                char op = c;
                //	Является ли оператор унарным символом
                if (op == '-' && (i == 0 || (i > 1 && operationPriority.ContainsKey( infixExpr[i-1] ))))
                    //	Если да - преобразуем его в тильду
                    op = '~';
				
                //	Заносим в выходную строку все операторы из стека, имеющие более высокий приоритет
                while (stack.Count > 0 && ( operationPriority[stack.Peek()] >= operationPriority[op]))
                    postfixExpr += stack.Pop();
                //	Заносим в стек оператор
                stack.Push(op);
            }
        }
        //	Заносим все оставшиеся операторы из стека в выходную строку
        foreach (char op in stack)
            postfixExpr += op;

        //	Возвращаем выражение в постфиксной записи
        return postfixExpr;
    }
    
    /// <summary>
    /// Парсинг целочисленных значений
    /// </summary>
    /// <param name="expr">Строка для парсинга</param>
    /// <param name="pos">Позиция</param>
    /// <returns>Число в виде строки</returns>
    private string GetStringNumber(string expr, ref int pos)
    {
        //	Хранит число
        string strNumber = "";
  
        //	Перебираем строку
        for (; pos < expr.Length; pos++)
        {
            //	Разбираемый символ строки
            char num = expr[pos];
	
            //	Проверяем, является символ числом
            if (Char.IsLetter(num))
                //	Если да - прибавляем к строке
                strNumber += num;
            else
            {
                //	Если нет, то перемещаем счётчик к предыдущему символу
                pos--;
                //	И выходим из цикла
                break;
            }
        }

        //	Возвращаем число
        return strNumber;
    }
}