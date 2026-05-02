using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.Database.SQLite;

public class VideoAnonymizerDbContextFactory : IDesignTimeDbContextFactory<VideoAnonymizerDbContext>
{
    public VideoAnonymizerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VideoAnonymizerDbContext>();
        optionsBuilder.UseSqlite("Data Source=design-time.db", b => b.MigrationsAssembly("VideoAnonymizer.Database.SQLite"));
        return new VideoAnonymizerDbContext(optionsBuilder.Options);
    }
}
