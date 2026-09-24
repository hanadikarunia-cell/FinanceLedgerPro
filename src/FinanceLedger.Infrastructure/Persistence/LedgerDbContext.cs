using FinanceLedger.Application.Interfaces;
using FinanceLedger.Domain.Common;
using FinanceLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinanceLedger.Infrastructure.Persistence;

public class LedgerDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public LedgerDbContext(DbContextOptions<LedgerDbContext> options, ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// The site this context is confined to. The global query filters below reference
    /// this property (not a captured value), so EF re-reads it for every query - a
    /// context therefore can never read another site's rows, even by accident. Null
    /// matches nothing (the column is NOT NULL), i.e. it fails closed.
    /// </summary>
    private string? CurrentTenantId => _tenantProvider.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ImpersonationSession> ImpersonationSessions => Set<ImpersonationSession>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<PettyCashRequest> PettyCashRequests => Set<PettyCashRequest>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Feedback> FeedbackItems => Set<Feedback>();
    public DbSet<ReleaseNote> ReleaseNotes => Set<ReleaseNote>();

    // Application-layer code (e.g. DashboardService's report date ranges) sometimes
    // builds DateTimes via `new DateTime(y, m, d)`, which defaults to Kind=Unspecified.
    // Npgsql refuses to write an unspecified-kind DateTime into a `timestamptz` column
    // (the Cosmos provider never cared about Kind, so this was latent until now).
    // Normalizing every DateTime at the EF Core boundary fixes this everywhere at once,
    // without touching business logic: every value in this app is implicitly UTC.
    // (A plain static method, not inlined into the converters' lambdas below: switch
    // expressions aren't valid inside the Expression<Func<>> a ValueConverter takes.)
    private static DateTime ToUtc(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };

    private sealed class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeValueConverter() : base(v => ToUtc(v), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class NullableUtcDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeValueConverter()
            : base(
                v => v.HasValue ? ToUtc(v.Value) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeValueConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema/indexes are owned by supabase/migrations/*.sql — this mapping targets
        // the tables that SQL creates. UseXminAsConcurrencyToken relies on Postgres's
        // built-in per-row xmin system column, so no explicit version column is needed.
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.Name).HasColumnName("name");
            builder.Property(t => t.Code).HasColumnName("code");
            builder.Property(t => t.IsActive).HasColumnName("is_active");
            builder.Property(t => t.CreatedDate).HasColumnName("created_date");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<ImpersonationSession>(builder =>
        {
            builder.ToTable("impersonation_sessions");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("id");
            builder.Property(s => s.AdminUserId).HasColumnName("admin_user_id");
            builder.Property(s => s.TargetUserId).HasColumnName("target_user_id");
            builder.Property(s => s.TargetTenantId).HasColumnName("target_tenant_id");
            builder.Property(s => s.StartedAt).HasColumnName("started_at");
            builder.Property(s => s.ExpiresAt).HasColumnName("expires_at");
            builder.Property(s => s.EndedAt).HasColumnName("ended_at");
            builder.Property(s => s.IpAddress).HasColumnName("ip_address");
            builder.Property(s => s.UserAgent).HasColumnName("user_agent");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<User>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("id");
            builder.Property(u => u.Email).HasColumnName("email");
            builder.Property(u => u.DisplayName).HasColumnName("display_name");
            builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>();
            builder.Property(u => u.AssignedBranches).HasColumnName("assigned_branches");
            builder.Property(u => u.IsActive).HasColumnName("is_active");
            builder.Property(u => u.CreatedDate).HasColumnName("created_date");
            builder.Property(u => u.PasswordHash).HasColumnName("password_hash");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<Transaction>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.Type).HasColumnName("type").HasConversion<string>();
            builder.Property(t => t.Category).HasColumnName("category");
            builder.Property(t => t.Description).HasColumnName("description");
            builder.Property(t => t.Amount).HasColumnName("amount");
            builder.Property(t => t.Date).HasColumnName("transaction_date");
            builder.Property(t => t.Branch).HasColumnName("branch");
            builder.Property(t => t.CreatedBy).HasColumnName("created_by");
            builder.Property(t => t.CreatedByName).HasColumnName("created_by_name");
            builder.Property(t => t.CreatedDate).HasColumnName("created_date");
            builder.Property(t => t.ApprovalStatus).HasColumnName("status").HasConversion<string>();
            builder.Property(t => t.ApprovedBy).HasColumnName("approved_by");
            builder.Property(t => t.ApprovedDate).HasColumnName("approved_date");
            builder.Property(t => t.AttachmentIds).HasColumnName("attachment_ids");
            builder.Property(t => t.RelatedUserId).HasColumnName("related_user_id");
            builder.Property(t => t.CarId).HasColumnName("car_id");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("audit_logs");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.UserId).HasColumnName("user_id");
            builder.Property(a => a.UserName).HasColumnName("user_name");
            builder.Property(a => a.Action).HasColumnName("action").HasConversion<string>();
            builder.Property(a => a.Entity).HasColumnName("entity");
            builder.Property(a => a.EntityId).HasColumnName("entity_id");
            builder.Property(a => a.OldValue).HasColumnName("old_value");
            builder.Property(a => a.NewValue).HasColumnName("new_value");
            builder.Property(a => a.Timestamp).HasColumnName("timestamp");
            builder.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
            builder.Property(a => a.ActingAsUserId).HasColumnName("acting_as_user_id");
            builder.Property(a => a.ImpersonationSessionId).HasColumnName("impersonation_session_id");
            builder.Property(a => a.Metadata).HasColumnName("metadata");
            // Append-only — no updates, so no concurrency token needed.
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("branches");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnName("id");
            builder.Property(b => b.Name).HasColumnName("name");
            builder.Property(b => b.Code).HasColumnName("code");
            builder.Property(b => b.Address).HasColumnName("address");
            builder.Property(b => b.IsActive).HasColumnName("is_active");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<Attachment>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("attachments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.TransactionId).HasColumnName("transaction_id");
            builder.Property(a => a.FileName).HasColumnName("file_name");
            builder.Property(a => a.ContentType).HasColumnName("content_type");
            builder.Property(a => a.SizeBytes).HasColumnName("size_bytes");
            builder.Property(a => a.BlobUrl).HasColumnName("storage_path");
            builder.Property(a => a.UploadedBy).HasColumnName("uploaded_by");
            builder.Property(a => a.UploadedDate).HasColumnName("uploaded_date");
        });

        modelBuilder.Entity<PettyCashRequest>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("petty_cash_requests");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.Amount).HasColumnName("amount");
            builder.Property(p => p.Reason).HasColumnName("reason");
            builder.Property(p => p.Branch).HasColumnName("branch");
            builder.Property(p => p.RequestedBy).HasColumnName("requested_by");
            builder.Property(p => p.RequestedByName).HasColumnName("requested_by_name");
            builder.Property(p => p.RequestedDate).HasColumnName("requested_date");
            builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>();
            builder.Property(p => p.ApprovedBy).HasColumnName("approved_by");
            builder.Property(p => p.ApprovedDate).HasColumnName("approved_date");
            builder.Property(p => p.LinkedTransactionId).HasColumnName("linked_transaction_id");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<Car>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("cars");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.Branch).HasColumnName("branch");
            builder.Property(c => c.Client).HasColumnName("client");
            builder.Property(c => c.Type).HasColumnName("type");
            builder.Property(c => c.Model).HasColumnName("model");
            builder.Property(c => c.PlateNumber).HasColumnName("plate_number");
            builder.Property(c => c.MonthlyBill).HasColumnName("monthly_bill");
            builder.Property(c => c.InitialDebt).HasColumnName("initial_debt");
            builder.Property(c => c.ContractStartDate).HasColumnName("contract_start_date");
            builder.Property(c => c.ContractDurationMonths).HasColumnName("contract_duration_months");
            builder.Property(c => c.Notes).HasColumnName("notes");
            builder.Property(c => c.IsActive).HasColumnName("is_active");
            builder.Property(c => c.CreatedBy).HasColumnName("created_by");
            builder.Property(c => c.CreatedDate).HasColumnName("created_date");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<Invoice>(builder =>
        {
            ConfigureTenant(builder);
            builder.ToTable("invoices");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).HasColumnName("id");
            builder.Property(i => i.Type).HasColumnName("type").HasConversion<string>();
            builder.Property(i => i.Branch).HasColumnName("branch");
            builder.Property(i => i.ClientName).HasColumnName("client_name");
            builder.Property(i => i.InvoiceDate).HasColumnName("invoice_date");
            builder.Property(i => i.CarId).HasColumnName("car_id");
            builder.Property(i => i.MonthlyBill).HasColumnName("monthly_bill");
            builder.Property(i => i.DriverName).HasColumnName("driver_name");
            builder.Property(i => i.WageDeposit).HasColumnName("wage_deposit");
            builder.Property(i => i.Fee).HasColumnName("fee");
            builder.Property(i => i.TaxScheme).HasColumnName("tax_scheme").HasConversion<string>();
            builder.Property(i => i.PpnAmount).HasColumnName("ppn_amount");
            builder.Property(i => i.Pph23Amount).HasColumnName("pph23_amount");
            builder.Property(i => i.TotalAmount).HasColumnName("total_amount");
            builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>();
            builder.Property(i => i.PaidDate).HasColumnName("paid_date");
            builder.Property(i => i.LinkedIncomeTransactionId).HasColumnName("linked_income_transaction_id");
            builder.Property(i => i.LinkedExpenseTransactionId).HasColumnName("linked_expense_transaction_id");
            builder.Property(i => i.CreatedBy).HasColumnName("created_by");
            builder.Property(i => i.CreatedByName).HasColumnName("created_by_name");
            builder.Property(i => i.CreatedDate).HasColumnName("created_date");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<Feedback>(builder =>
        {
            builder.ToTable("feedback");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).HasColumnName("id");
            builder.Property(f => f.Message).HasColumnName("message");
            builder.Property(f => f.ImageUrl).HasColumnName("image_url");
            builder.Property(f => f.Severity).HasColumnName("severity").HasConversion<string>();
            builder.Property(f => f.SubmittedBy).HasColumnName("submitted_by");
            builder.Property(f => f.SubmittedByName).HasColumnName("submitted_by_name");
            builder.Property(f => f.SubmittedDate).HasColumnName("submitted_date");
            builder.Property(f => f.AppVersion).HasColumnName("app_version");
            ConfigureXminConcurrencyToken(builder);
        });

        modelBuilder.Entity<ReleaseNote>(builder =>
        {
            builder.ToTable("release_notes");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id");
            builder.Property(r => r.Version).HasColumnName("version");
            builder.Property(r => r.Title).HasColumnName("title");
            builder.Property(r => r.Type).HasColumnName("type").HasConversion<string>();
            builder.Property(r => r.Notes).HasColumnName("notes");
            builder.Property(r => r.PublishedBy).HasColumnName("published_by");
            builder.Property(r => r.PublishedByName).HasColumnName("published_by_name");
            builder.Property(r => r.PublishedDate).HasColumnName("published_date");
            ConfigureXminConcurrencyToken(builder);
        });
    }

    /// <summary>
    /// Maps the tenant_id column and installs the site filter. Every tenant-scoped
    /// entity goes through here so none can be forgotten; bypassing it requires an
    /// explicit IgnoreQueryFilters() at the call site (see the *AnyTenant repository
    /// methods), which is greppable.
    /// </summary>
    private void ConfigureTenant<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantEntity
    {
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenancy();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenancy();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// New rows are stamped with the current site unless the caller named one explicitly
    /// (site creation and seeding do). An existing row's site can never be changed
    /// through EF. Rows without any site are only legitimate for the Application
    /// Admin's own user and for app-level audit entries.
    /// </summary>
    private void ApplyTenancy()
    {
        var current = CurrentTenantId;
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                    {
                        if (!string.IsNullOrEmpty(current))
                        {
                            entry.Entity.TenantId = current;
                        }
                        else if (entry.Entity is not User and not AuditLog)
                        {
                            throw new InvalidOperationException(
                                $"Cannot save a {entry.Entity.GetType().Name} without a site (tenant).");
                        }
                    }
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.TenantId).IsModified = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Maps Postgres's built-in per-row `xmin` system column as a shadow concurrency
    /// token, so EF Core throws DbUpdateConcurrencyException on a stale UPDATE without
    /// needing an explicit version column anywhere in the schema.
    /// </summary>
    private static void ConfigureXminConcurrencyToken<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
