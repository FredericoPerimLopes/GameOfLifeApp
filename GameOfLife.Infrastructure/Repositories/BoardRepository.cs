using GameOfLife.Models;
using GameOfLife.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameOfLife.Repository.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly GameDbContext _context;

    public BoardRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task<Board> GetByIdAsync(Guid id)
    {
        var board = await _context.Boards.FindAsync(id);
        if (board == null)
        {
            throw new KeyNotFoundException($"Board with id {id} not found");
        }
        return board;
    }

    public async Task<Board?> FindByIdAsync(Guid id)
    {
        return await _context.Boards.FindAsync(id);
    }

    public async Task<IEnumerable<Board>> GetAllAsync()
    {
        return await _context.Boards.ToListAsync();
    }

    public async Task AddAsync(Board entity)
    {
        await _context.Boards.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Board entity)
    {
        _context.Boards.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var board = await GetByIdAsync(id);
        _context.Boards.Remove(board);
        await _context.SaveChangesAsync();
    }
}
