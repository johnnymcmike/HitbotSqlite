using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HitbotSqlite.DataAccess;
using HitbotSqlite.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HitbotSqlite;

internal class Program
{
    private static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        //CONNECTIONS
        //read in json files

        //initialize connection to discord
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = await File.ReadAllTextAsync("token.txt"),
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });
        discord.UseInteractivity(new InteractivityConfiguration
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });
        var rng = new Random();
        var http = new HttpClient();
        var econdb = new EconContext();
        http.DefaultRequestHeaders.Add("User-Agent", "Epic C# Discord Bot (mbjmcm@gmail.com)");
        var services = new ServiceCollection()
            .AddSingleton(rng)
            .AddSingleton(http)
            .AddSingleton(econdb)
            .AddSingleton(new EconService(econdb))
            .BuildServiceProvider();

        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] {";"},
            Services = services
        });
        discord.GetCommandsNext().CommandErrored += async (s, e) =>
        {
            Console.WriteLine("Command errored:");
            Console.WriteLine(e.Exception);
        };
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}