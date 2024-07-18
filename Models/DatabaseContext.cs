using Microsoft.EntityFrameworkCore;

namespace WorldCompanyDataViewer.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<DataEntry> DataEntries { get; set; }
        public DbSet<ClusterEntry> ClusterEntries { get; set; }
        public DbSet<PostcodeGeodataEntry> PostcodeGeodataEntries { get; set; }
        //TODO consider making db location user selectable
        public string dbPath = AppDomain.CurrentDomain.BaseDirectory + @"\dataEntries.db";
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DataEntry>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();


            modelBuilder.Entity<ClusterEntry>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            modelBuilder.Entity<ClusterEntry>()
            .HasMany(c => c.PostcodeGeodataEntries)
            .WithMany(p => p.ClusterEntries)
            .UsingEntity<Dictionary<string, object>>(
                "ClusterAndPostcodeGeodataJunctionEntry",
                j => j
                    .HasOne<PostcodeGeodataEntry>()
                    .WithMany()
                    .HasForeignKey("PostcodeGeodataEntryId"),
                j => j
                    .HasOne<ClusterEntry>()
                    .WithMany()
                    .HasForeignKey("ClusterEntryId"));

            modelBuilder.Entity<ClusterEntry>()
                .Navigation(e => e.PostcodeGeodataEntries)
                .AutoInclude();

        }

    }
}
