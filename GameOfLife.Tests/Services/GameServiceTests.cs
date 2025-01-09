using GameOfLife.Models;
using GameOfLife.Services.Services;
using GameOfLife.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

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
        var board = new Board { Id = Guid.NewGuid(), Cells = initialState };
        
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Board>()))
            .Callback<Board>(b => board = b)
            .Returns(Task.CompletedTask);
        
        var service = new GameService(_mockRepository.Object);

        // Act
        var boardId = await service.CreateBoardAsync(initialState);

        // Assert
        Assert.NotEqual(Guid.Empty, boardId);
        _mockRepository.Verify(r => r.AddAsync(It.Is<Board>(b => 
            b.Cells == initialState && 
            b.Generation == 0 &&
            b.IsFinalState == false)), 
            Times.Once);
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
        
        var board = new Board { Id = Guid.NewGuid(), Cells = initialState };
        
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
        
        var board = new Board { Id = Guid.NewGuid(), Cells = initialState };
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
        
        var board = new Board { Id = Guid.NewGuid(), Cells = initialState };
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

}
