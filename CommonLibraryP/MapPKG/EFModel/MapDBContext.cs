using CommonLibraryP.MachinePKG;
using CommonLibraryP.ShopfloorPKG;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public class MapDBContext : DbContext
    {
        public MapDBContext(DbContextOptions<MapDBContext> options)
        : base(options)
        {
        }

        public virtual DbSet<MapConfig> MapConfigs { get; set; }
        public virtual DbSet<MapComponent> MapComponents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MapConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();

                //entity.HasMany(x => x.MapComponents).WithOne(y => y.MapConfig);
            });

            modelBuilder.Entity<MapComponent>(entity =>
            {
                entity.UseTpcMappingStrategy();

                entity.HasKey(e => e.Id);

                entity.HasOne(x => x.MapConfig).WithMany(x => x.MapComponents).HasForeignKey(x => x.MapId);
            });

            modelBuilder.Entity<MapComponentStation>(entity =>
            {
                entity.ToTable("MapComponentStations");
            });
            modelBuilder.Entity<MapComponentMachine>(entity =>
            {
                entity.ToTable("MapComponentMachines");
            });
        }
    }
}
