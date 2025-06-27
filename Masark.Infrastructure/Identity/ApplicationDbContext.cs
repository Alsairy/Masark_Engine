using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Masark.Domain.Entities;
using Masark.Domain.Common;
using Masark.Infrastructure.Services;

namespace Masark.Infrastructure.Identity
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        private readonly ITenantContextAccessor? _tenantContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContextAccessor? tenantContextAccessor = null)
            : base(options)
        {
            _tenantContextAccessor = tenantContextAccessor;
        }

        public DbSet<AssessmentSession> AssessmentSessions { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AssessmentAnswer> AssessmentAnswers { get; set; }
        public DbSet<PersonalityType> PersonalityTypes { get; set; }
        public DbSet<Career> Careers { get; set; }
        public DbSet<CareerCluster> CareerClusters { get; set; }
        public DbSet<PersonalityCareerMatch> PersonalityCareerMatches { get; set; }
        public DbSet<Program> Programs { get; set; }
        public DbSet<Pathway> Pathways { get; set; }
        public DbSet<CareerProgram> CareerPrograms { get; set; }
        public DbSet<CareerPathway> CareerPathways { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureIdentityTables(modelBuilder);
            ConfigureDomainEntities(modelBuilder);
            ConfigureMultiTenancy(modelBuilder);
        }

        private void ConfigureIdentityTables(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            });
        }

        private void ConfigureDomainEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssessmentSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.Property(e => e.LanguagePreference).HasMaxLength(10);
                entity.Property(e => e.UserAgent).HasMaxLength(1000);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TextEn).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.TextAr).HasMaxLength(1000);
                entity.Property(e => e.OptionATextEn).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OptionATextAr).HasMaxLength(500);
                entity.Property(e => e.OptionBTextEn).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OptionBTextAr).HasMaxLength(500);
                entity.HasIndex(e => new { e.TenantId, e.Dimension, e.OrderNumber });
            });

            modelBuilder.Entity<Career>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NameAr).HasMaxLength(200);
                entity.Property(e => e.DescriptionEn).HasMaxLength(2000);
                entity.Property(e => e.DescriptionAr).HasMaxLength(2000);
                entity.Property(e => e.SsocCode).HasMaxLength(20);
                entity.HasOne(e => e.Cluster).WithMany(c => c.Careers).HasForeignKey(e => e.ClusterId);
                entity.HasIndex(e => new { e.TenantId, e.ClusterId });
            });

            modelBuilder.Entity<PersonalityCareerMatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.PersonalityType).WithMany().HasForeignKey(e => e.PersonalityTypeId);
                entity.HasOne(e => e.Career).WithMany().HasForeignKey(e => e.CareerId);
                entity.HasIndex(e => new { e.TenantId, e.PersonalityTypeId, e.CareerId }).IsUnique();
            });
        }

        private void ConfigureMultiTenancy(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                        CreateTenantFilter(entityType.ClrType));
                }
            }
        }

        private LambdaExpression CreateTenantFilter(Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var tenantId = _tenantContextAccessor?.TenantContext?.TenantId ?? 1;
            var constant = Expression.Constant(tenantId);
            var equal = Expression.Equal(tenantIdProperty, constant);
            
            return Expression.Lambda(equal, parameter);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor?.TenantContext?.TenantId ?? 1;
            
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.TenantId = tenantId;
                        if (entry.Entity is Entity entity)
                        {
                            entity.UpdateTimestamp();
                        }
                        break;
                    case EntityState.Modified:
                        if (entry.Entity is Entity modifiedEntity)
                        {
                            modifiedEntity.UpdateTimestamp();
                        }
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
