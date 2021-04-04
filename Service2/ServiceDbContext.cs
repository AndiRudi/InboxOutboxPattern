using Microsoft.EntityFrameworkCore;

namespace InboxOutboxPattern.Service2
{
    public class ServiceDbContext : DbContext
    {
        public DbSet<Models.Customer> Customers { get; set; }
        public DbSet<Models.InBoxItem> InboxItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=InboxOutboxPatternService2;Username=postgres;Password=postgres");
        }
    }

}