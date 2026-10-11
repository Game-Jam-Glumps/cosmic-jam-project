using Godot;
using System;

namespace Orbitals;

// Displays session state and notifies the session when the intro has finished.
public partial class GameHud : CanvasLayer
{
    [Signal] public delegate void IntroFinishedEventHandler();
    [Export] public GameSession Session { get; set; }
    [Export] public Label CargoLabel { get; set; }
    [Export] public Label CountdownLabel { get; set; }
    [Export] public Control IntroSlides { get; set; }

    // Subscribes to the session and initializes labels from its current state.
    public override void _Ready()
    {
        if (!GodotObject.IsInstanceValid(Session))
        {
            GD.PushError($"{Name}: assign a GameSession before displaying the HUD.");
            return;
        }
        Session.RescueCountChanged += UpdateCargo;
        Session.TimeRemainingChanged += UpdateCountdown;
        UpdateCargo(Session.RescuedCount);
        UpdateCountdown(Session.SecondsRemaining);
        if (GodotObject.IsInstanceValid(IntroSlides))
            IntroSlides.VisibilityChanged += OnIntroVisibilityChanged;
        OnIntroVisibilityChanged();
    }

    // Formats the rescued total; all counting remains in GameSession.
    private void UpdateCargo(int count)
    {
        if (GodotObject.IsInstanceValid(CargoLabel)) CargoLabel.Text = $"RESCUED: {count}";
    }

    // Formats remaining time and highlights the final thirty seconds.
    private void UpdateCountdown(double remaining)
    {
        if (!GodotObject.IsInstanceValid(CountdownLabel)) return;
        int seconds = (int)Math.Ceiling(remaining);
        CountdownLabel.Text = $"BLACK HOLE ETA: {seconds / 60:00}:{seconds % 60:00}";
        CountdownLabel.AddThemeColorOverride("font_color", seconds <= 30 ?
            new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.75f, 1f));
    }

    // Reports that the intro is hidden; scene wiring decides when to start gameplay.
    private void OnIntroVisibilityChanged()
    {
        if (!GodotObject.IsInstanceValid(IntroSlides) || !IntroSlides.Visible)
            EmitSignal(SignalName.IntroFinished);
    }

    // Disconnects observers so removing or replacing a HUD cannot leave stale callbacks.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Session))
        {
            Session.RescueCountChanged -= UpdateCargo;
            Session.TimeRemainingChanged -= UpdateCountdown;
        }
        if (GodotObject.IsInstanceValid(IntroSlides))
            IntroSlides.VisibilityChanged -= OnIntroVisibilityChanged;
    }
}
