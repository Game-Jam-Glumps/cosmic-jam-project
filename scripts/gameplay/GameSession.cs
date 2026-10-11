using Godot;
using System;

namespace Orbitals;


public partial class GameSession : Node
{
    [Signal] public delegate void RescueCountChangedEventHandler(int count);
    [Signal] public delegate void ScoreChangedEventHandler(int score);
    [Signal] public delegate void TimeRemainingChangedEventHandler(double seconds);
    [Signal] public delegate void ArrivalDueEventHandler();

    [Export(PropertyHint.Range, "1,3600,1,suffix:s")]
    public float ArrivalSeconds { get; set; } = 10f;
    [Export] public bool StartOnReady { get; set; }
    [Export(PropertyHint.Range, "1,1000,1")] public int ScorePerStar { get; set; } = 20;
    public int RescuedCount { get; private set; }
<<<<<<< HEAD
    public int Score => RescuedCount;
=======
    public int Score { get; private set; }
>>>>>>> 2007f16 (Add random chance for Shiny critter)
    public double SecondsRemaining { get; private set; }
    public bool HasArrived { get; private set; }
    public bool IsRunning { get; private set; }

    
    public override void _Ready()
    {
        SecondsRemaining = Mathf.Max(1f, ArrivalSeconds);
        if (StartOnReady) StartSession();
    }

    
    public void StartSession()
    {
        if (HasArrived) return;
        IsRunning = true;
    }

<<<<<<< HEAD
    
    public void RecordRescue()
=======
    // Records one completed rescue and publishes the new session total.
    public void RecordRescue(Critter critter)
>>>>>>> 2007f16 (Add random chance for Shiny critter)
    {
        if (HasArrived) return;
        RescuedCount++;
        Score += critter?.IsShiny == true ? 500 : 50;
        EmitSignal(SignalName.RescueCountChanged, RescuedCount);
        EmitSignal(SignalName.ScoreChanged, Score);
    }

    
    public RunResult CaptureResult() => new()
    {
        RescuedCount = RescuedCount,
        Score = Score,
        Stars = Mathf.Clamp(Score / Mathf.Max(1, ScorePerStar), 0, 5)
    };

    
    public override void _PhysicsProcess(double delta)
        => AdvanceTime(delta);

    
    public void AdvanceTime(double seconds)
    {
        if (!IsRunning || HasArrived || seconds <= 0d) return;
        SecondsRemaining = Math.Max(0d, SecondsRemaining - seconds);
        EmitSignal(SignalName.TimeRemainingChanged, SecondsRemaining);
        if (SecondsRemaining > 0d) return;
        HasArrived = true;
        IsRunning = false;
        EmitSignal(SignalName.ArrivalDue);
    }
}
