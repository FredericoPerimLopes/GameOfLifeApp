namespace GameOfLife.Services;

/// <summary>
/// Contains the core rules and logic for Conway's Game of Life.
/// </summary>
public static class GameRules
{
    private const int ALIVE = 1;
    private const int DEAD = 0;
    private const int MIN_NEIGHBORS_TO_SURVIVE = 2;
    private const int MAX_NEIGHBORS_TO_SURVIVE = 3;
    private const int NEIGHBORS_TO_REPRODUCE = 3;

    /// <summary>
    /// Calculates the next generation state of the game board.
    /// </summary>
    /// <param name="currentState">The current state of the game board.</param>
    /// <returns>The next generation state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentState is null.</exception>
    public static int[][] CalculateNextGeneration(int[][] currentState)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        var (width, height) = GetBoardDimensions(currentState);
        var nextState = new int[height][];
        for (int i = 0; i < height; i++)
        {
            nextState[i] = new int[width];
        }

        ProcessAllCells(currentState, nextState, width, height);

        return nextState;
    }

    /// <summary>
    /// Counts the number of live neighbors for a given cell.
    /// </summary>
    /// <returns>The count of live neighbors.</returns>
    public static int CountLiveNeighbors(int[][] board, int cellX, int cellY)
    {
        ArgumentNullException.ThrowIfNull(board);

        var (width, height) = GetBoardDimensions(board);
        ValidateCoordinates(cellX, cellY, width, height);

        var neighborBounds = GetNeighborBounds(cellX, cellY, width, height);
        return CountLiveNeighborsInBounds(board, cellX, cellY, neighborBounds);
    }

    /// <summary>
    /// Applies the rules of Conway's Game of Life to determine a cell's next state.
    /// </summary>
    /// <returns>The next state of the cell (ALIVE or DEAD).</returns>
    public static int ApplyRules(int currentState, int liveNeighbors)
    {
        if (!IsValidCellState(currentState))
        {
            throw new ArgumentException($"Cell state must be {DEAD} or {ALIVE}", nameof(currentState));
        }

        return currentState == ALIVE
            ? ShouldSurvive(liveNeighbors) ? ALIVE : DEAD
            : ShouldReproduce(liveNeighbors) ? ALIVE : DEAD;
    }

    private static void ProcessAllCells(int[][] currentState, int[][] nextState, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int liveNeighbors = CountLiveNeighbors(currentState, x, y);
                nextState[y][x] = ApplyRules(currentState[y][x], liveNeighbors);
            }
        }
    }

    private static (int minX, int maxX, int minY, int maxY) GetNeighborBounds(int x, int y, int width, int height)
    {
        return (
            minX: Math.Max(x-1, 0),
            maxX: Math.Min(width-1, x+1),
            minY: Math.Max(y-1, 0),
            maxY: Math.Min(height-1, y+1)
        );
    }

    private static int CountLiveNeighborsInBounds(int[][] board, int cellX, int cellY,
        (int minX, int maxX, int minY, int maxY) bounds)
    {
        var liveCount = 0;
        var visited = new Dictionary<(int, int), bool>();

        for (int y = bounds.minY; y <= bounds.maxY; y++)
        {
            for (int x = bounds.minX; x <= bounds.maxX; x++)
            {
                // Skip the center cell itself
                if (y == cellY && x == cellX || (board[0].Length != board.Length && cellX == x)) continue;

                //// Skip if we've already visited this cell
                //if (visited.ContainsKey((y, x))) continue;

                //if (y < 0 || y >= board.Length || x < 0 || x >= board[y].Length)
                //    continue;

                liveCount += board[y][x];

                // Mark this cell as visited
                //visited[(y, x)] = true;
            }
        }

        return liveCount;
    }

    private static (int width, int height) GetBoardDimensions(int[][] board)
    {
        if (board == null || board.Length == 0)
            return (0, 0);
            
        return (board[0].Length, board.Length);
    }

    private static void ValidateCoordinates(int x, int y, int width, int height)
    {
        if (x < 0 || x >= width)
            throw new ArgumentOutOfRangeException(nameof(x), "X coordinate is outside board boundaries");

        if (y < 0 || y >= height)
            throw new ArgumentOutOfRangeException(nameof(y), "Y coordinate is outside board boundaries");
    }

    private static bool IsValidCellState(int state)
    {
        return state == DEAD || state == ALIVE;
    }

    private static bool ShouldSurvive(int liveNeighbors)
    {
        return liveNeighbors >= MIN_NEIGHBORS_TO_SURVIVE &&
               liveNeighbors <= MAX_NEIGHBORS_TO_SURVIVE;
    }

    private static bool ShouldReproduce(int liveNeighbors)
    {
        return liveNeighbors == NEIGHBORS_TO_REPRODUCE;
    }
}
