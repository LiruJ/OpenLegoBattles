using CommandLine;

namespace ContentUnpacker
{
    internal class CommandLineOptions
    {
        [Option('o', "output", Required = true, HelpText = "The output directory of the fully processed data")]
        public string OutputFolder { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input rom file")]
        public string InputFile { get; set; }
    }
}
