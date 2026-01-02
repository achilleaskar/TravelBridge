namespace TravelBridge.API.Contracts
{
    public class BoardTextBase
    {

        public List<Board> Boards { get; set; }

        public string BoardsText { get; set; }

        public bool HasBoards { get; set; }
    }
}