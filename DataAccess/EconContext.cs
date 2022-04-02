using HitbotSqlite.Models;
using Microsoft.EntityFrameworkCore;

namespace HitbotSqlite.DataAccess;

public class EconContext : DbContext
{
    public string DbPath { get; }

    public EconContext()
    {
        string folder = Directory.GetCurrentDirectory();
        DbPath = Path.Join(folder, "Econ.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }

    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Member> Members { get; set; }
}