using Godot;

namespace Orbitals;


public partial class StartScreen : Control
{
    [Signal] public delegate void PlayRequestedEventHandler();
    [Export] public Button PlayButton { get; set; }

    
    public override void _Ready()
    {
        PlayButton.Pressed += OnPlayPressed;
        PlayButton.GrabFocus();
    }

    
    private void OnPlayPressed() => EmitSignal(SignalName.PlayRequested);

    
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(PlayButton)) PlayButton.Pressed -= OnPlayPressed;
    }
}
