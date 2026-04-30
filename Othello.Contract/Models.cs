namespace Othello.Contract;

public enum DiscColor
{
    None,
    Black,
    White
}

public record Move(int Row, int Column);

public class BoardState
{
    public DiscColor[,] Grid { get; init; } = new DiscColor[8, 8];

    public BoardState Clone()
    {
        var newGrid = new DiscColor[8, 8];
        Array.Copy(Grid, newGrid, Grid.Length);
        return new BoardState { Grid = newGrid };
    }
}
