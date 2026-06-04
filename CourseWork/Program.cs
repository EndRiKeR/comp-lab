string folderPath = "D:\\myProgects\\repLab\\comp-lab\\CourseWork\\TestData";

string fileName = "CompileMe";
string fileNameSum = "CompileMeSum";

try
{
    var worker = new Worker();
    string fullPath = Path.Combine(folderPath, $"{fileName}.spv");
    string fullPathSum = Path.Combine(folderPath, $"{fileNameSum}.spv");
    
    // Общий шейдер на все, без вывода
    worker.Run(folderPath, fileName);
    worker.Run(folderPath, fileNameSum);
    
    // Шейдер с простой математикой
    Console.WriteLine("Start Vulcan");
    VulkanCompute.Main(fullPath);
    VulkanComputeSum.Main(fullPathSum);
    Console.WriteLine("EndVulcan");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}