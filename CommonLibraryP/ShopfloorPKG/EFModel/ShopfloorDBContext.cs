using CommonLibraryP.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonLibraryP.ShopfloorPKG
{
    public partial class ShopfloorDBContext : DbContext
    {
        public ShopfloorDBContext(DbContextOptions<ShopfloorDBContext> options)
        : base(options)
        {
        }

        #region process
        public virtual DbSet<Process> Processes { get; set; }
        public virtual DbSet<ProcessMachineRelation> ProcessMachineRelations { get; set; }
        #endregion

        #region station
        public virtual DbSet<Station> Stations { get; set; }
        #endregion

        #region workorder
        public virtual DbSet<Workorder> Workorders { get; set; }
        public virtual DbSet<WorkorderRecordConfig> WorkorderRecordConfigs { get; set; }
        public virtual DbSet<WorkorderRecordContent> WorkorderRecordContents { get; set; }
        public virtual DbSet<WorkorderRecordDetail> WorkorderRecordDetails { get; set; }
        #endregion

        #region item
        public virtual DbSet<ItemDetail> ItemDetails { get; set; }
        public virtual DbSet<ItemRecord> ItemRecords { get; set; }
        //public virtual DbSet<ItemRecordConfig> ItemRecordConfigs { get; set; }
        //public virtual DbSet<ItemRecordContent> ItemRecordContents { get; set; }
        //public virtual DbSet<ItemRecordDetail> ItemRecordDetails { get; set; }
        #endregion

        #region task
        public virtual DbSet<TaskDetail> TaskDetails { get; set; }
        //public virtual DbSet<TaskRecordConfig> TaskRecordConfigs { get; set; }
        //public virtual DbSet<TaskRecordContent> TaskRecordContents { get; set; }
        //public virtual DbSet<TaskRecordDetail> TaskRecordDetails { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Process>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("Process");

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("ID");

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.HasMany(d => d.Stations).WithOne(p => p.Process)
                    .HasForeignKey(d => d.ProcessId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProcessMachineRelation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("ProcessMachineRelations");

                entity.Property(e => e.ProcessId).HasColumnName("ProcessID");
                entity.Property(e => e.MachineId).HasColumnName("MachineID");
            });

            modelBuilder.Entity<Station>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Name).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("ID");
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.ProcessId).HasColumnName("ProcessID");

                entity.HasOne(d => d.Process).WithMany(p => p.Stations)
                    .HasForeignKey(d => d.ProcessId)
                    .OnDelete(DeleteBehavior.Restrict);
            });



            modelBuilder.Entity<Workorder>(entity =>
            {
                //entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.WorkorderNo, e.Lot }).IsUnique();

                entity.Property(e => e.Id);
                entity.Property(e => e.WorkorderNo);
                entity.Property(e => e.Lot);

                entity.Property(e => e.ProcessId);

                entity.Property(e => e.Status);
                entity.Property(e => e.PartNo);
                entity.Property(e => e.NgAmount);
                //entity.Property(e => e.OkAmount);
                entity.Property(e => e.TargetAmount);

                entity.Property(e => e.CreateTime);
                entity.Property(e => e.StartTime);
                entity.Property(e => e.FinishedTime);

                entity.Property(e => e.WorkorderRecordCategoryId);
                entity.Property(e => e.RecipeCategoryId);
                entity.Property(e => e.ItemRecordsCategoryId);
                entity.Property(e => e.TaskRecordCategoryId);

                entity.HasOne(d => d.Process).WithMany(p => p.Workorders)
                    .HasForeignKey(d => d.ProcessId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.WorkorderRecordCategory).WithMany(p => p.Workorders)
                    .HasForeignKey(d => d.WorkorderRecordCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                //entity.HasOne(d => d.RecipeCategory).WithMany(p => p.Workorders)
                //    .HasForeignKey(d => d.RecipeCategoryId)
                //    .OnDelete(DeleteBehavior.Restrict);

                //entity.HasOne(d => d.ItemRecordsCategory).WithMany(p => p.Workorders)
                //    .HasForeignKey(d => d.ItemRecordsCategoryId)
                //    .OnDelete(DeleteBehavior.Restrict);

                //entity.HasOne(d => d.TaskRecordCategory).WithMany(p => p.Workorders)
                //    .HasForeignKey(d => d.TaskRecordCategoryId)
                //    .OnDelete(DeleteBehavior.Restrict);


            });

            modelBuilder.Entity<WorkorderRecordConfig>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.WorkorderRecordCategory).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.WorkorderRecordCategory).HasMaxLength(50);
            });

            modelBuilder.Entity<WorkorderRecordContent>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");
                entity.Property(e => e.ConfigId).HasColumnName("ConfigID");
                entity.Property(e => e.RecordName).HasMaxLength(50);

                entity.HasOne(d => d.Config).WithMany(p => p.WorkorderRecordContents)
                    .HasForeignKey(d => d.ConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(d => d.WorkorderRecordDetails).WithOne(p => p.RecordContent)
                .HasForeignKey(d => d.RecordContentId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkorderRecordDetail>(entity =>
            {
                entity.HasKey(e => new { e.WorkerderId, e.RecordContentId });

                entity.Property(e => e.WorkerderId).HasColumnName("WorkerderID");
                entity.Property(e => e.RecordContentId).HasColumnName("RecordContentID");
                entity.Property(e => e.Value).HasMaxLength(50);

                entity.HasOne(d => d.RecordContent).WithMany(p => p.WorkorderRecordDetails)
                    .HasForeignKey(d => d.RecordContentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Workerder).WithMany(p => p.WorkorderRecordDetails)
                    .HasForeignKey(d => d.WorkerderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



            modelBuilder.Entity<ItemDetail>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.WorkordersId, e.SerialNo }).IsUnique();

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");
                entity.Property(e => e.FinishedTime).HasColumnType("datetime");
                entity.Property(e => e.Ngamount).HasColumnName("NGAmount");
                entity.Property(e => e.Okamount).HasColumnName("OKAmount");
                entity.Property(e => e.SerialNo).HasMaxLength(50);
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.WorkordersId).HasColumnName("WorkordersID");

                entity.HasOne(d => d.Workorder).WithMany(p => p.ItemDetails)
                    .HasForeignKey(d => d.WorkordersId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.HasOne(d => d.ItemDetail).WithMany(p => p.ItemRecords)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //modelBuilder.Entity<ItemRecordConfig>(entity =>
            //{
            //    entity.HasKey(e => e.Id);

            //    entity.HasIndex(e => e.ItemRecordCategory).IsUnique();

            //    entity.Property(e => e.Id)
            //        .ValueGeneratedNever()
            //        .HasColumnName("ID");

            //    entity.Property(e => e.ItemRecordCategory).HasMaxLength(50);

            //    entity.HasMany(e => e.ItemRecordContents).WithOne(p => p.Config)
            //    .HasForeignKey(e => e.ConfigId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasMany(e=>e.Workorders).WithOne(p=>p.ItemRecordsCategory)
            //    .HasForeignKey(e => e.ItemRecordsCategoryId)
            //    .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<ItemRecordContent>(entity =>
            //{
            //    entity.HasKey(e => e.Id);

            //    entity.Property(e => e.Id)
            //        .ValueGeneratedNever()
            //        .HasColumnName("ID");
            //    entity.Property(e => e.ConfigId).HasColumnName("ConfigID");
            //    entity.Property(e => e.RecordName).HasMaxLength(50);

            //    entity.HasOne(d => d.Config).WithMany(p => p.ItemRecordContents)
            //        .HasForeignKey(d => d.ConfigId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasMany(d => d.ItemRecordDetails).WithOne(p => p.RecordContent)
            //    .HasForeignKey(d => d.RecordContentId)
            //    .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<ItemRecordDetail>(entity =>
            //{
            //    entity.HasKey(e => new { e.ItemId, e.RecordContentId });

            //    entity.Property(e => e.ItemId).HasColumnName("ItemID");
            //    entity.Property(e => e.RecordContentId).HasColumnName("RecordContentID");
            //    entity.Property(e => e.Value).HasMaxLength(50);

            //    entity.HasOne(d => d.Item).WithMany(p => p.ItemRecordDetails)
            //        .HasForeignKey(d => d.ItemId)
            //        .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasOne(d => d.RecordContent).WithMany(p => p.ItemRecordDetails)
            //        .HasForeignKey(d => d.RecordContentId)
            //        .OnDelete(DeleteBehavior.Restrict);
            //});

            modelBuilder.Entity<TaskDetail>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("ID");
                entity.Property(e => e.FinishedTime).HasColumnType("datetime");
                entity.Property(e => e.ItemId).HasColumnName("ItemID");
                entity.Property(e => e.StartTime).HasColumnType("datetime");
                entity.Property(e => e.StationId).HasColumnName("StationID");

                entity.HasOne(d => d.Item).WithMany(p => p.TaskDetails)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Station).WithMany(p => p.TaskDetails)
                    .HasForeignKey(d => d.StationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //modelBuilder.Entity<TaskRecordConfig>(entity =>
            //{
            //    entity.HasKey(e => e.Id);

            //    entity.HasIndex(e => e.TaskRecordsCategory).IsUnique();

            //    entity.Property(e => e.Id)
            //        .ValueGeneratedNever()
            //        .HasColumnName("ID");
            //    entity.Property(e => e.TaskRecordsCategory).HasMaxLength(50);

            //    entity.HasMany(e => e.TaskRecordContents).WithOne(p => p.Config)
            //    .HasForeignKey(e => e.ConfigId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //    entity.HasMany(e => e.Workorders).WithOne(p => p.TaskRecordCategory)
            //    .HasForeignKey(e => e.TaskRecordCategoryId)
            //    .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<TaskRecordContent>(entity =>
            //{
            //    entity.HasKey(e => e.Id);

            //    entity.Property(e => e.Id)
            //        .ValueGeneratedNever()
            //        .HasColumnName("ID");
            //    entity.Property(e => e.ConfigId).HasColumnName("ConfigID");
            //    entity.Property(e => e.RecordName).HasMaxLength(50);

            //    entity.HasOne(d => d.Config).WithMany(p => p.TaskRecordContents)
            //        .HasForeignKey(d => d.ConfigId)
            //        .OnDelete(DeleteBehavior.Restrict);

            //    entity.HasMany(d => d.TaskRecordDetails).WithOne(p => p.RecordContent)
            //    .HasForeignKey(d => d.RecordContentId)
            //    .OnDelete(DeleteBehavior.Restrict);
            //});

            //modelBuilder.Entity<TaskRecordDetail>(entity =>
            //{
            //    entity.HasKey(e => new { e.TaskId, e.RecordContentId });

            //    entity.Property(e => e.TaskId).HasColumnName("TaskID");
            //    entity.Property(e => e.RecordContentId).HasColumnName("RecordContentID");
            //    entity.Property(e => e.Value).HasMaxLength(50);

            //    entity.HasOne(d => d.RecordContent).WithMany(p => p.TaskRecordDetails)
            //        .HasForeignKey(d => d.RecordContentId)
            //        .OnDelete(DeleteBehavior.Restrict);

            //    entity.HasOne(d => d.Task).WithMany(p => p.TaskRecordDetails)
            //        .HasForeignKey(d => d.TaskId)
            //        .OnDelete(DeleteBehavior.Cascade);
            //});

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
