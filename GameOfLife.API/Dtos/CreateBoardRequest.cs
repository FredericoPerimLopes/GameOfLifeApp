namespace GameOfLife.API.Dto
{
    public class CreateBoardRequest
    {
        public int[][]? InitialState { get; set; }
    }
}
