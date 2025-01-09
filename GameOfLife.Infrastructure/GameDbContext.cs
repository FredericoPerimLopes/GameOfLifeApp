using GameOfLife.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOfLife.Repository;

public class GameDbContext : DbContext
{
    public DbSet<Board> Boards { get; set; }

    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("Boards");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Cells)
                .HasConversion(
                    v => string.Join(";", v.Select(row => string.Join(",", row))),
                    v => ConvertStringTo2DArray(v))
                .IsRequired();
            entity.Property(b => b.CreatedAt).IsRequired();
            entity.Property(b => b.Generation).IsRequired();
        });
    }

    private static int[][] ConvertStringTo2DArray(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Array.Empty<int[]>();

        var rows = value.Split(';');
        var array = new int[rows.Length][];
        
        for (int y = 0; y < rows.Length; y++)
        {
            var cols = rows[y].Split(',');
            array[y] = new int[cols.Length];
            
            for (int x = 0; x < cols.Length; x++)
            {
                array[y][x] = int.Parse(cols[x]);
            }
        }
        return array;
    }
}
