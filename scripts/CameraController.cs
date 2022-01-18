using Godot;

public class CameraController : Camera2D
{

	#region Enums

	public enum EUpdateMode { PhysicsProcess, Process }

	#endregion // Enums



	#region Exports

	[Export] public EUpdateMode UpdateMode { get; set; } = EUpdateMode.PhysicsProcess;
	[Export] public float Lerp { get; set; } = 0.1f;
	[Export] public Node2D Target { get; set; } = null;
	[Export] public Rect2 BoundsRect { get; set; } = new Rect2(0f, 0f, 16f, 16f);
	[Export] public bool IsClampToBounds { get; set; } = true;
	[Export] public Vector2 InnerExtents { get; set; } = Vector2.One;

	#endregion // Exports



	#region Node2D methods

	public override void _Ready ()
	{
		base._Ready();
	}

	public override void _PhysicsProcess (float delta)
	{
		base._PhysicsProcess(delta);

		if (UpdateMode == EUpdateMode.PhysicsProcess) MainLoop(delta);
	}

	public override void _Process (float delta)
	{
		base._Process(delta);

		if (UpdateMode == EUpdateMode.Process) MainLoop(delta);
	}

	#endregion // Node2D methods



	#region Private methods

	private void MainLoop (float delta)
	{
		if (Target != null)
		{
			Position = Position.LinearInterpolate(Target.Position, Lerp);

			if (Target.Position.x < Position.x - InnerExtents.x)
			{
				Position = new Vector2(Target.Position.x + InnerExtents.x, Position.y);
			}
			if (Target.Position.x > Position.x + InnerExtents.x)
			{
				Position = new Vector2(Target.Position.x - InnerExtents.x, Position.y);
			}
			if (Target.Position.y < Position.y - InnerExtents.y)
			{
				Position = new Vector2(Position.x, Target.Position.y + InnerExtents.y);
			}
			if (Target.Position.y > Position.y + InnerExtents.y)
			{
				Position = new Vector2(Position.x, Target.Position.y - InnerExtents.y);
			}
		}

		if (IsClampToBounds)
		{
			Vector2 newPosition = Position;

			float minX = BoundsRect.Position.x * Zoom.x;
			float minY = BoundsRect.Position.y * Zoom.y;
			float maxX = minX + BoundsRect.Size.x;
			float maxY = minY + BoundsRect.Size.y;

			float width = GetViewportRect().Size.x * Zoom.x;
			float height = GetViewportRect().Size.y * Zoom.y;
			float halfWidth = width / 2;
			float halfHeight = height / 2;

			if (Mathf.Abs(maxX - minX) > width)
			{
				newPosition.x = Mathf.Clamp(newPosition.x, minX + halfWidth, maxX - halfWidth);
			}
			else
			{
				newPosition.x = Mathf.Lerp(minX, maxX, 0.5f);
			}

			if (Mathf.Abs(maxY - minY) > height)
			{
				newPosition.y = Mathf.Clamp(newPosition.y, minY + halfHeight, maxY - halfHeight);
			}
			else
			{
				newPosition.y = Mathf.Lerp(minY, maxY, 0.5f);
			}

			Position = newPosition;
		}
	}

	#endregion // Private methods

}
