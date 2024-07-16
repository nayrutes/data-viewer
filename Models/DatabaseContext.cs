using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WorldCompanyDataViewer.Models
{
    public class DatabaseContext : DbContext
    {
        //TODO split up into 2 or more Models as tasks asks for 2 or more tables in the db
        public DbSet<DataEntry> DataEntries { get; set; }
        public DbSet<ClusterEntry> ClusterEntries { get; set; }
        public DbSet<PostcodeGeodataEntry> PostcodeGeodataEntries { get; set; }
        //TODO make path not fixed
        public string dbPath = @"C:\Users\Felix\Documents\Projects\FatsharkCodeTest\dataEntries.db";
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);

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
            //modelBuilder.Entity<ClusterEntry>()
            //    .HasMany(c => c.PostcodeGeodataEntries)
            //    .WithOne(p => p.ClusterEntry)
            //    .HasForeignKey(p => p.ClusterEntryId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.Cascade); //delete clusters when deleting entries
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

        }

    }
}
