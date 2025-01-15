using System.Collections.Concurrent;

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

        if (currentState.Length == 0 || currentState[0].Length == 0)
        {
            throw new ArgumentException("Board cannot have zero dimensions");
        }

        // Convert to sparse, calculate, then convert back
        var liveCells = ConvertToSparse(currentState);
        var nextGen = CalculateNextGenerationSparse(liveCells, currentState[0].Length, currentState.Length);
        return ConvertToDense(nextGen, currentState[0].Length, currentState.Length);
    }

    /// <summary>
    /// Calculates the next generation of live cells using a sparse representation.
    /// </summary>
    /// <param name="liveCells">Set of currently live cell coordinates (x,y)</param>
    /// <param name="width">Width of the game board</param>
    /// <param name="height">Height of the game board</param>
    /// <returns>New set of live cell coordinates for the next generation</returns>
    private static HashSet<(int x, int y)> CalculateNextGenerationSparse(
        HashSet<(int x, int y)> liveCells, int width, int height)
    {
        var newLiveCells = new HashSet<(int x, int y)>();
        const int blockSize = 64; // Cache line friendly size

        // Group cells by spatial blocks to improve cache locality
        // This groups cells into 64x64 blocks based on their coordinates
        var cellsByBlock = liveCells
            .GroupBy(c => (c.x / blockSize, c.y / blockSize))
            .ToList();

        // Use thread-local storage for neighbor counts to reduce contention
        var neighborCounts = new ConcurrentDictionary<(int x, int y), int>();

        // Process each block in parallel using thread-local storage
        Parallel.ForEach(cellsByBlock,
            // Initialize thread-local storage for each thread
            () => new Dictionary<(int x, int y), int>(),
            (block, state, localCounts) =>
            {
                // Process each cell in the current block
                foreach (var (x, y) in block)
                {
                    // Process all 8 neighbors using unrolled loop for better performance
                    // The order is optimized for cache locality:
                    // 1. Top-left    2. Top     3. Top-right
                    // 4. Left       5. Right
                    // 6. Bottom-left 7. Bottom 8. Bottom-right
                    ProcessNeighbor(x - 1, y - 1);
                    ProcessNeighbor(x, y - 1);
                    ProcessNeighbor(x + 1, y - 1);
                    ProcessNeighbor(x - 1, y);
                    ProcessNeighbor(x + 1, y);
                    ProcessNeighbor(x - 1, y + 1);
                    ProcessNeighbor(x, y + 1);
                    ProcessNeighbor(x + 1, y + 1);
                }

                // Merge local counts into global dictionary
                foreach (var kvp in localCounts)
                {
                    neighborCounts.AddOrUpdate(kvp.Key, kvp.Value, (_, count) => count + kvp.Value);
                }

                return localCounts;

                void ProcessNeighbor(int nx, int ny)
                {
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        localCounts[(nx, ny)] = localCounts.GetValueOrDefault((nx, ny), 0) + 1;
                    }
                }
            },
            localCounts => { });

        // Parallel processing of neighbor counts
        var liveCellsSet = new ConcurrentBag<(int x, int y)>();
        Parallel.ForEach(neighborCounts, pair =>
        {
            var (cell, count) = pair;
            if (count == NEIGHBORS_TO_REPRODUCE ||
                (count >= MIN_NEIGHBORS_TO_SURVIVE &&
                 count <= MAX_NEIGHBORS_TO_SURVIVE &&
                 liveCells.Contains(cell)))
            {
                liveCellsSet.Add(cell);
            }
        });

        // Convert concurrent bag to hash set
        foreach (var cell in liveCellsSet)
        {
            newLiveCells.Add(cell);
        }

        return newLiveCells;
    }

    /// <summary>
    /// Converts a dense grid representation to a sparse set of live cell coordinates.
    /// </summary>
    /// <param name="grid">2D array representing the game board (1=alive, 0=dead)</param>
    /// <returns>Set of live cell coordinates (x,y)</returns>
    private static HashSet<(int x, int y)> ConvertToSparse(int[][] grid)
    {
        var liveCells = new HashSet<(int x, int y)>();
        for (int y = 0; y < grid.Length; y++)
        {
            for (int x = 0; x < grid[y].Length; x++)
            {
                if (grid[y][x] == ALIVE)
                {
                    liveCells.Add((x, y));
                }
            }
        }
        return liveCells;
    }

    /// <summary>
    /// Converts a sparse set of live cell coordinates to a dense grid representation.
    /// </summary>
    /// <param name="liveCells">Set of live cell coordinates (x,y)</param>
    /// <param name="width">Width of the game board</param>
    /// <param name="height">Height of the game board</param>
    /// <returns>2D array representing the game board (1=alive, 0=dead)</returns>
    private static int[][] ConvertToDense(HashSet<(int x, int y)> liveCells, int width, int height)
    {
        var grid = new int[height][];
        const int blockSize = 64; // Cache line friendly size

        // Process the grid in vertical blocks to improve cache locality
        Parallel.For(0, (height + blockSize - 1) / blockSize, blockY =>
        {
            // Calculate the vertical range for this block
            int yStart = blockY * blockSize;
            int yEnd = Math.Min(yStart + blockSize, height);

            // Process each row in the vertical block
            for (int y = yStart; y < yEnd; y++)
            {
                grid[y] = new int[width];

                // Process each row in horizontal blocks for better cache utilization
                for (int xBlock = 0; xBlock < width; xBlock += blockSize)
                {
                    // Calculate the horizontal range for this block
                    int xEnd = Math.Min(xBlock + blockSize, width);

                    // Process each cell in the current block
                    for (int x = xBlock; x < xEnd; x++)
                    {
                        // Set cell state based on presence in liveCells set
                        grid[y][x] = liveCells.Contains((x, y)) ? ALIVE : DEAD;
                    }
                }
            }
        });

        return grid;
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

    /// <summary>
    /// Calculates the bounds for a cell's neighbors, respecting board edges.
    /// </summary>
    /// <param name="x">X coordinate of the cell</param>
    /// <param name="y">Y coordinate of the cell</param>
    /// <param name="width">Width of the game board</param>
    /// <param name="height">Height of the game board</param>
    /// <returns>Tuple containing min/max x/y bounds for neighbors</returns>
    private static (int minX, int maxX, int minY, int maxY) GetNeighborBounds(int x, int y, int width, int height)
    {
        return (
            minX: Math.Max(x - 1, 0),
            maxX: Math.Min(width - 1, x + 1),
            minY: Math.Max(y - 1, 0),
            maxY: Math.Min(height - 1, y + 1)
        );
    }

    /// <summary>
    /// Counts live neighbors within specified bounds for a given cell.
    /// </summary>
    /// <param name="board">Current state of the game board</param>
    /// <param name="cellX">X coordinate of the cell</param>
    /// <param name="cellY">Y coordinate of the cell</param>
    /// <param name="bounds">Neighbor bounds (minX, maxX, minY, maxY)</param>
    /// <returns>Number of live neighbors</returns>
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

    /// <summary>
    /// Gets the dimensions of the game board.
    /// </summary>
    /// <param name="board">2D array representing the game board</param>
    /// <returns>Tuple containing width and height of the board</returns>
    private static (int width, int height) GetBoardDimensions(int[][] board)
    {
        if (board == null || board.Length == 0)
            return (0, 0);

        return (board[0].Length, board.Length);
    }

    /// <summary>
    /// Validates that coordinates are within board boundaries.
    /// </summary>
    /// <param name="x">X coordinate to validate</param>
    /// <param name="y">Y coordinate to validate</param>
    /// <param name="width">Width of the game board</param>
    /// <param name="height">Height of the game board</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if coordinates are outside board boundaries</exception>
    private static void ValidateCoordinates(int x, int y, int width, int height)
    {
        if (x < 0 || x >= width)
            throw new ArgumentOutOfRangeException(nameof(x), "X coordinate is outside board boundaries");

        if (y < 0 || y >= height)
            throw new ArgumentOutOfRangeException(nameof(y), "Y coordinate is outside board boundaries");
    }

    /// <summary>
    /// Validates that a cell state is either ALIVE (1) or DEAD (0).
    /// </summary>
    /// <param name="state">Cell state to validate</param>
    /// <returns>True if state is valid, false otherwise</returns>
    private static bool IsValidCellState(int state)
    {
        return state == DEAD || state == ALIVE;
    }

    /// <summary>
    /// Determines if a live cell should survive to the next generation.
    /// </summary>
    /// <param name="liveNeighbors">Number of live neighbors</param>
    /// <returns>True if cell should survive, false otherwise</returns>
    private static bool ShouldSurvive(int liveNeighbors)
    {
        return liveNeighbors >= MIN_NEIGHBORS_TO_SURVIVE &&
               liveNeighbors <= MAX_NEIGHBORS_TO_SURVIVE;
    }

    /// <summary>
    /// Determines if a dead cell should become alive in the next generation.
    /// </summary>
    /// <param name="liveNeighbors">Number of live neighbors</param>
    /// <returns>True if cell should reproduce, false otherwise</returns>
    private static bool ShouldReproduce(int liveNeighbors)
    {
        return liveNeighbors == NEIGHBORS_TO_REPRODUCE;
    }
}
