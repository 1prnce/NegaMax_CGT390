using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using Othello.Contract;
using Othello.Engine;

namespace Othello.UI;

public partial class MainWindow : Window
{
    private IOthelloAI? _blackAI;
    private IOthelloAI? _whiteAI;
    private List<IOthelloAI> _tournamentAIs = new();
    private GameRunner? _runner;
    private readonly Border[,] _cellBorders = new Border[8, 8];

    public MainWindow()
    {
        InitializeComponent();
        InitializeBoardUI();

        LoadBlackButton.Click += async (_, _) => { _blackAI = await LoadAIAsync(BlackAIName); UpdateStartButton(); };
        LoadWhiteButton.Click += async (_, _) => { _whiteAI = await LoadAIAsync(WhiteAIName); UpdateStartButton(); };
        StartButton.Click += StartMatch;

        LoadTourneyDirButton.Click += LoadTourneyDirAsync;
        StartTourneyButton.Click += StartTournament;
    }

    private void InitializeBoardUI()
    {
        BoardGrid.Children.Clear();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Background = Brushes.Transparent,
                    Child = CreateDisc(DiscColor.None)
                };
                _cellBorders[r, c] = border;
                BoardGrid.Children.Add(border);
            }
        }
    }

    private Control CreateDisc(DiscColor color)
    {
        if (color == DiscColor.None) return new Canvas();

        return new Ellipse
        {
            Width = 40,
            Height = 40,
            Fill = color == DiscColor.Black ? Brushes.Black : Brushes.White,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private async Task<IOthelloAI?> LoadAIAsync(TextBlock nameLabel)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select AI DLL",
            Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "DLL Modules", Extensions = { "dll" } } }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            var ai = PluginLoader.LoadPlugin(result[0]);
            if (ai != null)
            {
                nameLabel.Text = ai.Name;
                LogText.Text += $"\nLoaded AI: {ai.Name}";
                return ai;
            }
            else
            {
                LogText.Text += $"\nFailed to load AI from: {result[0]}";
            }
        }
        return null;
    }

    private void UpdateStartButton()
    {
        StartButton.IsEnabled = _blackAI != null && _whiteAI != null;
        StartTourneyButton.IsEnabled = _tournamentAIs.Count >= 2;
    }

    private async void LoadTourneyDirAsync(object? sender, EventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select Directory containing AI DLLs" };
        var result = await dialog.ShowAsync(this);
        if (!string.IsNullOrEmpty(result))
        {
            _tournamentAIs = PluginLoader.LoadPluginsFromDirectory(result);
            TourneyStatus.Text = $"Loaded {_tournamentAIs.Count} AIs from directory.";
            foreach (var ai in _tournamentAIs)
            {
                LogText.Text += $"\nTournament Loaded: {ai.Name}";
            }
            UpdateStartButton();
        }
    }

    private async void StartTournament(object? sender, EventArgs e)
    {
        if (_tournamentAIs.Count < 2) return;

        StartTourneyButton.IsEnabled = false;
        LoadTourneyDirButton.IsEnabled = false;
        StartButton.IsEnabled = false;

        var tourney = new TournamentRunner(_tournamentAIs);
        tourney.MatchStarted += (b, w) => Dispatcher.UIThread.InvokeAsync(() => {
            CurrentBlackName.Text = $"Black: {b.Name}";
            CurrentWhiteName.Text = $"White: {w.Name}";
        });
        tourney.TournamentLog += s => Dispatcher.UIThread.InvokeAsync(() => LogText.Text += "\n" + s);
        tourney.BoardUpdated += UpdateBoard;
        
        await tourney.RunTournamentAsync();

        StartTourneyButton.IsEnabled = true;
        LoadTourneyDirButton.IsEnabled = true;
        UpdateStartButton();
    }

    private async void StartMatch(object? sender, EventArgs e)
    {
        if (_blackAI == null || _whiteAI == null) return;

        StartButton.IsEnabled = false;
        LoadBlackButton.IsEnabled = false;
        LoadWhiteButton.IsEnabled = false;

        _runner = new GameRunner(_blackAI, _whiteAI);
        CurrentBlackName.Text = $"Black: {_blackAI.Name}";
        CurrentWhiteName.Text = $"White: {_whiteAI.Name}";
        _runner.BoardUpdated += UpdateBoard;
        _runner.GameEvent += s => Dispatcher.UIThread.InvokeAsync(() => LogText.Text += "\n" + s);

        UpdateBoard(_runner.Board);
        await _runner.RunGameAsync();

        StartButton.IsEnabled = true;
        LoadBlackButton.IsEnabled = true;
        LoadWhiteButton.IsEnabled = true;
    }

    private void UpdateBoard(BoardState board)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    _cellBorders[r, c].Child = CreateDisc(board.Grid[r, c]);
                }
            }
            var score = GameLogic.GetScore(board);
            ScoreText.Text = $"Black: {score.Black} - White: {score.White}";
            StatusText.Text = _runner?.IsGameOver == true ? "Game Over" : $"Current Turn: {_runner?.CurrentTurn}";
        });
    }
}