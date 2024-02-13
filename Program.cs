using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO.Compression;
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");
//options
var bundleOption = new Option<FileInfo>("--output", "File path and name")
{
    Name = "output"
};
var languageOption = new Option<string>("--language", "Programming language to include in the bundle")
{
    IsRequired = true
};
var noteOption = new Option<bool>("--note", "Include source code note in the bundle");
var sortOption = new Option<string>("--sort", "Sort the code files in the bundle")
{
    Name = "sort"
};
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from the code");
var creatorOption = new Option<string>("--creator", "Author/Creator of the bundle")
{
    Name = "creator"
};
bundleCommand.AddOption(creatorOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(languageOption);
//aliases
bundleOption.AddAlias("-o");
languageOption.AddAlias("-l");
noteOption.AddAlias("-n");
sortOption.AddAlias("-s");
creatorOption.AddAlias("-c");
removeEmptyLinesOption.AddAlias("-r");
noteOption.SetDefaultValue(false);
removeEmptyLinesOption.SetDefaultValue(false);
static string RemoveEmptyLines(string code)
{
    string[] lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    List<string> nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    return string.Join(Environment.NewLine, nonEmptyLines);
}
createRspCommand.SetHandler(() =>
{
    var responseFile = new FileInfo("responseFile.rsp");
    try
    {
        using (StreamWriter rspWriter = new StreamWriter(responseFile.FullName))
        {
            Console.WriteLine("Enter File output name");
            string output;
            do
            {
                output = Console.ReadLine();
            } while (String.IsNullOrEmpty(output));
            rspWriter.Write($"--output {output} ");
            Console.WriteLine("Enter programming language or to include every language enter all");
            string lang;
            do
            {
                lang = Console.ReadLine();
            } while (String.IsNullOrEmpty(lang));
            rspWriter.Write($"--language {lang} ");

            Console.WriteLine("Include source code origin as a comment? (y/n)");
            var noteInput = Console.ReadLine();
            rspWriter.Write(noteInput.Trim().ToLower() == "y" ? "--note " : "");

            Console.WriteLine("Enter the sort order for code files ('name' or 'type'): ");
            rspWriter.Write($"--sort {Console.ReadLine()} ");

            Console.WriteLine("Remove empty lines from code files? (y/n)");
            var removeEmptyLinesInput = Console.ReadLine();
            rspWriter.Write(removeEmptyLinesInput.Trim().ToLower() == "y" ? "--remove-empty-lines " : "");

            Console.WriteLine("Enter the creator`s name: ");
            rspWriter.Write($"--creator {Console.ReadLine()}");
        }
        Console.WriteLine($"Response file created successfully: {responseFile.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating response file: {ex.Message}");
    }
});
//הפונקציה שאני יריץ ואני יראה את הפקודה 
bundleCommand.SetHandler((language, output,note,sort,creator) =>
{
    try
    {
        DirectoryInfo directory = new DirectoryInfo(".");
        FileInfo[] files;
        List<string> excludedDirectories = new List<string> { "bin", "debug" };
        files = directory.GetFiles()
            .Where(file => !excludedDirectories.Any(dir => file.FullName.ToLower().Contains(dir)))
            .ToArray();


        if (language.ToLower() == "all")
        {
            // Include all files in the directory
            files = directory.GetFiles();
        }
        else
        {
            // Include only files with the specified language extension
            files = directory.GetFiles("*." + language);
        }
        if (!string.IsNullOrEmpty(sort))
        {
            switch (sort.ToLower())
            {
                case "name":
                    files = files.OrderBy(file => file.Name).ToArray();
                    break;
                case "type":
                    files = files.OrderBy(file => Path.GetExtension(file.Name)).ToArray();
                    break;
                default:
                    Console.WriteLine($"Error: Invalid sort option '{sort}'. Defaulting to 'name'.");
                    files = files.OrderBy(file => file.Name).ToArray();
                    break;
            }
            Console.WriteLine("Sorted files:");
            foreach (var file in files)
            {
                Console.WriteLine(file.FullName);
            }
        }

        // If the output file name doesn't contain a path, use the current directory
        string outputPath = output.FullName;
        if (!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.Combine(Environment.CurrentDirectory, outputPath);
           
        }
        // Check if the directory exists
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Error: Directory {outputDirectory} does not exist.");
            return;
        }
        Console.WriteLine($"Output path: {outputPath}");
        Console.WriteLine($"Number of files bundled: {files.Length}");
        // Create or append to the output file and write the bundled code
        using (StreamWriter writer = File.AppendText(outputPath))
        {
            if (files.Length == 0)
            {
                Console.WriteLine("Error: No files found to bundle.");
                return;
            }
            foreach (var file in files)
            {
                string code = File.ReadAllText(file.FullName);
                if (!string.IsNullOrEmpty(creator))
                {
                    writer.WriteLine($"// Creator: {creator}");
                }

                // Remove empty lines if the option is specified
                if (bundleCommand.Parse(args).HasOption(removeEmptyLinesOption))
                {
                    code = RemoveEmptyLines(code);
                }
                // Include source code note if requested
                if (note)
                {
                    writer.WriteLine($"Source code from: {file.FullName}");
                }
                writer.WriteLine($"// File: {file.Name}");
                writer.WriteLine(code);

            }
            Console.WriteLine("File was created");
           
        }
      
        
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: File path is not valid");
    }
}, languageOption, bundleOption, noteOption, sortOption,creatorOption);
//תיאור הפקודה
var rootCommand = new RootCommand("RootCommand for file bundle cli");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
//בשביל שזה עוטף את הmain
rootCommand.InvokeAsync(args);






