// See https://aka.ms/new-console-template for more information

using ContentUnpacker;
using Microsoft.Extensions.Logging.Abstractions;

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

// Start the unpacker.
await RomUnpacker.UnpackFileAsync(inputFilePath, outputFolderPath, NullLogger.Instance);