using DgcReader.BlacklistProviders.Italy.Entities;
using Microsoft.EntityFrameworkCore;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy
{

    /// <summary>
    /// Ef core DbContext for storing the blacklist entries
    /// </summary>
    public class ItalianDrlBlacklistDbContext : DbContext
    {
        /// <inheritdoc/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ItalianDrlBlacklistDbContext(DbContextOptions options) : base(options)
        {

        }

        /// <inheritdoc/>
        public ItalianDrlBlacklistDbContext()
        {
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured)
            {
                var connString = "DataSource=italian-drl.db";
                optionsBuilder.UseSqlite(connString);
            }

        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlacklistEntry>(b =>
            {
                b.ToTable("DgcReader_ItalianDrl_Blacklist");
                b.HasKey(e => e.HashedUCVI);
                b.Property(e => e.HashedUCVI).HasMaxLength(44);
            });

            modelBuilder.Entity<SyncStatus>(b =>
            {
                b.ToTable("DgcReader_ItalianDrl_SyncStatus");

                b.Property(r => r.Id).ValueGeneratedNever();
                b.HasKey(r=>r.Id);

                b.Property(e => e.CurrentVersionId).HasMaxLength(24).IsRequired(false);
                b.Property(e => e.TargetVersionId).HasMaxLength(24).IsRequired(false);

            });
        }

        /// <summary>
        /// The blacklist containing the sha256 hash of the blacklisted UCVIs
        /// </summary>
        public DbSet<BlacklistEntry> Blacklist { get; private set; }

        /// <summary>
        /// Status of the syncronization with the backend
        /// </summary>
        public DbSet<SyncStatus> SyncStatus { get; private set; }
    }

    ///// <summary>
    ///// Workaround for Migrations EFCore 1.x
    ///// </summary>
    //public class Program
    //{
    //    /// <inheritdoc/>
    //    public static void Main(string[] args)
    //    {
    //    }
    //}
}