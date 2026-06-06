string folderPath = "C:\\Work\\Repos\\comp-lab1\\CourseWork\\TestData";

string fileName = "CompileMe";
string fileNameSum = "CompileMeSum";
string fileNameFib = "CompileMeFib";

try
{
    var worker = new Worker();
    // string fullPath = Path.Combine(folderPath, $"{fileName}.spv");
    // string fullPathSum = Path.Combine(folderPath, $"{fileNameSum}.spv");
    string fullPathFib = Path.Combine(folderPath, $"{fileNameFib}.spv");
    
    // worker.Run(folderPath, fileName);
    // worker.Run(folderPath, fileNameSum);
    worker.Run(folderPath, fileNameFib);
    
    Console.WriteLine("Start Vulcan");
    // VulkanCompute.Main(fullPath);
    // VulkanComputeSum.Main(fullPathSum);
    VulkanComputeFibonacci.Main(fullPathFib);
    Console.WriteLine("EndVulcan");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}