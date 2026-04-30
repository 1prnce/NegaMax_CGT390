using System;
using System.Collections.Generic;
using Othello.Contract;

namespace Othello.Engine;

public static class GameLogic
{
    public static BoardState CreateInitialBoard()
    {
        var board = new BoardState();
        board.Grid[3, 3] = DiscColor.White;
        board.Grid[3, 4] = DiscColor.Black;
        board.Grid[4, 3] = DiscColor.Black;
        board.Grid[4, 4] = DiscColor.White;
        return board;
    }

    public static bool IsValidMove(BoardState board, Move move, DiscColor color)
    {
        if (move.Row < 0 || move.Row >= 8 || move.Column < 0 || move.Column >= 8) return false;
        if (board.Grid[move.Row, move.Column] != DiscColor.None) return false;

        return GetFlippableDiscs(board, move, color).Count > 0;
    }

    public static List<Move> GetValidMoves(BoardState board, DiscColor color)
    {
        var moves = new List<Move>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var move = new Move(r, c);
                if (IsValidMove(board, move, color))
                {
                    moves.Add(move);
                }
            }
        }
        return moves;
    }

    public static void ApplyMove(BoardState board, Move move, DiscColor color)
    {
        var flippable = GetFlippableDiscs(board, move, color);
        if (flippable.Count == 0) throw new InvalidOperationException("Invalid move");

        board.Grid[move.Row, move.Column] = color;
        foreach (var disc in flippable)
        {
            board.Grid[disc.Row, disc.Column] = color;
        }
    }

    private static List<Move> GetFlippableDiscs(BoardState board, Move move, DiscColor color)
    {
        var flippable = new List<Move>();
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        DiscColor opponent = color == DiscColor.Black ? DiscColor.White : DiscColor.Black;

        for (int i = 0; i < 8; i++)
        {
            var path = new List<Move>();
            int r = move.Row + dr[i];
            int c = move.Column + dc[i];

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == opponent)
            {
                path.Add(new Move(r, c));
                r += dr[i];
                c += dc[i];
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == color && path.Count > 0)
            {
                flippable.AddRange(path);
            }
        }

        return flippable;
    }

    public static (int Black, int White) GetScore(BoardState board)
    {
        int black = 0, white = 0;
        foreach (var disc in board.Grid)
        {
            if (disc == DiscColor.Black) black++;
            else if (disc == DiscColor.White) white++;
        }
        return (black, white);
    }
}
