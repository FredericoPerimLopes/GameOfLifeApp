namespace GameOfLife.Models;

public class Board
{
    public Guid Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    public int Generation { get; set; } = 0;
    public bool IsFinalState { get; set; } = false;

    // Sparse representation
    public HashSet<(int x, int y)> LiveCells { get; set; } = new();

    // Dense representation for compatibility
    public int[][] Cells
    {
        get
        {
            var grid = new int[Height][];
            for (int y = 0; y < Height; y++)
            {
                grid[y] = new int[Width];
                for (int x = 0; x < Width; x++)
                {
                    grid[y][x] = LiveCells.Contains((x, y)) ? 1 : 0;
                }
            }
            return grid;
        }
        set
        {
            LiveCells.Clear();
            for (int y = 0; y < value.Length; y++)
            {
                for (int x = 0; x < value[y].Length; x++)
                {
                    if (value[y][x] == 1)
                    {
                        LiveCells.Add((x, y));
                    }
                }
            }
        }
    }
}
