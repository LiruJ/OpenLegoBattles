using CommandLine;

namespace OpenLegoBattles
{
    internal class CommandLineOptions
    {
        [Option('s', "skip-intro", HelpText = "If the intro logo should be skipped, this also skips the rom content check")]
        public bool SkipIntro { get; set; }
    }
}
