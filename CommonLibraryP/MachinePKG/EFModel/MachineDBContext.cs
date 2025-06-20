using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class MachineDBContext : DbContext
    {
        public MachineDBContext(DbContextOptions<MachineDBContext> options) : base(options)
        {

        }

        public virtual DbSet<ModbusSlaveConfig> ModbusSlaveConfigs { get; set; }

        public virtual DbSet<Machine> Machines { get; set; }
        public virtual DbSet<MachineStatusLog> MachineStatusLogs { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<ModbusTCPTag> ModbusTCPTags { get; set; }

        public virtual DbSet<TagCategory> TagCategories { get; set; }

        //public virtual DbSet<Condition> Conditions { get; set; }

        //public virtual DbSet<ConditionNode> ConditionNodes { get; set; }

        //public virtual DbSet<ConditionAction> ConditionActions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModbusSlaveConfig>(entity =>
            {

                entity.ToTable("ModbusSlaveConfigs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Ip).HasColumnName("Ip");
                entity.Property(e => e.Port).HasColumnName("Port");
                entity.Property(e => e.Station).HasColumnName("Station");
            });

            modelBuilder.Entity<Machine>(entity =>
            {
                entity.HasKey(e => e.Id);
                //entity.UseTpcMappingStrategy();
                entity.ToTable("Machine");

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");
                entity.Property(e => e.Ip)
                    .HasMaxLength(50)
                    .HasColumnName("IP");

                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.TagCategoryId).HasColumnName("TagCategoryID");
                //entity.Property(e => e.LogicStatusCategoryId).HasColumnName("LogicStatusCategoryID");


                entity.HasOne(d => d.TagCategory).WithMany(p => p.Machines)
                    .HasForeignKey(d => d.TagCategoryId);

                entity.Property(e => e.Enabled).HasColumnName("Enabled");
                entity.Property(e => e.UpdateDelay).HasColumnName("UpdateDelay");
                entity.Property(e => e.MaxRetryCount).HasColumnName("MaxRetryCount");
                entity.Property(e => e.RecordStatusChanged).HasColumnName("RecordStatusChanged");

            });

            modelBuilder.Entity<MachineStatusLog>(entity =>
            {

                entity.ToTable("MachineStatusLogs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.MachineID).HasColumnName("MachineID");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.LogTime).HasColumnName("LogTime");
            });

            modelBuilder.Entity<TagCategory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("TagCategory");

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);

                //entity.ToTable("Tag");
                entity.UseTpcMappingStrategy();

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("ID");
                

                entity.HasOne(d => d.Category).WithMany(p => p.Tags)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientCascade);
            });

            modelBuilder.Entity<ModbusTCPTag>(entity =>
            {

                //entity.ToTable("ModbusTCPTags");

            });

            //modelBuilder.Entity<Condition>(entity =>
            //{
            //    entity.HasKey(e => e.Id);

            //    entity.ToTable("Conditions");

            //    entity.HasIndex(e => e.Name).IsUnique();

            //    entity.HasMany(e => e.ConditionNodes).WithOne(p => p.Condition)
            //        .HasForeignKey(q => q.ConditionId);
            //});

            //modelBuilder.Entity<ConditionNode>(entity =>
            //{
            //    entity.UseTpcMappingStrategy();

            //    entity.HasKey(e => e.Id);

            //    //entity.ToTable("ConditionNodes");

            //    entity.HasOne(e => e.Condition).WithMany(p => p.ConditionNodes);

            //    entity.HasOne(e => e.ParentNode).WithMany(f => f.ChildNodes)
            //    .HasForeignKey(g => g.ParentNodeId);
            //});

            //modelBuilder.Entity<ConditionLogicNode>(entity =>
            //{

            //    entity.ToTable("ConditionLogicNodes");

            //});

            //modelBuilder.Entity<ConditionConstDataNode>(entity =>
            //{

            //    entity.ToTable("ConditionConstDataNodes");

            //});

            //modelBuilder.Entity<ConditionTagDataNode>(entity =>
            //{

            //    entity.ToTable("ConditionTagDataNodes");

            //});





            //modelBuilder.Entity<ConditionAction>(entity =>
            //{
            //    entity.UseTpcMappingStrategy();

            //    entity.HasKey(e => e.Id);

            //    entity.HasOne(e => e.Condition).WithMany(p => p.ConditionActions);
            //});
            //modelBuilder.Entity<AwaitAction>(entity =>
            //{

            //    entity.ToTable("AwaitActions");

            //});
            //modelBuilder.Entity<SetTagAction>(entity =>
            //{

            //    entity.ToTable("SetTagActions");

            //});

        }
    }
}
