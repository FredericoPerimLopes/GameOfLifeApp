using GameOfLife.Services;
using Xunit;

namespace GameOfLife.Tests.Services;

public class GameRulesTests
{
    [Fact]
    public void CalculateNextGeneration_ShouldReturnCorrectNextState()
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

        // Act
        var result = GameRules.CalculateNextGeneration(initialState);

        // Assert
        Assert.Equal(expectedState, result);
    }

    [Fact]
    public void CountLiveNeighbors_ShouldReturnCorrectCount()
    {
        // Arrange
        var board = new int[][]
        {
            new int[] { 1, 0, 1 },
            new int[] { 0, 1, 0 },
            new int[] { 1, 0, 1 }
        };

        // Act & Assert
        Assert.Equal(4, GameRules.CountLiveNeighbors(board, 1, 1)); // Center cell
        Assert.Equal(1, GameRules.CountLiveNeighbors(board, 0, 0)); // Corner cell
        Assert.Equal(3, GameRules.CountLiveNeighbors(board, 1, 0)); // Edge cell
    }

    [Fact]
    public void CountLiveNeighbors_ShouldHandleEmptyBoard()
    {
        // Arrange
        var board = Array.Empty<int[]>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.CountLiveNeighbors(board, 0, 0));
        Assert.Contains("X coordinate is outside board boundaries", ex.Message);
    }

    [Fact]
    public void CountLiveNeighbors_ShouldHandleSingleCellBoard()
    {
        // Arrange
        var board = new int[][] { new int[] { 1 } };

        // Act & Assert
        Assert.Equal(0, GameRules.CountLiveNeighbors(board, 0, 0));
    }

    [Fact]
    public void CountLiveNeighbors_ShouldHandleRectangularBoard()
    {
        // Arrange
        var board = new int[][]
        {
            new int[] { 1, 0, 1, 0 },
            new int[] { 0, 1, 0, 1 }
        };

        // Act & Assert
        Assert.Equal(2, GameRules.CountLiveNeighbors(board, 1, 0)); // Second row, first column
        Assert.Equal(2, GameRules.CountLiveNeighbors(board, 1, 1)); // Second row, second column
    }

    [Fact]
    public void CountLiveNeighbors_ShouldThrowForInvalidCoordinates()
    {
        // Arrange
        var board = new int[][]
        {
            new int[] { 1, 0 },
            new int[] { 0, 1 }
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.CountLiveNeighbors(board, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.CountLiveNeighbors(board, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.CountLiveNeighbors(board, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GameRules.CountLiveNeighbors(board, 0, 2));
    }

    [Theory]
    [InlineData(1, 2, 1)] // Live cell with 2 neighbors stays alive
    [InlineData(1, 3, 1)] // Live cell with 3 neighbors stays alive
    [InlineData(1, 1, 0)] // Live cell with 1 neighbor dies
    [InlineData(1, 4, 0)] // Live cell with 4 neighbors dies
    [InlineData(0, 3, 1)] // Dead cell with 3 neighbors becomes alive
    [InlineData(0, 2, 0)] // Dead cell with 2 neighbors stays dead
    public void ApplyRules_ShouldReturnCorrectState(int currentState, int liveNeighbors, int expected)
    {
        // Act
        var result = GameRules.ApplyRules(currentState, liveNeighbors);

        // Assert
        Assert.Equal(expected, result);
    }
}
