using Antlr4.Runtime;
using comp_lab.CourseWork;
using comp_lab.CourseWork.Visualization;

// Получение пути к файлу
string filePath = "D:\\myProgects\\repLab\\comp-lab\\CourseWork\\TestData\\CompileMe.glsl";

Console.WriteLine($"Processing file: {filePath}");

try
{
    // Чтение GLSL кода из файла
    string glslCode = File.ReadAllText(filePath);
    
    // Лексический анализ
    Console.WriteLine($"Лексический анализ");
    var inputStream = new AntlrInputStream(glslCode);
    var lexer = new GLSLLexerFull(inputStream);
    var tokenStream = new CommonTokenStream(lexer);
    
    // Синтаксический анализ
    Console.WriteLine($"Синтаксический анализ");
    var parser = new GLSLParserFull(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new DiagnosticErrorListener());
    
    var tree = parser.translation_unit();
    
    // Построение AST
    Console.WriteLine($"Build AST");
    var astBuilder = new AstBuilderVisitor();
    var ast = (TranslationUnitNode)astBuilder.Visit(tree);
    
    // Визуализация
    var printer = new AstPrinter();
    
    // 1. Текстовый вывод в консоль
    Console.WriteLine("\n========== AST TEXT REPRESENTATION ==========");
    string textOutput = printer.PrintText(ast);
    Console.WriteLine(textOutput);
    
    // 2. Сохранение текстового представления в файл
    string outputDir = Path.GetDirectoryName(filePath) ?? ".";
    string baseName = Path.GetFileNameWithoutExtension(filePath);
    
    string textFilePath = Path.Combine(outputDir, $"{baseName}_ast.txt");
    printer.SaveToFile(textFilePath, textOutput);
    
    // 3. Сохранение DOT формата для визуализации через Graphviz
    string dotFilePath = Path.Combine(outputDir, $"{baseName}_ast.dot");
    string dotOutput = printer.PrintDot(ast);
    printer.SaveToFile(dotFilePath, dotOutput);
    
    // 4. Сохранение полного вывода в консоль тоже в файл (опционально)
    string consoleFilePath = Path.Combine(outputDir, $"{baseName}_console.txt");
    File.WriteAllText(consoleFilePath, textOutput);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

// Вспомогательный класс для ошибок
public class DiagnosticErrorListener : IAntlrErrorListener<IToken>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, 
                            int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Console.WriteLine($"Syntax error at line {line}:{charPositionInLine} - {msg}");
    }
}