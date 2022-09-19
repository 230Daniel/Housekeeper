using Housekeeper.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Housekeeper.Database;

public class DatabaseContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }

    private readonly string _connectionString;

    public DatabaseContext(IConfiguration config)
    {
        _connectionString = config["Database:Connection"];
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_connectionString);
        options.UseSnakeCaseNamingConvention();
    }
}
