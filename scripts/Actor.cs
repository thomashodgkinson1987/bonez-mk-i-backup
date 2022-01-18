using System.Collections.Generic;

using Godot;

public class Actor : Node2D
{

	#region Signals

	[Signal] private delegate void OnActorEntered (Area2D area);

	#endregion // Signals



	#region Nodes

	private AnimatedSprite m_animatedSprite;

	private Area2D m_area2D;
	private CollisionShape2D m_collisionShape2D;
	private RectangleShape2D m_rectangleShape2D;

	private AudioStreamPlayer m_audioStreamPlayer_Jump;

	#endregion // Nodes



	#region Exports

	[Export] public float Acceleration { get; set; } = 2000f;
	[Export] public float Deceleration { get; set; } = 4000f;

	[Export] public Vector2 Velocity { get; set; } = Vector2.Zero;
	[Export] public Vector2 MaxVelocity { get; set; } = new Vector2(400f, 1000f);
	[Export] public bool IsClampVelocity { get; set; } = true;

	[Export] public float Gravity { get; set; } = 200f;
	[Export] public bool IsApplyGravity { get; set; } = true;

	[Export] public Vector2 InputDirection { get; set; } = Vector2.Zero;
	[Export] public bool IsInputJump { get; set; } = false;
	[Export] public bool WasInputJump { get; set; } = false;
	[Export] public bool IsInputEnabled { get; set; } = true;

	[Export] public bool IsResolveInputDirectionX { get; set; } = true;
	[Export] public bool IsResolveInputDirectionY { get; set; } = true;

	[Export] public int JumpCount { get; set; } = 0;
	[Export] public int JumpLimit { get; set; } = 2;
	[Export] public int JumpHeight { get; set; } = 32;
	[Export] public bool IsJumpQueued { get; set; } = false;

	[Export] public int RayCount { get; set; } = 3;
	[Export] public float Margin { get; set; } = 0.1f;

	[Export] public bool IsCollisionLeft { get; set; } = false;
	[Export] public bool IsCollisionRight { get; set; } = false;
	[Export] public bool IsCollisionUp { get; set; } = false;
	[Export] public bool IsCollisionDown { get; set; } = false;

	[Export] public bool WasCollisionLeft { get; set; } = false;
	[Export] public bool WasCollisionRight { get; set; } = false;
	[Export] public bool WasCollisionUp { get; set; } = false;
	[Export] public bool WasCollisionDown { get; set; } = false;

	[Export] public bool IsCollisionEnabled { get; set; } = true;

	[Export] public bool IsJumping { get; set; } = false;
	[Export] public bool IsFalling { get; set; } = false;
	[Export] public bool IsDying { get; set; } = false;

	[Export] public bool IsUpdateAnimation { get; set; } = true;

	#endregion // Exports



	#region Properties

	public float Width => m_rectangleShape2D.Extents.x * 2;
	public float Height => m_rectangleShape2D.Extents.y * 2;

	#endregion // Properties



	#region Node2D methods

	public override void _EnterTree ()
	{
		base._EnterTree();

		GD.Print(Name + ": EnterTree");
	}

	public override void _ExitTree ()
	{
		base._ExitTree();

		GD.Print(Name + ": ExitTree");
	}

	public override void _Ready ()
	{
		base._Ready();

		m_animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");

		m_area2D = GetNode<Area2D>("Area2D");
		m_collisionShape2D = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
		m_rectangleShape2D = (RectangleShape2D)m_collisionShape2D.Shape;

		m_audioStreamPlayer_Jump = GetNode<AudioStreamPlayer>("AudioStreamPlayers/AudioStreamPlayer_Jump");

		GD.Print(Name + ": Ready");
	}

	public override void _PhysicsProcess (float delta)
	{
		base._PhysicsProcess(delta);

		if (IsApplyGravity) ApplyGravity(delta / 2);
		if (IsClampVelocity) ClampVelocity();

		if (IsInputEnabled) ResolveInput_All(delta / 2);
		if (IsClampVelocity) ClampVelocity();

		if (IsCollisionEnabled) Move(delta);

		if (IsUpdateAnimation) ResolveAnimations();

		if (IsApplyGravity) ApplyGravity(delta / 2);
		if (IsClampVelocity) ClampVelocity();

		if (IsInputEnabled) ResolveInput_All(delta / 2);
		if (IsClampVelocity) ClampVelocity();
	}

	public override void _Process (float delta)
	{
		base._Process(delta);

		if (IsInputEnabled) UpdateInput_All();
	}

	#endregion // Node2D methods



	#region Area2D callback methods

	private void OnArea2DEntered (Area2D area)
	{
		EmitSignal("OnActorEntered", area);
	}

	#endregion // Area2D callback methods



	#region Public methods

	public void Reset ()
	{
		Velocity = Vector2.Zero;

		InputDirection = Vector2.Zero;

		IsInputJump = WasInputJump = false;
		JumpCount = 0;
		IsJumpQueued = false;

		IsCollisionLeft = WasCollisionLeft = false;
		IsCollisionRight = WasCollisionRight = false;
		IsCollisionUp = WasCollisionUp = false;
		IsCollisionDown = WasCollisionDown = false;

		m_animatedSprite.Play("idle");
		m_animatedSprite.FlipH = false;
	}

	#endregion // Public methods



	#region Private methods

	private void UpdateInput_All ()
	{
		UpdateInput_Direction();
		UpdateInput_Jump();
	}
	private void UpdateInput_Direction ()
	{
		bool isLeft = Input.IsActionPressed("move_left");
		bool isRight = Input.IsActionPressed("move_right");

		if (isLeft && !isRight)
		{
			InputDirection = new Vector2(-1f, InputDirection.y);
		}
		else if (!isLeft && isRight)
		{
			InputDirection = new Vector2(1f, InputDirection.y);
		}
		else
		{
			InputDirection = new Vector2(0f, InputDirection.y);
		}
	}
	private void UpdateInput_Jump ()
	{
		WasInputJump = IsInputJump;
		IsInputJump = false;

		if (Input.IsActionPressed("jump"))
		{
			IsInputJump = true;
		}

		if (!WasInputJump && IsInputJump)
		{
			IsJumpQueued = true;
		}
	}

	private void ResolveInput_All (float delta)
	{
		ResolveInput_Direction(delta);
		ResolveInput_Jump();
	}
	private void ResolveInput_Direction (float delta)
	{
		if (IsResolveInputDirectionX)
		{
			if (InputDirection.x == 0)
			{
				if (Velocity.x < 0)
				{
					Velocity += new Vector2(Deceleration * delta, 0f);
					if (Velocity.x > 0) Velocity = new Vector2(0f, Velocity.y);
				}
				else if (Velocity.x > 0)
				{
					Velocity += new Vector2(-Deceleration * delta, 0f);
					if (Velocity.x < 0) Velocity = new Vector2(0f, Velocity.y);
				}
			}
			else
			{
				int mDir = Velocity.x < 0 ? -1 : Velocity.x > 0 ? 1 : 0;
				int iDir = InputDirection.x < 0 ? -1 : InputDirection.x > 0 ? 1 : 0;

				if (Velocity.x == 0 || mDir == iDir)
				{
					Velocity += new Vector2(InputDirection.x * Acceleration * delta, 0f);
				}
				else
				{
					Velocity += new Vector2(InputDirection.x * Deceleration * delta, 0f);
				}

			}
		}

		if (IsResolveInputDirectionY)
		{
			if (InputDirection.y == 0)
			{
				if (Velocity.y < 0)
				{
					Velocity += new Vector2(0f, Deceleration * delta);
					if (Velocity.y > 0) Velocity = new Vector2(Velocity.x, 0f);
				}
				else if (Velocity.y > 0)
				{
					Velocity += new Vector2(0f, -Deceleration * delta);
					if (Velocity.y < 0) Velocity = new Vector2(Velocity.x, 0f);
				}
			}
			else
			{
				int mDir = Velocity.y < 0 ? -1 : Velocity.y > 0 ? 1 : 0;
				int iDir = InputDirection.y < 0 ? -1 : InputDirection.y > 0 ? 1 : 0;

				if (Velocity.y == 0 || mDir == iDir)
				{
					Velocity += new Vector2(0f, InputDirection.y * Acceleration * delta);
				}
				else
				{
					Velocity += new Vector2(0f, InputDirection.y * Deceleration * delta);
				}
			}
		}
	}
	private void ResolveInput_Jump ()
	{
		if (IsJumpQueued)
		{
			IsJumpQueued = false;
			if (JumpCount < JumpLimit)
			{
				JumpCount++;
				Velocity = new Vector2(Velocity.x, -Mathf.Sqrt(2 * Gravity * JumpHeight));
				IsJumping = true;
				m_audioStreamPlayer_Jump.Play();
			}
		}
	}

	protected virtual void ResolveAnimations ()
	{
		if (Velocity.y < 0)
		{
			if (IsCollisionDown) m_animatedSprite.Play("idle");
			else m_animatedSprite.Play("jump");
		}
		else if (Velocity.y > 0)
		{
			if (IsCollisionDown) m_animatedSprite.Play("idle");
			else m_animatedSprite.Play("fall");
		}
		else
		{
			if (Velocity.x == 0) m_animatedSprite.Play("idle");
			else m_animatedSprite.Play("run");
		}

		if (Velocity.x == 0 && InputDirection.x != 0)
		{
			m_animatedSprite.FlipH = InputDirection.x < 0 ? true : InputDirection.x > 0 ? false : m_animatedSprite.FlipH;
		}
		else
		{
			m_animatedSprite.FlipH = Velocity.x < 0 ? true : Velocity.x > 0 ? false : m_animatedSprite.FlipH;
		}
	}

	private void ApplyGravity (float delta)
	{
		Velocity += new Vector2(0f, Gravity * delta);
	}
	private void ClampVelocity ()
	{
		if (Velocity.x < -MaxVelocity.x) Velocity = new Vector2(-MaxVelocity.x, Velocity.y);
		if (Velocity.x > MaxVelocity.x) Velocity = new Vector2(MaxVelocity.x, Velocity.y);
		if (Velocity.y < -MaxVelocity.y) Velocity = new Vector2(Velocity.x, -MaxVelocity.y);
		if (Velocity.y > MaxVelocity.y) Velocity = new Vector2(Velocity.x, MaxVelocity.y);
	}
	private void Move (float delta)
	{
		WasCollisionLeft = IsCollisionLeft;
		WasCollisionRight = IsCollisionRight;
		WasCollisionUp = IsCollisionUp;
		WasCollisionDown = IsCollisionDown;

		IsCollisionLeft = false;
		IsCollisionRight = false;
		IsCollisionUp = false;
		IsCollisionDown = false;

		if (Velocity.x < 0)
		{
			RayLeft(delta);
		}
		else if (Velocity.x > 0)
		{
			RayRight(delta);
		}
		else
		{
			RayLeft(delta);
			RayRight(delta);
		}

		Position += new Vector2(Velocity.x * delta, 0f);

		if (Velocity.y < 0)
		{
			RayUp(delta);
		}
		else if (Velocity.y > 0)
		{
			RayDown(delta);
		}
		else
		{
			RayUp(delta);
			RayDown(delta);
		}

		Position += new Vector2(0f, Velocity.y * delta);
	}

	private void RayLeft (float delta)
	{
		Physics2DDirectSpaceState spaceState = GetWorld2d().DirectSpaceState;

		float halfWidth = Width / 2;
		float halfHeight = Height / 2;

		Vector2 start = new Vector2(Position.x, (Position.y - halfHeight) + Margin);
		Vector2 end = new Vector2(Position.x, (Position.y + halfHeight) - Margin);

		float distance = halfWidth + Mathf.Abs(Velocity.x * delta);
		Vector2 direction = Vector2.Left;

		List<Godot.Collections.Dictionary> results = new List<Godot.Collections.Dictionary>();

		int indexOfShortestDistance = -1;
		float shortestDistance = Mathf.Inf;

		for (int i = 0; i < RayCount; i++)
		{
			float lerp = (float)i / (RayCount - 1);
			Vector2 from = start.LinearInterpolate(end, lerp);
			Vector2 to = from + (direction * distance);

			object[] exceptionsArray = new object[] { m_area2D };
			Godot.Collections.Array exceptions = new Godot.Collections.Array(exceptionsArray);
			Godot.Collections.Dictionary result = spaceState.IntersectRay(from, to, exceptions);
			if (result.Count > 0)
			{
				Vector2 hit = (Vector2)result["position"];
				float distanceToHit = from.DistanceSquaredTo(hit);
				if (distanceToHit < shortestDistance)
				{
					shortestDistance = distanceToHit;
					indexOfShortestDistance = i;
				}
			}
			results.Add(result);
		}

		if (indexOfShortestDistance != -1)
		{
			Godot.Collections.Dictionary result = results[indexOfShortestDistance];
			Vector2 hit = (Vector2)result["position"];

			IsCollisionLeft = true;
			Position = new Vector2(hit.x + halfWidth, Position.y);
			Velocity = new Vector2(0f, Velocity.y);
		}
	}
	private void RayRight (float delta)
	{
		Physics2DDirectSpaceState spaceState = GetWorld2d().DirectSpaceState;

		float halfWidth = Width / 2;
		float halfHeight = Height / 2;

		Vector2 start = new Vector2(Position.x, (Position.y - halfHeight) + Margin);
		Vector2 end = new Vector2(Position.x, (Position.y + halfHeight) - Margin);

		float distance = halfWidth + Mathf.Abs(Velocity.x * delta);
		Vector2 direction = Vector2.Right;

		List<Godot.Collections.Dictionary> results = new List<Godot.Collections.Dictionary>();

		int indexOfShortestDistance = -1;
		float shortestDistance = Mathf.Inf;

		for (int i = 0; i < RayCount; i++)
		{
			float lerp = (float)i / (RayCount - 1);
			Vector2 from = start.LinearInterpolate(end, lerp);
			Vector2 to = from + (direction * distance);

			object[] exceptionsArray = new object[] { m_area2D };
			Godot.Collections.Array exceptions = new Godot.Collections.Array(exceptionsArray);
			Godot.Collections.Dictionary result = spaceState.IntersectRay(from, to, exceptions);
			if (result.Count > 0)
			{
				Vector2 hit = (Vector2)result["position"];
				float distanceToHit = from.DistanceSquaredTo(hit);
				if (distanceToHit < shortestDistance)
				{
					shortestDistance = distanceToHit;
					indexOfShortestDistance = i;
				}
			}
			results.Add(result);
		}

		if (indexOfShortestDistance != -1)
		{
			Godot.Collections.Dictionary result = results[indexOfShortestDistance];
			Vector2 hit = (Vector2)result["position"];

			IsCollisionRight = true;
			Position = new Vector2(hit.x - halfWidth, Position.y);
			Velocity = new Vector2(0f, Velocity.y);
		}
	}
	private void RayUp (float delta)
	{
		Physics2DDirectSpaceState spaceState = GetWorld2d().DirectSpaceState;

		float halfWidth = Width / 2;
		float halfHeight = Height / 2;

		Vector2 start = new Vector2((Position.x - halfWidth) + Margin, Position.y);
		Vector2 end = new Vector2((Position.x + halfWidth) - Margin, Position.y);

		float distance = halfHeight + Mathf.Abs(Velocity.y * delta);
		Vector2 direction = Vector2.Up;

		List<Godot.Collections.Dictionary> results = new List<Godot.Collections.Dictionary>();

		int indexOfShortestDistance = -1;
		float shortestDistance = Mathf.Inf;

		for (int i = 0; i < RayCount; i++)
		{
			float lerp = (float)i / (RayCount - 1);
			Vector2 from = start.LinearInterpolate(end, lerp);
			Vector2 to = from + (direction * distance);

			object[] exceptionsArray = new object[] { m_area2D };
			Godot.Collections.Array exceptions = new Godot.Collections.Array(exceptionsArray);
			Godot.Collections.Dictionary result = spaceState.IntersectRay(from, to, exceptions);
			if (result.Count > 0)
			{
				Vector2 hit = (Vector2)result["position"];
				float distanceToHit = from.DistanceSquaredTo(hit);
				if (distanceToHit < shortestDistance)
				{
					shortestDistance = distanceToHit;
					indexOfShortestDistance = i;
				}
			}
			results.Add(result);
		}

		if (indexOfShortestDistance != -1)
		{
			Godot.Collections.Dictionary result = results[indexOfShortestDistance];
			Vector2 hit = (Vector2)result["position"];

			IsCollisionUp = true;
			Position = new Vector2(Position.x, hit.y + halfHeight);
			Velocity = new Vector2(Velocity.x, 0f);
		}
	}
	private void RayDown (float delta)
	{
		Physics2DDirectSpaceState spaceState = GetWorld2d().DirectSpaceState;

		float halfWidth = Width / 2;
		float halfHeight = Height / 2;

		Vector2 start = new Vector2((Position.x - halfWidth) + Margin, Position.y);
		Vector2 end = new Vector2((Position.x + halfWidth) - Margin, Position.y);

		float distance = halfHeight + Mathf.Abs(Velocity.y * delta);
		Vector2 direction = Vector2.Down;

		List<Godot.Collections.Dictionary> results = new List<Godot.Collections.Dictionary>();

		int indexOfShortestDistance = -1;
		float shortestDistance = Mathf.Inf;

		for (int i = 0; i < RayCount; i++)
		{
			float lerp = (float)i / (RayCount - 1);
			Vector2 from = start.LinearInterpolate(end, lerp);
			Vector2 to = from + (direction * distance);

			object[] exceptionsArray = new object[] { m_area2D };
			Godot.Collections.Array exceptions = new Godot.Collections.Array(exceptionsArray);
			Godot.Collections.Dictionary result = spaceState.IntersectRay(from, to, exceptions);
			if (result.Count > 0)
			{
				Vector2 hit = (Vector2)result["position"];
				float distanceToHit = from.DistanceSquaredTo(hit);
				if (distanceToHit < shortestDistance)
				{
					shortestDistance = distanceToHit;
					indexOfShortestDistance = i;
				}
			}
			results.Add(result);
		}

		if (indexOfShortestDistance != -1)
		{
			Godot.Collections.Dictionary result = results[indexOfShortestDistance];
			Vector2 hit = (Vector2)result["position"];

			IsCollisionDown = true;
			Position = new Vector2(Position.x, hit.y - halfHeight);
			Velocity = new Vector2(Velocity.x, 0f);
			JumpCount = 0;
		}
	}

	#endregion // Private methods

}
