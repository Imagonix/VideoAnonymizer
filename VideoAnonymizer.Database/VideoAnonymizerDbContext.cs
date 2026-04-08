using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace VideoAnonymizer.Database
{
    public class VideoAnonymizerDbContext : DbContext
    {
        public DbSet<Video> Videos { get; set; }
        public DbSet<AnalyzedFrame> AnalyzedFrames { get; set; }
        public DbSet<DetectedObject> DetectedObjects { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        public VideoAnonymizerDbContext(DbContextOptions options) : base(options)
        {
        }

        protected VideoAnonymizerDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SystemSetting>()
                .HasIndex(x => x.Key)
                .IsUnique();
        }
    }
}
