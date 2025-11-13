using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsGemDBContext : DbContext
    {
        public SecsGemDBContext(DbContextOptions<SecsGemDBContext> options) : base(options)
        {

        }

        public virtual DbSet<SecsTreeNode> SecsTreeNodes { get; set; }
        public virtual DbSet<SecsList> SecsLists { get; set; }
        public virtual DbSet<SecsAscii> SecsAsciis { get; set; }
        public virtual DbSet<SecsBinary> SecsBinarys { get; set; }
        public virtual DbSet<SecsBinaryValue> SecsBinaryValues { get; set; }
        public virtual DbSet<SecsBool> SecsBools { get; set; }
        public virtual DbSet<SecsBoolValue> SecsBoolValues { get; set; }
        public virtual DbSet<SecsI1> SecsI1s { get; set; }
        public virtual DbSet<SecsI1Value> SecsI1Values { get; set; }
        public virtual DbSet<SecsI2> SecsI2s { get; set; }
        public virtual DbSet<SecsI2Value> SecsI2Values { get; set; }
        public virtual DbSet<SecsI4> SecsI4s { get; set; }
        public virtual DbSet<SecsI4Value> SecsI4Values { get; set; }
        public virtual DbSet<SecsI8> SecsI8s { get; set; }
        public virtual DbSet<SecsI8Value> SecsI8Values { get; set; }
        public virtual DbSet<SecsU1> SecsU1s { get; set; }
        public virtual DbSet<SecsU1Value> SecsU1Values { get; set; }
        public virtual DbSet<SecsU2> SecsU2s { get; set; }
        public virtual DbSet<SecsU2Value> SecsU2Values { get; set; }
        public virtual DbSet<SecsU4> SecsU4s { get; set; }
        public virtual DbSet<SecsU4Value> SecsU4Values { get; set; }
        public virtual DbSet<SecsU8> SecsU8s { get; set; }
        public virtual DbSet<SecsU8Value> SecsU8Values { get; set; }
        public virtual DbSet<SecsF4> SecsF4s { get; set; }
        public virtual DbSet<SecsF4Value> SecsF4Values { get; set; }
        public virtual DbSet<SecsF8> SecsF8s { get; set; }
        public virtual DbSet<SecsF8Value> SecsF8Values { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecsTreeNode>(entity =>
            {
                entity.HasKey(e => e.Id);


                entity.UseTpcMappingStrategy();
            });

            modelBuilder.Entity<SecsList>(entity =>
            {

                //entity.Ignore(nameof(SecsItemBase.ValueSourceCode));
            });

            modelBuilder.Entity<SecsAscii>();
            modelBuilder.Entity<SecsBinary>();
            modelBuilder.Entity<SecsBinaryValue>();
            modelBuilder.Entity<SecsBool>();
            modelBuilder.Entity<SecsBoolValue>();
            modelBuilder.Entity<SecsI1>();
            modelBuilder.Entity<SecsI1Value>();
            modelBuilder.Entity<SecsI2>();
            modelBuilder.Entity<SecsI2Value>();
            modelBuilder.Entity<SecsI4>();
            modelBuilder.Entity<SecsI4Value>();
            modelBuilder.Entity<SecsI8>();
            modelBuilder.Entity<SecsI8Value>();
            modelBuilder.Entity<SecsU1>();
            modelBuilder.Entity<SecsU1Value>();
            modelBuilder.Entity<SecsU2>();
            modelBuilder.Entity<SecsU2Value>();
            modelBuilder.Entity<SecsU4>();
            modelBuilder.Entity<SecsU4Value>();
            modelBuilder.Entity<SecsU8>();
            modelBuilder.Entity<SecsU8Value>();
            modelBuilder.Entity<SecsF4>();
            modelBuilder.Entity<SecsF4Value>();
            modelBuilder.Entity<SecsF8>();
            modelBuilder.Entity<SecsF8Value>();




        }
    }
}
