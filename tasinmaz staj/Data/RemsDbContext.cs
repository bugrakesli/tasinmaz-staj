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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure relationships and constraints if needed
        modelBuilder.Entity<Property>(entity =>
        {
            entity.Property(x => x.Geometry)
                .HasColumnType("geometry(Polygon,4326)") // Set the SRID to 4326 for WGS84
                .IsRequired(false); 
        });   
    }
}