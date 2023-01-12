using CommandLine;
using OpenLegoBattles;
using System;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Parse the command line options and start the game.
        CommandLineOptions options = new();
        Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsedOptions) => options = parsedOptions)
            .WithNotParsed((parsedOptions) => throw new Exception("Start parameters could not be parsed."));
        using var game = new Game1(options);
        game.Run();
    }
}