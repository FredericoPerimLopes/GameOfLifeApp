namespace GameOfLife.Models;

public class Board
{
    public Guid Id { get; set; }
    public required int[][] Cells { get; set; }
    public int Width => Cells?.Length > 0 ? Cells[0].Length : 0;
    public int Height => Cells?.Length ?? 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
    public int Generation { get; set; } = 0;
    public bool IsFinalState { get; set; } = false;
}
