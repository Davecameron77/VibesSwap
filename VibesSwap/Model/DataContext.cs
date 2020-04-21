using Microsoft.EntityFrameworkCore;
using VibesSwap.ViewModel.Helpers;

namespace VibesSwap.Model
{
    class DataContext : DbContext
    {
        #region Constructors



        #endregion

        #region Members

        public DbSet<VibesHost> EnvironmentHosts { get; set; }
        public DbSet<VibesCm> HostCms { get; set; }

        #endregion

        #region Methods

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(StaticGlobals.GetConnectionString())
                .EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VibesCm>()
                .HasOne<VibesHost>(c => c.VibesHost)
                .WithMany(h => h.VibesCms)
                .HasForeignKey(h => h.VibesHostId);
        }

        #endregion
    }
}
