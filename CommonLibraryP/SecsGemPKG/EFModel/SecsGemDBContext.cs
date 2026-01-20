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

		public DbSet<SV> SVs { get; set; }
		public DbSet<HSMSParameter> HSMSParameter { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SV>();
			modelBuilder.Entity<HSMSParameter>(e =>
			{
				e.Property(u => u.CommMode)
				.HasConversion<int>().IsRequired();
				e.Property(p => p.HSMS_Connect_Mode)
				.HasConversion<int>().IsRequired();
			});
		}
	}
}
