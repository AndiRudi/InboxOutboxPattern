using Microsoft.EntityFrameworkCore;

namespace InboxOutboxPattern.Service1
{
    public class ServiceDbContext : DbContext
    {
        public DbSet<Models.Customer> Customers { get; set; }
        public DbSet<Models.OutBoxItem> OutboxItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql($"Host=localhost;Database=InboxOutboxPatternService1;Username={Program.PostgresLogin};Password={Program.PostgresPassword}");
        }
    }

}