using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.Engine;

public class TournamentRunner
{
    private readonly List<IOthelloAI> _allAIs;
    private IOthelloAI? _currentChampion;
    private int _nextAIIndex = 0;

    public event Action<IOthelloAI, IOthelloAI>? MatchStarted;
    public event Action<IOthelloAI, (int Black, int White)>? MatchFinished;
    public event Action<BoardState>? BoardUpdated;
    public event Action<string>? TournamentLog;

    public TournamentRunner(List<IOthelloAI> ais)
    {
        _allAIs = ais;
    }

    public async Task RunTournamentAsync()
    {
        if (_allAIs.Count < 2)
        {
            TournamentLog?.Invoke("Need at least 2 AIs for a tournament.");
            return;
        }

        _currentChampion = _allAIs[0];
        _nextAIIndex = 1;

        while (_nextAIIndex < _allAIs.Count)
        {
            var challenger = _allAIs[_nextAIIndex];
            TournamentLog?.Invoke($"--- Match {_nextAIIndex}: {_currentChampion.Name} (Black) vs {challenger.Name} (White) ---");
            
            MatchStarted?.Invoke(_currentChampion, challenger);

            var runner = new GameRunner(_currentChampion, challenger);
            runner.BoardUpdated += b => BoardUpdated?.Invoke(b);
            runner.GameEvent += s => TournamentLog?.Invoke(s);

            await runner.RunGameAsync();

            var score = GameLogic.GetScore(runner.Board);
            MatchFinished?.Invoke(_currentChampion, score);

            if (score.White > score.Black)
            {
                TournamentLog?.Invoke($"{challenger.Name} WINS and becomes the new champion!");
                _currentChampion = challenger;
            }
            else if (score.Black > score.White)
            {
                TournamentLog?.Invoke($"{_currentChampion.Name} DEFENDS the title!");
            }
            else
            {
                TournamentLog?.Invoke("It's a DRAW! Champion retains the title by default logic.");
            }

            _nextAIIndex++;
            await Task.Delay(2000); // Wait between matches
        }

        TournamentLog?.Invoke($"Tournament Finished! Final Champion: {_currentChampion.Name}");
    }
}
