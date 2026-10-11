using Godot;

namespace Orbitals;


public partial class EndScreen : Control
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();
    [Export] public RunResult Result { get; set; } = new();
    [Export] public Label ScoreLabel { get; set; }
    [Export] public Label RescuedLabel { get; set; }
    [Export] public Godot.Collections.Array<Label> StarLabels { get; set; } = new();
    [Export] public Button RetryButton { get; set; }
    [Export] public Button MenuButton { get; set; }

    
    public override void _Ready()
    {
        Result ??= new RunResult();
        ScoreLabel.Text = Result.Score.ToString();
        RescuedLabel.Text = $"{Result.RescuedCount} CREATURES RESCUED";
        for (int index = 0; index < StarLabels.Count; index++)
            StarLabels[index].Modulate = index < Result.Stars
                ? new Color(1f, 0.75f, 0.3f) : new Color(0.32f, 0.36f, 0.43f);
        RetryButton.Pressed += OnRetryPressed;
        MenuButton.Pressed += OnMenuPressed;
        RetryButton.GrabFocus();
    }

    
    private void OnRetryPressed() => EmitSignal(SignalName.RetryRequested);
    private void OnMenuPressed() => EmitSignal(SignalName.MenuRequested);

    
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(RetryButton)) RetryButton.Pressed -= OnRetryPressed;
        if (GodotObject.IsInstanceValid(MenuButton)) MenuButton.Pressed -= OnMenuPressed;
    }
}
