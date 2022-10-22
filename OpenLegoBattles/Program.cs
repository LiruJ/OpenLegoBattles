using System;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var game = new OpenLegoBattles.Game1();
        game.Run();
    }
}