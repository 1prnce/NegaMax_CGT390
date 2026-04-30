using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.AI.Shlok;

public class ShlokAI : IOthelloAI
{
    public string Name => "Shlok NegaMax";

    private static readonly int[,] SquareWeights = {
        { 120, -20,  20,   5,   5,  20, -20, 120 },
        { -20, -40,  -5,  -5,  -5,  -5, -40, -20 },
        {  20,  -5,  15,   3,   3,  15,  -5,  20 },
        {   5,  -5,   3,   3,   3,   3,  -5,   5 },
        {   5,  -5,   3,   3,   3,   3,  -5,   5 },
        {  20,  -5,  15,   3,   3,  15,  -5,  20 },
        { -20, -40,  -5,  -5,  -5,  -5, -40, -20 },
        { 120, -20,  20,   5,   5,  20, -20, 120 }
    };

    private static readonly int[] Dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly int[] Dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

    private const int TimeBudgetMs = 4500;
    private const int MaxDepth = 8;
    private const int WinScore = 100000;

    public async Task<Move?> GetMoveAsync(BoardState board, DiscColor yourColor, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var validMoves = GetValidMoves(board, yourColor);
            if (validMoves.Count == 0) return null;
            if (validMoves.Count == 1) return validMoves[0];

            var stopwatch = Stopwatch.StartNew();
            Move bestMove = validMoves[0];

            for (int depth = 1; depth <= MaxDepth; depth++)
            {
                if (stopwatch.ElapsedMilliseconds >= TimeBudgetMs || ct.IsCancellationRequested) break;

                Move? bestThisDepth = null;
                int bestScore = int.MinValue;
                int alpha = int.MinValue + 1;
                int beta = int.MaxValue;
                bool finished = true;

                foreach (var move in OrderMoves(validMoves))
                {
                    if (stopwatch.ElapsedMilliseconds >= TimeBudgetMs || ct.IsCancellationRequested)
                    {
                        finished = false;
                        break;
                    }

                    var newBoard = ApplyMove(board, move, yourColor);
                    int score = -NegaMax(newBoard, depth - 1, -beta, -alpha,
                                         Opponent(yourColor), yourColor, stopwatch, ct);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestThisDepth = move;
                    }
                    if (score > alpha) alpha = score;
                }

                if (finished && bestThisDepth != null)
                {
                    bestMove = bestThisDepth;
                    if (bestScore >= WinScore) break; 
                }
            }

            return bestMove;
        }, ct);
    }

    private int NegaMax(BoardState board, int depth, int alpha, int beta,
                        DiscColor current, DiscColor me, Stopwatch sw, CancellationToken ct)
    {
        if (sw.ElapsedMilliseconds >= TimeBudgetMs || ct.IsCancellationRequested) return 0;

        var validMoves = GetValidMoves(board, current);

        if (depth == 0)
        {
            int eval = Evaluate(board, me);
            return current == me ? eval : -eval;
        }

        // no moves means we have to pass
        if (validMoves.Count == 0)
        {
            var oppMoves = GetValidMoves(board, Opponent(current));
            if (oppMoves.Count == 0) return TerminalScore(board, current); // game over
            return -NegaMax(board, depth - 1, -beta, -alpha, Opponent(current), me, sw, ct);
        }

        int best = int.MinValue;
        foreach (var move in OrderMoves(validMoves))
        {
            var newBoard = ApplyMove(board, move, current);
            int score = -NegaMax(newBoard, depth - 1, -beta, -alpha, Opponent(current), me, sw, ct);

            if (score > best) best = score;
            if (best > alpha) alpha = best;
            if (alpha >= beta) break; 
        }
        return best;
    }

    private int TerminalScore(BoardState board, DiscColor current)
    {
        int my = 0, op = 0;
        DiscColor opp = Opponent(current);
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                if (board.Grid[r, c] == current) my++;
                else if (board.Grid[r, c] == opp) op++;
            }
        if (my > op) return WinScore + (my - op);
        if (my < op) return -WinScore - (op - my);
        return 0;
    }

    private int Evaluate(BoardState board, DiscColor myColor)
    {
        DiscColor opp = Opponent(myColor);

        int posScore = 0;
        int myDiscs = 0, oppDiscs = 0;
        int totalDiscs = 0;

        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var cell = board.Grid[r, c];
                if (cell == DiscColor.None) continue;
                totalDiscs++;
                if (cell == myColor)
                {
                    myDiscs++;
                    posScore += SquareWeights[r, c];
                }
                else
                {
                    oppDiscs++;
                    posScore -= SquareWeights[r, c];
                }
            }

        if (totalDiscs >= 50)
        {
            return 100 * (myDiscs - oppDiscs);
        }

        int myMoves = GetValidMoves(board, myColor).Count;
        int oppMoves = GetValidMoves(board, opp).Count;
        int mobilityScore = (myMoves + oppMoves == 0) ? 0
            : 100 * (myMoves - oppMoves) / (myMoves + oppMoves);

        int cornerScore = 0;
        int[,] corners = { { 0, 0 }, { 0, 7 }, { 7, 0 }, { 7, 7 } };
        for (int i = 0; i < 4; i++)
        {
            int r = corners[i, 0], c = corners[i, 1];
            if (board.Grid[r, c] == myColor) cornerScore += 25;
            else if (board.Grid[r, c] == opp) cornerScore -= 25;
        }

        int discScore = (myDiscs + oppDiscs == 0) ? 0
            : 100 * (myDiscs - oppDiscs) / (myDiscs + oppDiscs);

        return posScore + 30 * cornerScore + 5 * mobilityScore + 1 * discScore;
    }

    private IEnumerable<Move> OrderMoves(List<Move> moves)
    {
        return moves.OrderByDescending(m => SquareWeights[m.Row, m.Column]);
    }

    private BoardState ApplyMove(BoardState board, Move move, DiscColor color)
    {
        var newGrid = new DiscColor[8, 8];
        Array.Copy(board.Grid, newGrid, 64);
        var newBoard = new BoardState { Grid = newGrid };

        newBoard.Grid[move.Row, move.Column] = color;
        DiscColor opp = Opponent(color);

        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + Dr[i];
            int c = move.Column + Dc[i];
            var toFlip = new List<(int, int)>();

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == opp)
            {
                toFlip.Add((r, c));
                r += Dr[i];
                c += Dc[i];
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == color && toFlip.Count > 0)
            {
                foreach (var (fr, fc) in toFlip)
                {
                    newBoard.Grid[fr, fc] = color;
                }
            }
        }

        return newBoard;
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

        DiscColor opp = Opponent(color);
        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + Dr[i];
            int c = move.Column + Dc[i];
            int count = 0;

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == opp)
            {
                r += Dr[i]; c += Dc[i]; count++;
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == color && count > 0)
            {
                return true;
            }
        }
        return false;
    }

    private static DiscColor Opponent(DiscColor color)
        => color == DiscColor.Black ? DiscColor.White : DiscColor.Black;
}