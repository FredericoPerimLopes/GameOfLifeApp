# Game of Life API

This is a REST API implementation of Conway's Game of Life, built with .NET 7 and SQLite.

## Features

- Create new game boards with custom initial states
- Get next generation state of a board
- Get state after N generations
- Get final stable state (with max generation limit)
- Persistent storage using SQLite
- RESTful API endpoints
- Unit tests for core game logic

## Technologies

- .NET 7
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- xUnit (for testing)
- Swagger/OpenAPI

## API Endpoints

- `POST /api/game` - Create new board
- `GET /api/game/{id}` - Get current state
- `GET /api/game/{id}/next` - Get next generation
- `GET /api/game/{id}/generations/{n}` - Get state after N generations
- `GET /api/game/{id}/final` - Get final stable state

## Getting Started

### Prerequisites

- .NET 7 SDK
- SQLite

### Installation

1. Clone the repository
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Run database migrations:
   ```bash
   dotnet ef database update
   ```
4. Start the API:
   ```bash
   dotnet run --project GameOfLife.API
   ```

### Running Tests

Run all tests with:
```bash
dotnet test
```

## Project Structure

- `GameOfLife.API` - Web API project
- `GameOfLife.Core` - Core domain and services
- `GameOfLife.Models` - Data models
- `GameOfLife.Infrastructure` - Database and repository implementation
- `GameOfLife.Tests` - Unit tests

## Database

The API uses SQLite for persistent storage. The database file `GameOfLife.db` will be created in the project directory on first run.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
