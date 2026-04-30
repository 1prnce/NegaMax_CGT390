using System;
using System.Threading;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.Engine;

public class GameRunner
{
    public BoardState Board { get; private set; } = GameLogic.CreateInitialBoard();
    public DiscColor CurrentTurn { get; private set; } = DiscColor.Black;
    public bool IsGameOver { get; private set; } = false;

    private readonly IOthelloAI _blackAI;
    private readonly IOthelloAI _whiteAI;

    public event Action<BoardState>? BoardUpdated;
    public event Action<string>? GameEvent;

    public GameRunner(IOthelloAI blackAI, IOthelloAI whiteAI)
    {
        _blackAI = blackAI;
        _whiteAI = whiteAI;
    }

    public async Task RunGameAsync()
    {
        while (!IsGameOver)
        {
            var validMoves = GameLogic.GetValidMoves(Board, CurrentTurn);
            if (validMoves.Count == 0)
            {
                GameEvent?.Invoke($"{CurrentTurn} has no moves.");
                CurrentTurn = CurrentTurn == DiscColor.Black ? DiscColor.White : DiscColor.Black;
                
                var opponentMoves = GameLogic.GetValidMoves(Board, CurrentTurn);
                if (opponentMoves.Count == 0)
                {
                    IsGameOver = true;
                    GameEvent?.Invoke("Game Over!");
                    break;
                }
                continue;
            }

            IOthelloAI currentAI = CurrentTurn == DiscColor.Black ? _blackAI : _whiteAI;
            Move? move = null;

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    move = await currentAI.GetMoveAsync(Board.Clone(), CurrentTurn, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    GameEvent?.Invoke($"{CurrentTurn} ({currentAI.Name}) timed out!");
                }
                catch (Exception ex)
                {
                    GameEvent?.Invoke($"{CurrentTurn} error: {ex.Message}");
                }
            }

            if (move != null && GameLogic.IsValidMove(Board, move, CurrentTurn))
            {
                GameLogic.ApplyMove(Board, move, CurrentTurn);
                BoardUpdated?.Invoke(Board);
            }
            else
            {
                GameEvent?.Invoke($"{CurrentTurn} forfeited turn (invalid move or timeout).");
            }

            CurrentTurn = CurrentTurn == DiscColor.Black ? DiscColor.White : DiscColor.Black;
            await Task.Delay(500); // Small delay for visual effect
        }

        var score = GameLogic.GetScore(Board);
        GameEvent?.Invoke($"Final Score: Black {score.Black} - White {score.White}");
    }
}
