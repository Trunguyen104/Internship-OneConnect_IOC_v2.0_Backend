using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace IOCv2.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Auth & Users
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<UserCodeSequence> UserCodeSequences { get; set; } = null!;

    // University & Enterprise
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Domain.Entities.University> Universities { get; set; } = null!;
    public DbSet<UniversityUser> UniversityUsers { get; set; } = null!;
    public DbSet<Domain.Entities.Enterprise> Enterprises { get; set; } = null!;
    public DbSet<EnterpriseUser> EnterpriseUsers { get; set; } = null!;

    // Project Management
    public DbSet<Term> Terms { get; set; } = null!;
    public DbSet<StudentTerm> StudentTerms { get; set; } = null!;
    public DbSet<InternshipGroup> InternshipGroups { get; set; } = null!;
    public DbSet<InternshipStudents> InternshipStudents { get; set; } = null!;
    public DbSet<InternshipApplication> InternshipApplications { get; set; } = null!;
    public DbSet<Logbook> Logbooks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectResources> ProjectResources { get; set; } = null!;
    public DbSet<WorkItem> WorkItems { get; set; } = null!;
    public DbSet<Sprint> Sprints { get; set; } = null!;
    public DbSet<SprintWorkItem> SprintWorkItems { get; set; } = null!;
    public DbSet<Domain.Entities.Stakeholder> Stakeholders { get; set; } = null!;
    public DbSet<StakeholderIssue> StakeholderIssues { get; set; } = null!;
    public DbSet<InternshipGroup> InternshipGroups { get; set; } = null!;
    public DbSet<InternshipStudent> InternshipStudents { get; set; } = null!;
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
        {
            currentUserId = parsedId;
        }
        
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        OnModelCreatingPartial(modelBuilder);

        ApplyGlobalFilters(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        // Global Query Filter for Soft Delete (DeletedAt == null)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
                var nullValue = Expression.Constant(null, typeof(DateTime?));
                var equalExpression = Expression.Equal(property, nullValue);
                var lambda = Expression.Lambda(equalExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
