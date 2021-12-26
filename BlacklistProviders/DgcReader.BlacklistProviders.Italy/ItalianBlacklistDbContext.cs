// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using DgcReader.BlacklistProviders.Italy.Entities;
using Microsoft.EntityFrameworkCore;

namespace DgcReader.BlacklistProviders.Italy
{

    public class ItalianBlacklistDbContext : DbContext
    {
        public ItalianBlacklistDbContext(DbContextOptions options) : base(options)
        {

        }
        public ItalianBlacklistDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured)
            {
                var connString = "italianBlacklist.db";
                optionsBuilder.UseSqlite(connString);
            }

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlacklistEntry>(b =>
            {
                b.ToTable("DgcReader_ItalianDrl_Blacklist");
                b.HasKey(e => e.HashedUvci);
                b.Property(e => e.HashedUvci).HasMaxLength(44);
            });

            modelBuilder.Entity<SyncStatus>(b =>
            {
                b.ToTable("DgcReader_ItalianDrl_SyncStatus");


                b.Property<int>("Id").ValueGeneratedNever();
                b.HasKey("Id");

                b.Property(e => e.LocalVersionId).HasMaxLength(24).IsRequired(false);
                b.Property(e => e.TargetVersionId).HasMaxLength(24).IsRequired(false);
            });
        }

        public DbSet<BlacklistEntry> Blacklist { get; private set; }
        public DbSet<SyncStatus> SyncStatus { get; private set; }
    }

    // Workaround for Migrations EFCore 1.x
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}