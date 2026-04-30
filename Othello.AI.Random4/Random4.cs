using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.AI.Random;

public class RandomAI4 : IOthelloAI
{
    public string Name => "Random AI 4";

    public async Task<Move?> GetMoveAsync(BoardState board, DiscColor yourColor, CancellationToken ct)
    {
        await Task.Delay(new System.Random().Next(100, 1000), ct);

        var validMoves = GetValidMoves(board, yourColor);
        if (validMoves.Count == 0) return null;

        return validMoves[new System.Random().Next(validMoves.Count)];
    }

    private List<Move> GetValidMoves(BoardState board, DiscColor color)
    {
        var moves = new List<Move>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (IsValidMove(board, new Move(r, c), color))
                {
                    moves.Add(new Move(r, c));
                }
            }
        }
        return moves;
    }

    private bool IsValidMove(BoardState board, Move move, DiscColor color)
    {
        if (board.Grid[move.Row, move.Column] != DiscColor.None) return false;
        
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
        DiscColor opponent = color == DiscColor.Black ? DiscColor.White : DiscColor.Black;

        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + dr[i];
            int c = move.Column + dc[i];
            int count = 0;

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == opponent)
            {
                r += dr[i];
                c += dc[i];
                count++;
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == color && count > 0)
            {
                return true;
            }
        }
        return false;
    }
}
