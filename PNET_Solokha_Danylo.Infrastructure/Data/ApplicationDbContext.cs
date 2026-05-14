using Microsoft.EntityFrameworkCore;

namespace PNET_Solokha_Danylo.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // All entity configurations should be done here or in separate configuration classes
        // Example: builder.ApplyConfiguration(new MyEntityConfiguration());
    }
}
