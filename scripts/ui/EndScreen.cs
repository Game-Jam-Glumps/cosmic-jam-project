using Godot;

namespace Orbitals;


public partial class EndScreen : Control
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();
    [Export] public RunResult Result { get; set; } = new();
    [Export] public Label ScoreLabel { get; set; }
    [Export] public Label RescuedLabel { get; set; }
    [Export] public Godot.Collections.Array<TextureRect> StarIcons { get; set; } = new();
    [Export] public Texture2D FullStarTexture { get; set; }
    [Export] public Texture2D EmptyStarTexture { get; set; }
    [Export] public Button RetryButton { get; set; }
    [Export] public Button MenuButton { get; set; }

    
    public override void _Ready()
    {
        Result ??= new RunResult();
        ScoreLabel.Text = Result.Score.ToString();
        RescuedLabel.Text = $"{Result.RescuedCount} CREATURES RESCUED";
        for (int index = 0; index < StarIcons.Count; index++)
            StarIcons[index].Texture = index < Result.Stars ? FullStarTexture : EmptyStarTexture;
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
