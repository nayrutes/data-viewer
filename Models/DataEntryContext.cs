using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WorldCompanyDataViewer.Models
{
    internal class DataEntryContext : DbContext
    {
        //TODO split up into 2 or more Models as tasks asks for 2 or more tables in the db
        public DbSet<DataEntry> DataEntries { get; set; }
        //TODO make path not fixed
        public string dbPath = @"C:\Users\Felix\Documents\Projects\FatsharkCodeTest\dataEntries.db";
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataEntry>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            //base.OnModelCreating(modelBuilder);
        }

    }
}
