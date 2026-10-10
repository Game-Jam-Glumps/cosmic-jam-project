using Godot;

public partial class Planet : Node2D
{
	[Export] public Node2D Center { get; set; }

	[Export] public float Radius { get; set; } = 200f;
	[Export] public float RotationSpeed { get; set; } = 1.5f;

	public override void _Ready()
	{
		ProcessPhysicsPriority = -20;
	}

	public override void _PhysicsProcess(double delta)
	{
		float direction = Input.GetAxis("Left", "Right");

		Rotation -= direction * RotationSpeed * (float)delta;
	}

	public Vector2 CenterPosition =>
		GodotObject.IsInstanceValid(Center)
			? Center.GlobalPosition
			: GlobalPosition;

	private Vector2 LocalCenter => ToLocal(CenterPosition);

	public static float Wrap(float angle)
	{
		return Mathf.Wrap(angle, -Mathf.Pi, Mathf.Pi);
	}

	public float AngleAt(Vector2 worldPosition)
	{
		return (ToLocal(worldPosition) - LocalCenter).Angle();
	}

	public Vector2 SurfacePoint(float angle, float height = 0f)
	{
		Vector2 direction = Vector2.Right.Rotated(angle);

		return ToGlobal(
			LocalCenter + direction * (Radius + height)
		);
	}
}
	
