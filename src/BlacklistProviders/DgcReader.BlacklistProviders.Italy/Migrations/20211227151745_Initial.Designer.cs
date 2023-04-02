using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using DgcReader.BlacklistProviders.Italy;

namespace DgcReader.BlacklistProviders.Italy.Migrations
{
    [DbContext(typeof(ItalianDrlBlacklistDbContext))]
    [Migration("20211227151745_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.6");

            modelBuilder.Entity("DgcReader.BlacklistProviders.Italy.Entities.BlacklistEntry", b =>
                {
                    b.Property<string>("HashedUCVI")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(44);

                    b.HasKey("HashedUCVI");

                    b.ToTable("DgcReader_ItalianDrl_Blacklist");
                });

            modelBuilder.Entity("DgcReader.BlacklistProviders.Italy.Entities.SyncStatus", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("CurrentVersion");

                    b.Property<string>("CurrentVersionId")
                        .HasMaxLength(24);

                    b.Property<DateTime>("LastCheck");

                    b.Property<int>("LastChunkSaved");

                    b.Property<int>("TargetChunkSize");

                    b.Property<int>("TargetChunksCount");

                    b.Property<int>("TargetTotalNumberUCVI");

                    b.Property<int>("TargetVersion");

                    b.Property<string>("TargetVersionId")
                        .HasMaxLength(24);

                    b.HasKey("Id");

                    b.ToTable("DgcReader_ItalianDrl_SyncStatus");
                });
        }
    }
}
