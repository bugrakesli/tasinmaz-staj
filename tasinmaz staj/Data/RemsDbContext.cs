using Microsoft.EntityFrameworkCore;

public class RemsDbContext : DbContext
{
    public RemsDbContext(DbContextOptions<RemsDbContext> options) : base(options) { }

    public DbSet<Il> Iller { get; set; }
    public DbSet<Ilce> Ilceler { get; set; }
    public DbSet<Mahalle> Mahalleler { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<GeometryResult> GeometryResults { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<RevokedToken> RevokedTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Property geometrisi
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasIndex(x => x.ParselNo);
            entity.HasIndex(x => x.AdaNo);
            entity.Property(x => x.Geometry)
                .HasColumnType("geometry(Polygon,4326)")
                .IsRequired(false);
        });

        // Union sonuclari: D ve E
        modelBuilder.Entity<GeometryResult>(entity =>
        {
            entity.Property(x => x.Label)
                .HasMaxLength(1)
                .IsRequired();

            entity.Property(x => x.Wkt)
                .IsRequired();

            entity.Property(x => x.SurfaceArea)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();
        });

        // Log ekrani/exportu her zaman Timestamp'e gore siraliyor (LogService.
        // BuildFilteredQuery) ve sik filtrelenen UserId/Status alanlari var;
        // index olmadan tablo buyudukce sorgu/siralama yavasliyor.
        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.PasswordResetTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Her istekte blacklist kontrolu Jti uzerinden yapiliyor (bkz.
        // Startup.cs OnTokenValidated); bu yuzden Jti'nin index'li ve
        // benzersiz olmasi onemli.
        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.HasIndex(x => x.Jti).IsUnique();
            entity.HasIndex(x => x.ExpiresAt);
        });
    }
}