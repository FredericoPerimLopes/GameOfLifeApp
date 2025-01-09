using GameOfLife.API.Dto;
using GameOfLife.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost("boards")]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
    {
        if (request?.InitialState == null || request.InitialState.Length == 0 || request.InitialState[0] == null)
        {
            return BadRequest("Initial state must be provided and cannot be empty");
        }

        var boardId = await _gameService.CreateBoardAsync(request.InitialState);
        return Ok(new { BoardId = boardId });
    }

    [HttpGet("boards/{boardId}/next")]
    public async Task<IActionResult> GetNextState(Guid boardId)
    {
        var board = await _gameService.GetNextStateAsync(boardId);
        return Ok(board);
    }

    [HttpGet("boards/{boardId}/generations/{generations}")]
    public async Task<IActionResult> GetStateAfterGenerations(Guid boardId, int generations)
    {
        var board = await _gameService.GetStateAfterGenerationsAsync(boardId, generations);
        return Ok(board);
    }

    [HttpGet("boards/{boardId}/final")]
    public async Task<IActionResult> GetFinalState(Guid boardId, [FromQuery] int maxGenerations = 1000)
    {
        var board = await _gameService.GetFinalStateAsync(boardId, maxGenerations);
        return Ok(board);
    }
}
