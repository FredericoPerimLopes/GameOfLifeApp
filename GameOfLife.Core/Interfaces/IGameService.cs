using GameOfLife.Models;

namespace GameOfLife.Services.Interfaces;

public interface IGameService
{
    Task<Guid> CreateBoardAsync(int[][] initialState);
    Task<Board> GetNextStateAsync(Guid boardId);
    Task<Board> GetStateAfterGenerationsAsync(Guid boardId, int generations);
    Task<Board> GetFinalStateAsync(Guid boardId, int maxGenerations = 1000);
}
