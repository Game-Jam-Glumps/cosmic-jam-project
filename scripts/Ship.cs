using Godot;
using System;

public partial class Ship : CharacterBody2D
{
    [Export]
    public int speed { get; set; } = 200;
    //public Vector2 velocity = Vector2.Zero;
    public override void _PhysicsProcess(double delta)
	{
        var velocity = Vector2.Zero;
		//float direction = Input.GetAxis("Up", "Down");

        if(Input.IsActionPressed("Up"))
        {
            velocity.Y -= 1;
        }
        if(Input.IsActionPressed("Down"))
        {
            velocity.Y += 1;
        }

        if(velocity.Length() > 0)
        {
            velocity = velocity.Normalized() * speed;
        }
        
        Position += velocity * (float)delta;       
        GD.Print(Position); 
	}
}
