using GameOfLife.Services.Interfaces;
using GameOfLife.Models;
using GameOfLife.Repository.Interfaces;


namespace GameOfLife.Services.Services;

public class GameService : IGameService
{
    private readonly IBoardRepository _boardRepository;

    public GameService(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    /// <summary>
    /// Creates a new game board with the specified initial state.
    /// </summary>
    /// <param name="initialState">2D array representing the initial state of the board</param>
    /// <returns>Guid of the newly created board</returns>
    /// <exception cref="ArgumentNullException">Thrown if initialState is null</exception>
    /// <exception cref="ArgumentException">Thrown if initialState is empty or contains invalid values</exception>
    public async Task<Guid> CreateBoardAsync(int[][] initialState)
    {
        if (initialState == null)
        {
            throw new ArgumentNullException(nameof(initialState), "Initial state must be provided");
        }

        if (initialState.Length == 0 || initialState[0] == null)
        {
            throw new ArgumentException("Initial state cannot be empty", nameof(initialState));
        }

        // Validate all cell values are either 0 or 1
        foreach (var row in initialState)
        {
            foreach (var cell in row)
            {
                if (cell != 0 && cell != 1)
                {
                    throw new ArgumentException("All cell values must be either 0 or 1", nameof(initialState));
                }
            }
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = initialState[0].Length,
            Height = initialState.Length,
            Cells = initialState, // This will populate LiveCells
            Generation = 0
        };

        await _boardRepository.AddAsync(board);
        return board.Id;
    }

    /// <summary>
    /// Calculates and returns the next state of the specified board.
    /// </summary>
    /// <param name="boardId">Guid of the board to process</param>
    /// <returns>Board object with updated state</returns>
    /// <exception cref="KeyNotFoundException">Thrown if board is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if board state is invalid</exception>
    public async Task<Board> GetNextStateAsync(Guid boardId)
    {
        var board = await GetBoard(boardId);
        if (board.IsFinalState) return board;

        if (board.Cells == null || board.Cells.Length == 0)
        {
            throw new InvalidOperationException("Board cells cannot be null or empty");
        }

        var nextState = GameRules.CalculateNextGeneration(board.Cells);
        board.Generation++; // Increment here
        return await UpdateBoardState(board, nextState);
    }

    /// <summary>
    /// Calculates and returns the state of the board after the specified number of generations.
    /// </summary>
    /// <param name="boardId">Guid of the board to process</param>
    /// <param name="generations">Number of generations to simulate</param>
    /// <returns>Board object with updated state</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if generations is negative</exception>
    /// <exception cref="KeyNotFoundException">Thrown if board is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if board state is invalid</exception>
    public async Task<Board> GetStateAfterGenerationsAsync(Guid boardId, int generations)
    {
        if (generations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generations), "Number of generations must be non-negative");
        }

        var board = await GetBoard(boardId);
        if (board.IsFinalState) return board;

        if (board.Cells == null)
        {
            throw new InvalidOperationException("Board cells cannot be null");
        }

        int[][] currentState = board.Cells;
        for (int i = 0; i < generations; i++)
        {
            currentState = GameRules.CalculateNextGeneration(currentState);
        }

        board.Generation += generations; // Increment total generations
        return await UpdateBoardState(board, currentState);
    }

    /// <summary>
    /// Calculates and returns the final stable state of the board.
    /// </summary>
    /// <param name="boardId">Guid of the board to process</param>
    /// <param name="maxGenerations">Maximum number of generations to simulate</param>
    /// <returns>Board object with final stable state</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxGenerations is less than or equal to 0</exception>
    /// <exception cref="KeyNotFoundException">Thrown if board is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if board state is invalid</exception>
    public async Task<Board> GetFinalStateAsync(Guid boardId, int maxGenerations = 1000)
    {
        if (maxGenerations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGenerations), "Max generations must be greater than 0");
        }

        var board = await GetBoard(boardId);
        if (board.IsFinalState) return board;

        if (board.Cells == null)
        {
            throw new InvalidOperationException("Board cells cannot be null");
        }

        int[][] currentState = board.Cells;
        int[][] previousState;
        int generations = 0;

        do
        {
            previousState = currentState;
            currentState = GameRules.CalculateNextGeneration(currentState);
            generations++;
        }
        while (!AreStatesEqual(previousState, currentState) && generations < maxGenerations);

        board.Generation += generations; // Increment total generations
        return await UpdateBoardState(board, currentState, true);
    }

    /// <summary>
    /// Retrieves a board from the repository.
    /// </summary>
    /// <param name="boardId">Guid of the board to retrieve</param>
    /// <returns>Board object</returns>
    /// <exception cref="KeyNotFoundException">Thrown if board is not found</exception>
    private async Task<Board> GetBoard(Guid boardId)
    {
        return await _boardRepository.GetByIdAsync(boardId);
    }

    /// <summary>
    /// Updates a board's state in the repository.
    /// </summary>
    /// <param name="board">Board object to update</param>
    /// <param name="newState">New state of the board</param>
    /// <param name="isFinal">Whether this is the final stable state</param>
    /// <returns>Updated board object</returns>
    /// <exception cref="ArgumentNullException">Thrown if newState is null</exception>
    private async Task<Board> UpdateBoardState(Board board, int[][]? newState, bool isFinal = false)
    {
        if (newState == null)
        {
            throw new ArgumentNullException(nameof(newState), "New state cannot be null");
        }

        board.Cells = newState;
        board.LastUpdated = DateTime.UtcNow;
        board.IsFinalState = isFinal;

        await _boardRepository.UpdateAsync(board);
        return board;
    }

    private static bool AreStatesEqual(int[][]? state1, int[][]? state2)
    {
        // Handle null cases
        if (state1 == null || state2 == null)
            return state1 == state2;

        // Check if dimensions match
        if (state1.Length != state2.Length ||
            (state1.Length > 0 && state1[0].Length != state2[0].Length))
        {
            return false;
        }

        const int blockSize = 64; // Cache line friendly size

        // Process the grid in blocks to improve cache locality
        for (int yBlock = 0; yBlock < state1.Length; yBlock += blockSize)
        {
            // Calculate vertical block boundaries
            int yEnd = Math.Min(yBlock + blockSize, state1.Length);

            // Process each vertical block in horizontal blocks
            for (int xBlock = 0; xBlock < state1[0].Length; xBlock += blockSize)
            {
                // Calculate horizontal block boundaries
                int xEnd = Math.Min(xBlock + blockSize, state1[0].Length);

                // Compare cells within the current block
                for (int y = yBlock; y < yEnd; y++)
                {
                    for (int x = xBlock; x < xEnd; x++)
                    {
                        // If any cell differs, states are not equal
                        if (state1[y][x] != state2[y][x])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }
}
