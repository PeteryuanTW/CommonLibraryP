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

        public DbSet<SecsTreeNode> SecsTreeNodes { get; set; }

        public DbSet<SecsEvent> SecsEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecsTreeNode>()
            .HasDiscriminator<SecsNodeType>("NodeType")
            .HasValue<SecsList>(SecsNodeType.List)
            .HasValue<SecsAscii>(SecsNodeType.Ascii)
            .HasValue<SecsBinary>(SecsNodeType.Binary)
            .HasValue<SecsBinaryValue>(SecsNodeType.BinaryValue)
            .HasValue<SecsBool>(SecsNodeType.Boolean)
            .HasValue<SecsBoolValue>(SecsNodeType.BooleanValue)
            .HasValue<SecsI1>(SecsNodeType.I1)
            .HasValue<SecsI1Value>(SecsNodeType.I1Value)
            .HasValue<SecsI2>(SecsNodeType.I2)
            .HasValue<SecsI2Value>(SecsNodeType.I2Value)
            .HasValue<SecsI4>(SecsNodeType.I4)
            .HasValue<SecsI4Value>(SecsNodeType.I4Value)
            .HasValue<SecsI8>(SecsNodeType.I8)
            .HasValue<SecsI8Value>(SecsNodeType.I8Value)
            .HasValue<SecsU1>(SecsNodeType.U1)
            .HasValue<SecsU1Value>(SecsNodeType.U1Value)
            .HasValue<SecsU2>(SecsNodeType.U2)
            .HasValue<SecsU2Value>(SecsNodeType.U2Value)
            .HasValue<SecsU4>(SecsNodeType.U4)
            .HasValue<SecsU4Value>(SecsNodeType.U4Value)
            .HasValue<SecsU8>(SecsNodeType.U8)
            .HasValue<SecsU8Value>(SecsNodeType.U8Value)
            .HasValue<SecsF4>(SecsNodeType.F4)
            .HasValue<SecsF4Value>(SecsNodeType.F4Value)
            .HasValue<SecsF8>(SecsNodeType.F8)
            .HasValue<SecsF8Value>(SecsNodeType.F8Value);


            modelBuilder.Entity<SecsTreeNode>()
            .Property<SecsNodeType>("NodeType")
            .HasColumnName("NodeType")
            .HasConversion<int>();

            modelBuilder.Entity<SecsEvent>();





        }
    }
}
