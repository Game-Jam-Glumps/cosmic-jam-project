using Godot;

namespace Orbitals;


public partial class GameRound : Node
{
    [Signal] public delegate void RoundFinishedEventHandler(RunResult result);
    [Export] public GameSession Session { get; set; }
    [Export] public PlayerController Controls { get; set; }
    private bool _finished;

    
    public override void _Ready()
    {
        if (!GodotObject.IsInstanceValid(Session))
        {
            GD.PushError($"{Name}: assign the round's GameSession.");
            return;
        }
        Session.ArrivalDue += OnArrivalDue;
        if (GodotObject.IsInstanceValid(Controls)) Controls.ReleaseControls();
    }

    
    public void StopRound()
    {
        if (GodotObject.IsInstanceValid(Controls)) Controls.SetEnabled(false);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    
    private void OnArrivalDue()
    {
        if (_finished) return;
        _finished = true;
        RunResult result = Session.CaptureResult();
        StopRound();
        EmitSignal(SignalName.RoundFinished, result);
    }

    
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Session)) Session.ArrivalDue -= OnArrivalDue;
    }
}
