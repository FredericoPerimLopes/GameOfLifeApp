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
            Cells = initialState,
            Generation = 0
        };

        await _boardRepository.AddAsync(board);
        return board.Id;
    }

    public async Task<Board> GetNextStateAsync(Guid boardId)
    {
        var board = await GetBoard(boardId);
        if (board.IsFinalState) return board;

        if (board.Cells == null || board.Cells.Length == 0)
        {
            throw new InvalidOperationException("Board cells cannot be null or empty");
        }

        var nextState = GameRules.CalculateNextGeneration(board.Cells);
        return await UpdateBoardState(board, nextState);
    }

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

        return await UpdateBoardState(board, currentState);
    }

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

            if (generations >= maxGenerations)
            {
                throw new InvalidOperationException(
                    $"Board did not reach final state after {maxGenerations} generations");
            }
        }
        while (!AreStatesEqual(previousState, currentState));

        return await UpdateBoardState(board, currentState, true);
    }

    private async Task<Board> GetBoard(Guid boardId)
    {
        return await _boardRepository.GetByIdAsync(boardId);
    }

    private async Task<Board> UpdateBoardState(Board board, int[][]? newState, bool isFinal = false)
    {
        if (newState == null)
        {
            throw new ArgumentNullException(nameof(newState), "New state cannot be null");
        }

        board.Cells = newState;
        board.Generation++;
        board.LastUpdated = DateTime.UtcNow;
        board.IsFinalState = isFinal;

        await _boardRepository.UpdateAsync(board);
        return board;
    }

    private static bool AreStatesEqual(int[][]? state1, int[][]? state2)
    {
        if (state1 == null || state2 == null)
            return state1 == state2;
            
        if (state1.Length != state2.Length || 
            (state1.Length > 0 && state1[0].Length != state2[0].Length))
        {
            return false;
        }

        for (int x = 0; x < state1.Length; x++)
        {
            for (int y = 0; y < state1[x].Length; y++)
            {
                if (state1[x][y] != state2[x][y])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
