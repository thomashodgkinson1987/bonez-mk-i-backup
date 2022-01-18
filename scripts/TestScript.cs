using Godot;

using System;
using System.Security.Policy;

public class TestScript : Node2D
{

	[Export] public float X { get; set; } = 1f;
	[Export] public float Y { get; set; } = 1f;

	private float m_elapsedTime = 0f;

	public override void _Ready ()
	{
		base._Ready();
	}

	public override void _Process (float delta)
	{
		base._Process(delta);


	}

	public override void _PhysicsProcess (float delta)
	{
		base._PhysicsProcess(delta);

		m_elapsedTime += delta;

		Vector2 position = Position;

		position.x = Mathf.Sin(m_elapsedTime) * X;
		position.y = Mathf.Cos(m_elapsedTime) * Y;

		Position = position;
	}

}
