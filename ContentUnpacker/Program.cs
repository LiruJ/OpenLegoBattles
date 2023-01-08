// See https://aka.ms/new-console-template for more information

using CommandLine;
using ContentUnpacker;

// Ensure arguments were given.
if (args.Length < 2)
{
    Console.WriteLine("Input and output paths required.");
    Console.ReadKey();
    return;
}

// Get the paths from the arguments.
string inputFilePath = args[0];
string outputFolderPath = args[1];

CommandLineOptions? options = null;
Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsedOptions) =>
{
    // TODO: Check if paths are valid.

    // Set the options.
    options = parsedOptions;
}).WithNotParsed((parsedOptions) =>
{
    return;
});

// Start the unpacker.
await RomUnpacker.UnpackFileAsync(options);