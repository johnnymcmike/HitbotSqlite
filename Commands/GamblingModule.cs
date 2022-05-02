using DSharpPlus.CommandsNext;
using HitbotSqlite.Services;

namespace HitbotSqlite.Commands;

public class GamblingModule : BaseCommandModule
{
    private EconService Econ { get; set; }
    
}