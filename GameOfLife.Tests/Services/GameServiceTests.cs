using GameOfLife.Models;
using GameOfLife.Services.Services;
using GameOfLife.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using GameOfLife.Services;

namespace GameOfLife.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IBoardRepository> _mockRepository;

    public GameServiceTests()
    {
        _mockRepository = new Mock<IBoardRepository>();
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldCreateNewBoard()
    {
        // Arrange
        var initialState = new int[][]
        {
            new int[] { 0, 1 },
            new int[] { 1, 0 }
        };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 3,
            Height = 3,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Board>()))
            .Callback<Board>(b => board = b)
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var boardId = await service.CreateBoardAsync(initialState);

        // Assert
        Assert.NotEqual(Guid.Empty, boardId);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Board>(b =>
            b.Width == initialState[0].Length &&
            b.Height == initialState.Length &&
            b.Generation == 0 &&
            b.IsFinalState == false &&
            b.LiveCells.SetEquals(GetExpectedLiveCells(initialState)))),
            Times.Once);
    }

    private HashSet<(int x, int y)> GetExpectedLiveCells(int[][] initialState)
    {
        var liveCells = new HashSet<(int x, int y)>();
        for (int y = 0; y < initialState.Length; y++)
        {
            for (int x = 0; x < initialState[y].Length; x++)
            {
                if (initialState[y][x] == 1)
                {
                    liveCells.Add((x, y));
                }
            }
        }
        return liveCells;
    }

    [Fact]
    public async Task GetNextStateAsync_ShouldReturnNextGeneration()
    {
        // Arrange
        var initialState = new int[][]
        {
            new int[] { 0, 1, 0 },
            new int[] { 0, 1, 0 },
            new int[] { 0, 1, 0 }
        };
        var expectedState = new int[][]
        {
            new int[] { 0, 0, 0 },
            new int[] { 1, 1, 1 },
            new int[] { 0, 0, 0 }
        };

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 3,
            Height = 3,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetNextStateAsync(board.Id);

        // Assert
        Assert.Equal(expectedState, result.Cells);
        Assert.Equal(1, result.Generation);
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_ShouldReturnCorrectState()
    {
        // Arrange
        // Use a block pattern which is stable
        var initialState = new int[][]
        {
            new int[] { 0, 0, 0, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 0, 0, 0 }
        };
        var expectedState = new int[][]
        {
            new int[] { 0, 0, 0, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 0, 0, 0 }
        };

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 4,
            Height = 4,
            Cells = initialState
        };
        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetStateAfterGenerationsAsync(board.Id, 1);

        // Assert
        Assert.Equal(expectedState, result.Cells);
        Assert.Equal(1, result.Generation);
    }

    [Fact]
    public async Task GetFinalStateAsync_ShouldReturnFinalState()
    {
        // Arrange
        // Use a block pattern which is stable
        var initialState = new int[][]
        {
            new int[] { 0, 0, 0, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 0, 0, 0 }
        };
        var expectedState = new int[][]
        {
            new int[] { 0, 0, 0, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 1, 1, 0 },
            new int[] { 0, 0, 0, 0 }
        };

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 4,
            Height = 4,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetFinalStateAsync(board.Id);

        // Assert
        Assert.Equal(expectedState, result.Cells);
        Assert.True(result.IsFinalState);
    }

    [Fact]
    public async Task GetNextStateAsync_ShouldIncrementGeneration()
    {
        // Arrange
        var initialState = new int[][] { new int[] { 0, 1 }, new int[] { 1, 0 } };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 2,
            Height = 2,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetNextStateAsync(board.Id);

        // Assert
        Assert.Equal(1, result.Generation);
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_ShouldIncrementCorrectGenerations()
    {
        // Arrange
        var initialState = new int[][] { new int[] { 0, 1 }, new int[] { 1, 0 } };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 2,
            Height = 2,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetStateAfterGenerationsAsync(board.Id, 5);

        // Assert
        Assert.Equal(5, result.Generation);
    }

    [Fact]
    public async Task GetFinalStateAsync_ShouldIncrementCorrectGenerations()
    {
        // Arrange
        var initialState = new int[][]
        {
            new int[] { 0, 0, 0 },
            new int[] { 1, 1, 1 },
            new int[] { 0, 0, 0 }
        };
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = 3,
            Height = 3,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetFinalStateAsync(board.Id);

        // Assert
        Assert.True(result.Generation > 0);
        Assert.True(result.IsFinalState);
    }

    [Fact]
    public async Task GetBoard_ShouldThrowException_WhenBoardNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException());

        var service = new GameService(_mockRepository.Object);
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetNextStateAsync(nonExistentId));
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldThrowForNullInitialState()
    {
        // Arrange
        var service = new GameService(_mockRepository.Object);
        int[][]? nullState = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateBoardAsync(nullState!));
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldThrowForEmptyInitialState()
    {
        // Arrange
        var service = new GameService(_mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBoardAsync(Array.Empty<int[]>()));
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldThrowForInvalidCellValues()
    {
        // Arrange
        var invalidState = new int[][]
        {
            new int[] { 0, 2 },
            new int[] { -1, 1 }
        };
        var service = new GameService(_mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBoardAsync(invalidState));
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldHandleSingleCellBoard()
    {
        // Arrange
        var initialState = new int[][] { new int[] { 1 } };
        var service = new GameService(_mockRepository.Object);

        // Act
        var boardId = await service.CreateBoardAsync(initialState);

        // Assert
        Assert.NotEqual(Guid.Empty, boardId);
    }

    [Fact]
    public async Task GetNextStateAsync_ShouldHandleEmptyBoard()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Cells = Array.Empty<int[]>() };
        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        var service = new GameService(_mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetNextStateAsync(board.Id));
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_ShouldThrowForNegativeGenerations()
    {
        // Arrange
        var service = new GameService(_mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetStateAfterGenerationsAsync(Guid.NewGuid(), -1));
    }

    [Fact]
    public async Task GetFinalStateAsync_ShouldThrowForInvalidMaxGenerations()
    {
        // Arrange
        var service = new GameService(_mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetFinalStateAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task CreateBoardAsync_ShouldHandleLargeGrid()
    {
        // Arrange
        const int size = 1000;
        var initialState = GenerateLargeGrid(size);
        var service = new GameService(_mockRepository.Object);

        // Act
        var boardId = await service.CreateBoardAsync(initialState);

        // Assert
        Assert.NotEqual(Guid.Empty, boardId);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Board>(b =>
            b.Width == size &&
            b.Height == size &&
            b.Generation == 0 &&
            b.IsFinalState == false)),
            Times.Once);
    }

    [Fact]
    public async Task GetNextStateAsync_ShouldHandleLargeGrid()
    {
        // Arrange
        const int size = 1000;
        var initialState = GenerateLargeGrid(size);
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = size,
            Height = size,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetNextStateAsync(board.Id);

        // Assert
        Assert.Equal(size, result.Width);
        Assert.Equal(size, result.Height);
        Assert.Equal(1, result.Generation);
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_ShouldHandleLargeGrid()
    {
        // Arrange
        const int size = 1000;
        var initialState = GenerateLargeGrid(size);
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = size,
            Height = size,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetStateAfterGenerationsAsync(board.Id, 10);

        // Assert
        Assert.Equal(size, result.Width);
        Assert.Equal(size, result.Height);
        Assert.Equal(10, result.Generation);
    }

    [Fact]
    public async Task GetFinalStateAsync_ShouldHandleLargeGrid()
    {
        // Arrange
        const int size = 1000;
        // Use a known stable pattern (empty grid with a single block)
        var initialState = new int[size][];
        for (int y = 0; y < size; y++)
        {
            initialState[y] = new int[size];
        }

        // Add a stable block pattern in the center
        int center = size / 2;
        initialState[center][center] = 1;
        initialState[center][center + 1] = 1;
        initialState[center + 1][center] = 1;
        initialState[center + 1][center + 1] = 1;

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = size,
            Height = size,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetFinalStateAsync(board.Id, 1000);

        // Assert
        Assert.Equal(size, result.Width);
        Assert.Equal(size, result.Height);
        Assert.Equal(1, result.Generation); // Should stabilize immediately
        Assert.True(result.IsFinalState);

        // Verify the stable pattern remains
        var finalCells = result.Cells;
        Assert.Equal(1, finalCells[center][center]);
        Assert.Equal(1, finalCells[center][center + 1]);
        Assert.Equal(1, finalCells[center + 1][center]);
        Assert.Equal(1, finalCells[center + 1][center + 1]);
    }

    [Fact]
    public async Task GetFinalStateAsync_ShouldHandleLargeRandomGrid()
    {
        // Arrange
        const int size = 100;
        var initialState = GenerateStablePattern(size); // Use a known stable pattern
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Width = size,
            Height = size,
            Cells = initialState
        };

        _mockRepository.Setup(r => r.GetByIdAsync(board.Id))
            .ReturnsAsync(board);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        var service = new GameService(_mockRepository.Object);

        // Act
        var result = await service.GetFinalStateAsync(board.Id, 10);

        // Assert
        Assert.Equal(size, result.Width);
        Assert.Equal(size, result.Height);
        Assert.True(result.Generation > 0);
        Assert.True(result.IsFinalState);

        // Verify stability by checking one generation
        var nextState = GameRules.CalculateNextGeneration(result.Cells);
        Assert.Equal(result.Cells, nextState);
    }

    private int[][] GenerateStablePattern(int size)
    {
        var grid = new int[size][];
        for (int y = 0; y < size; y++)
        {
            grid[y] = new int[size];
            for (int x = 0; x < size; x++)
            {
                // Create a stable block pattern every 4 cells
                grid[y][x] = (x % 4 < 2 && y % 4 < 2) ? 1 : 0;
            }
        }
        return grid;
    }

    private int[][] GenerateLargeGrid(int size)
    {
        var grid = new int[size][];
        var random = new Random();

        for (int y = 0; y < size; y++)
        {
            grid[y] = new int[size];
            for (int x = 0; x < size; x++)
            {
                // Create a sparse grid with about 10% live cells
                grid[y][x] = random.Next(10) == 0 ? 1 : 0;
            }
        }
        return grid;
    }
}
