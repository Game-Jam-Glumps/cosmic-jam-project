using Godot;
using System;

namespace Orbitals;

// Displays progress and remaining time from the current session.
public partial class GameHud : CanvasLayer
{
    [Export] public GameSession Session { get; set; }
    [Export] public Label CargoLabel { get; set; }
    [Export] public Label ScoreLabel { get; set; }
    [Export] public Label CountdownLabel { get; set; }

    // Subscribes to the session and initializes labels from its current state.
    public override void _Ready()
    {
        if (!GodotObject.IsInstanceValid(Session))
        {
            GD.PushError($"{Name}: assign a GameSession before displaying the HUD.");
            return;
        }
        Session.RescueCountChanged += UpdateCargo;
        Session.ScoreChanged += UpdateScore;
        Session.TimeRemainingChanged += UpdateCountdown;
        UpdateCargo(Session.RescuedCount);
        UpdateScore(Session.Score);
        UpdateCountdown(Session.SecondsRemaining);
    }

    // Formats the rescued total; all counting remains in GameSession.
    private void UpdateCargo(int count)
    {
        if (GodotObject.IsInstanceValid(CargoLabel)) CargoLabel.Text = $"RESCUED: {count}";
    }

    // Formats points separately from the number of rescued creatures.
    private void UpdateScore(int score)
    {
        if (GodotObject.IsInstanceValid(ScoreLabel)) ScoreLabel.Text = $"SCORE: {score}";
    }

    // Formats remaining time and highlights the final thirty seconds.
    private void UpdateCountdown(double remaining)
    {
        if (!GodotObject.IsInstanceValid(CountdownLabel)) return;
        int seconds = (int)Math.Ceiling(remaining);
        CountdownLabel.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        CountdownLabel.AddThemeColorOverride("font_color", seconds <= 30 ?
            new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.75f, 1f));
    }

    // Disconnects observers so removing or replacing a HUD cannot leave stale callbacks.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Session))
        {
            Session.RescueCountChanged -= UpdateCargo;
            Session.ScoreChanged -= UpdateScore;
            Session.TimeRemainingChanged -= UpdateCountdown;
        }
    }
}
