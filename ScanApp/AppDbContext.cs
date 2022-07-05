using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ScanApp
{
    public class AppDbContext : DbContext
    {
        public DbSet<ScanResult> ScanResults { get; set; }

        public string DbPath { get; }

        public AppDbContext()
        {
            var path = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            DbPath = System.IO.Path.Join(path, "scanapp.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ScanResult
            // ==========

            modelBuilder.Entity<ScanResult>()
                .HasIndex(p => p.MD5);

            modelBuilder.Entity<ScanResult>()
                .HasIndex(p => p.Sha1);

            modelBuilder.Entity<ScanResult>()
                .HasIndex(p => p.Sha256)
                .IsUnique();

            modelBuilder.Entity<ScanResult>()
                .HasIndex(p => p.CacheKey);
        }
    }

    public class ScanResult
    {
        [Key]
        public long ScanResultId { get; set; }

        public string FilePath { get; set; }
        public string MD5 { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }

        public long FileSize { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public DateTime? LastSeen { get; set; }
        public int Scanned { get; set; }

        public string CacheKey { get; set; }
    }
}

