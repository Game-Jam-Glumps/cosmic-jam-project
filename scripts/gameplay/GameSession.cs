using Godot;
using System;

namespace Orbitals;

// Owns session state and rules; it has no references to actors, input, or UI nodes.
public partial class GameSession : Node
{
    [Signal] public delegate void RescueCountChangedEventHandler(int count);
    [Signal] public delegate void TimeRemainingChangedEventHandler(double seconds);
    [Signal] public delegate void ArrivalDueEventHandler();

    [Export(PropertyHint.Range, "1,3600,1,suffix:s")]
    public float ArrivalSeconds { get; set; } = 180f;
    [Export] public bool StartOnReady { get; set; }
    public int RescuedCount { get; private set; }
    public double SecondsRemaining { get; private set; }
    public bool HasArrived { get; private set; }
    public bool IsRunning { get; private set; }

    // Initializes the clock; the main scene waits for the intro to finish.
    public override void _Ready()
    {
        SecondsRemaining = Mathf.Max(1f, ArrivalSeconds);
        if (StartOnReady) StartSession();
    }

    // Starts the clock once without resetting accumulated rescues or elapsed time.
    public void StartSession()
    {
        if (HasArrived) return;
        IsRunning = true;
    }

    // Records one completed rescue and publishes the new session total.
    public void RecordRescue()
    {
        RescuedCount++;
        EmitSignal(SignalName.RescueCountChanged, RescuedCount);
    }

    // Advances the session clock on Godot's fixed physics timestep.
    public override void _PhysicsProcess(double delta)
        => AdvanceTime(delta);

    // Advances a running clock, clamps it to zero, and reports arrival exactly once.
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
