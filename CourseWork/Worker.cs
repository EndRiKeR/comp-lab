using Antlr4.Runtime;
using comp_lab.CourseWork._1._AstBuilder;
using comp_lab.CourseWork._3._AstToBinary;
using comp_lab.CourseWork.Common;

public class Worker
{
    public void Run(string folderPath, string fileName)
    {
        try
        {
            Console.WriteLine($"Processing file: {fileName}");
            
            string fullPath = Path.Combine(folderPath, $"{fileName}.glsl");
            string glslCode = File.ReadAllText(fullPath);
            
            // Лексический анализ
            Console.WriteLine("Лексический анализ");
            var inputStream = new AntlrInputStream(glslCode);
            var lexer = new GLSLLexerFull(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            
            // Синтаксический анализ
            Console.WriteLine("Синтаксический анализ");
            var parser = new GLSLParserFull(tokenStream);
            var tree = parser.translation_unit();
            
            // Построение AST
            Console.WriteLine("Build AST");
            var astBuilder = new AstBuilderVisitor();
            var ast = (TranslationUnitNode)astBuilder.Visit(tree);
            
            // Визуализация
            var printer = new AstPrinter();
            string textOutput = printer.PrintText(ast);
            // Console.WriteLine("\n========== AST TEXT REPRESENTATION ==========");
            // Console.WriteLine(textOutput);
            //
            // string textFilePath = Path.Combine(folderPath, $"{fileName}_ast.txt");
            // printer.SaveToFile(textFilePath, textOutput);
            
            string dotFilePath = Path.Combine(folderPath, $"{fileName}_ast.dot");
            string dotOutput = printer.PrintDot(ast);
            printer.SaveToFile(dotFilePath, dotOutput);
            
            // Генерация SPIR‑V
            var spirvGenerator = new SpirVGenerator();
            string spirvPath = Path.Combine(folderPath, $"{fileName}.spv");
            spirvGenerator.GenerateAndSave(ast, "main", spirvPath);
            
            Console.WriteLine($"Done for {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
