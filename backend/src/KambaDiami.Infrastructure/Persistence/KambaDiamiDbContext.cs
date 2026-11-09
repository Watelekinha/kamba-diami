using KambaDiami.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Infrastructure.Persistence;

public class KambaDiamiDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public KambaDiamiDbContext(DbContextOptions<KambaDiamiDbContext> options) : base(options) { }

    public DbSet<FamiliaProfile> FamiliaProfiles => Set<FamiliaProfile>();
    public DbSet<SecretariaProfile> SecretariaProfiles => Set<SecretariaProfile>();

    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobPostingCategory> JobPostingCategories => Set<JobPostingCategory>();
    public DbSet<JobPostingLocation> JobPostingLocations => Set<JobPostingLocation>();

    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractClause> ContractClauses => Set<ContractClause>();
    public DbSet<PaymentReference> PaymentReferences => Set<PaymentReference>();
    public DbSet<Rating> Ratings => Set<Rating>();

    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<UserDailyActivity> UserDailyActivities => Set<UserDailyActivity>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- ApplicationUser ----
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(u => u.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(u => u.SuspensionReason).HasMaxLength(500);
            entity.HasIndex(u => u.CreatedAt);

            entity.HasOne(u => u.Address)
                .WithMany()
                .HasForeignKey(u => u.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.FamiliaProfile)
                .WithOne(p => p.User)
                .HasForeignKey<FamiliaProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.SecretariaProfile)
                .WithOne(p => p.User)
                .HasForeignKey<SecretariaProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Location hierarchy ----
        builder.Entity<Municipality>()
            .HasOne(m => m.Province)
            .WithMany(p => p.Municipalities)
            .HasForeignKey(m => m.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Neighborhood>()
            .HasOne(n => n.Municipality)
            .WithMany(m => m.Neighborhoods)
            .HasForeignKey(n => n.MunicipalityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Address>()
            .HasOne(a => a.Neighborhood)
            .WithMany()
            .HasForeignKey(a => a.NeighborhoodId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- ServiceCategory ----
        builder.Entity<ServiceCategory>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // ---- JobPosting ----
        builder.Entity<JobPosting>(entity =>
        {
            entity.Property(j => j.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(j => j.PaymentType).HasConversion<string>().HasMaxLength(20);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(j => j.PaymentValue).HasPrecision(18, 2);
            entity.Property(j => j.HoursPerDay).HasPrecision(5, 2);

            entity.HasOne(j => j.Author)
                .WithMany()
                .HasForeignKey(j => j.AuthorUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<JobPostingCategory>(entity =>
        {
            entity.HasKey(jc => new { jc.JobPostingId, jc.ServiceCategoryId });

            entity.HasOne(jc => jc.JobPosting)
                .WithMany(j => j.Categories)
                .HasForeignKey(jc => jc.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(jc => jc.ServiceCategory)
                .WithMany(c => c.JobPostingCategories)
                .HasForeignKey(jc => jc.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobPostingLocation>(entity =>
        {
            entity.HasKey(jl => new { jl.JobPostingId, jl.NeighborhoodId });

            entity.HasOne(jl => jl.JobPosting)
                .WithMany(j => j.Locations)
                .HasForeignKey(jl => jl.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(jl => jl.Neighborhood)
                .WithMany()
                .HasForeignKey(jl => jl.NeighborhoodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Match ----
        builder.Entity<Match>(entity =>
        {
            entity.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(m => m.JobPosting)
                .WithMany(j => j.Matches)
                .HasForeignKey(m => m.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.InitiatorUser)
                .WithMany()
                .HasForeignKey(m => m.InitiatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.CounterpartUser)
                .WithMany()
                .HasForeignKey(m => m.CounterpartUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Conversation / Message ----
        builder.Entity<Conversation>()
            .HasOne(c => c.Match)
            .WithOne(m => m.Conversation)
            .HasForeignKey<Conversation>(c => c.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>(entity =>
        {
            entity.HasOne(msg => msg.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(msg => msg.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(msg => msg.Sender)
                .WithMany()
                .HasForeignKey(msg => msg.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Contract ----
        builder.Entity<Contract>(entity =>
        {
            entity.Property(c => c.PaymentType).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(c => c.PaymentValue).HasPrecision(18, 2);
            entity.Property(c => c.CancellationReason).HasMaxLength(500);

            entity.HasOne(c => c.Match)
                .WithOne(m => m.Contract)
                .HasForeignKey<Contract>(c => c.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.FamiliaUser)
                .WithMany()
                .HasForeignKey(c => c.FamiliaUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SecretariaUser)
                .WithMany()
                .HasForeignKey(c => c.SecretariaUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Clauses)
                .WithOne(cl => cl.Contract)
                .HasForeignKey(cl => cl.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContractClause>(entity =>
        {
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Body).IsRequired();
        });

        // ---- PaymentReference ----
        builder.Entity<PaymentReference>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(p => p.ReferenceCode).IsUnique();

            entity.HasOne(p => p.Contract)
                .WithMany(c => c.PaymentReferences)
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Rating ----
        builder.Entity<Rating>(entity =>
        {
            entity.HasIndex(r => new { r.ContractId, r.RaterUserId }).IsUnique();

            entity.HasOne(r => r.Contract)
                .WithMany(c => c.Ratings)
                .HasForeignKey(r => r.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.RaterUser)
                .WithMany()
                .HasForeignKey(r => r.RaterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.RateeUser)
                .WithMany()
                .HasForeignKey(r => r.RateeUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Actividade e auditoria ----
        builder.Entity<LoginEvent>(entity =>
        {
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FailureReason).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(300);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UserDailyActivity>(entity =>
        {
            entity.HasKey(a => new { a.UserId, a.Day });
            entity.HasIndex(a => a.Day);

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdminAuditLog>(entity =>
        {
            entity.Property(a => a.Action).IsRequired().HasMaxLength(60);
            entity.Property(a => a.TargetType).IsRequired().HasMaxLength(40);
            entity.Property(a => a.TargetId).HasMaxLength(60);
            entity.Property(a => a.Details).HasMaxLength(1000);
            entity.HasIndex(a => a.CreatedAt);

            entity.HasOne(a => a.AdminUser)
                .WithMany()
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
