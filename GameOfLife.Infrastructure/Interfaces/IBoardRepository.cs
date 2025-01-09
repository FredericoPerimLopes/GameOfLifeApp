using GameOfLife.Models;


namespace GameOfLife.Repository.Interfaces;

public interface IBoardRepository : IRepository<Board>
{
    Task<Board?> FindByIdAsync(Guid id);
}
