using Godot;
using System;
using System.Collections.Generic;

namespace Orbitals;


public partial class GameSession : Node
{
    [Signal] public delegate void RescueCountChangedEventHandler(int count);
    [Signal] public delegate void ScoreChangedEventHandler(int score);
    [Signal] public delegate void TimeRemainingChangedEventHandler(double seconds);
    [Signal] public delegate void ArrivalDueEventHandler();

    [Export(PropertyHint.Range, "1,3600,1,suffix:s")]
    public float ArrivalSeconds { get; set; } = 120f;
    [Export] public bool StartOnReady { get; set; }
    [Export(PropertyHint.Range, "1,10000,50")] public int ScorePerStar { get; set; } = 10000;
    public int RescuedCount { get; private set; }
    public int Score { get; private set; }
    public double SecondsRemaining { get; private set; }
    public bool HasArrived { get; private set; }
    public bool IsRunning { get; private set; }
    private readonly HashSet<ulong> _rescuedCritters = new();

    
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

    // Records one completed rescue and publishes the new session total.
    public void RecordRescue(Critter critter)
    {
        if (HasArrived || !GodotObject.IsInstanceValid(critter) || critter.IsQueuedForDeletion() ||
            !_rescuedCritters.Add(critter.GetInstanceId())) return;
        RescuedCount++;
        int rarityPoints = string.Equals(critter.Profile?.Rarity, "Legendary",
            StringComparison.OrdinalIgnoreCase) ? 1000 : 50;
        Score += rarityPoints * (critter.IsShiny ? 5 : 1);
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
