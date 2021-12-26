using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using DgcReader.BlacklistProviders.Italy;

namespace DgcReader.BlacklistProviders.Italy.Migrations
{
    [DbContext(typeof(ItalianBlacklistDbContext))]
    partial class ItalianBlacklistDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.6");

            modelBuilder.Entity("DgcReader.BlacklistProviders.Italy.Entities.BlacklistEntry", b =>
                {
                    b.Property<string>("HashedUvci")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(44);

                    b.HasKey("HashedUvci");

                    b.ToTable("DgcReader_ItalianDrl_Blacklist");
                });

            modelBuilder.Entity("DgcReader.BlacklistProviders.Italy.Entities.SyncStatus", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("ChunksCount");

                    b.Property<DateTime>("LastCheck");

                    b.Property<int>("LastChunkSaved");

                    b.Property<int>("LocalVersion");

                    b.Property<string>("LocalVersionId")
                        .HasMaxLength(24);

                    b.Property<int>("TargetVersion");

                    b.Property<string>("TargetVersionId")
                        .HasMaxLength(24);

                    b.Property<int>("TotalNumberUVCI");

                    b.HasKey("Id");

                    b.ToTable("DgcReader_ItalianDrl_SyncStatus");
                });
        }
    }
}
