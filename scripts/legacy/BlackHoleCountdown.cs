// Legacy prototype: preserved for reference; not used by the main game scene.
using Godot;
using System;

namespace Orbitals;

public partial class BlackHoleCountdown : Label
{
	[Signal] public delegate void ArrivalDueEventHandler();
	[Export(PropertyHint.Range, "1,3600,1,suffix:s")]
	public float ArrivalSeconds { get; set; } = 180f;
	[Export] public Control IntroSlides { get; set; }
	public double SecondsRemaining { get; private set; }
	public bool HasArrived { get; private set; }

	public override void _Ready()
	{
		SecondsRemaining = Mathf.Max(1f, ArrivalSeconds);
		UpdateDisplay();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (HasArrived || (GodotObject.IsInstanceValid(IntroSlides) && IntroSlides.Visible)) return;
		SecondsRemaining = Math.Max(0d, SecondsRemaining - delta);
		if (SecondsRemaining == 0d)
		{
			HasArrived = true;
			EmitSignal(SignalName.ArrivalDue);
		}
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		int seconds = (int)Math.Ceiling(SecondsRemaining);
		Text = HasArrived ? "BLACK HOLE ETA: 00:00" : $"BLACK HOLE ETA: {seconds / 60:00}:{seconds % 60:00}";
		AddThemeColorOverride("font_color", seconds <= 30 ? new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.75f, 1f));
	}
}
