using System.Collections.Generic;

using Godot;

public class GameSceneController : Node2D
{

	private class Snapshot
	{
		public Vector2 Position { get; set; } = Vector2.Zero;

		public bool IsFacingRight { get; set; } = true;

		public string AnimationName { get; set; } = "default";
		public int AnimationFrame { get; set; } = 0;

		public float Acceleration { get; set; } = 2000f;
		public float Deceleration { get; set; } = 4000f;

		public Vector2 Velocity { get; set; } = Vector2.Zero;
		public Vector2 MaxVelocity { get; set; } = new Vector2(400f, 1000f);
		public bool IsClampVelocity { get; set; } = true;

		public float Gravity { get; set; } = 200f;
		public bool IsApplyGravity { get; set; } = true;

		public Vector2 InputDirection { get; set; } = Vector2.Zero;
		public bool IsInputJump { get; set; } = false;
		public bool WasInputJump { get; set; } = false;
		public bool IsInputEnabled { get; set; } = true;

		public bool IsResolveInputDirectionX { get; set; } = true;
		public bool IsResolveInputDirectionY { get; set; } = true;

		public int JumpCount { get; set; } = 0;
		public int JumpLimit { get; set; } = 2;
		public int JumpHeight { get; set; } = 32;
		public bool IsJumpQueued { get; set; } = false;

		public int RayCount { get; set; } = 3;
		public float Margin { get; set; } = 0.1f;

		public bool IsCollisionLeft { get; set; } = false;
		public bool IsCollisionRight { get; set; } = false;
		public bool IsCollisionUp { get; set; } = false;
		public bool IsCollisionDown { get; set; } = false;

		public bool WasCollisionLeft { get; set; } = false;
		public bool WasCollisionRight { get; set; } = false;
		public bool WasCollisionUp { get; set; } = false;
		public bool WasCollisionDown { get; set; } = false;

		public bool IsCollisionEnabled { get; set; } = true;

		public bool IsJumping { get; set; } = false;
		public bool IsFalling { get; set; } = false;
		public bool IsDying { get; set; } = false;

		public bool IsUpdateAnimation { get; set; } = true;
	}

	#region Nodes

	private AudioStreamPlayer m_audioStreamPlayer_CollectCoin;
	private AudioStreamPlayer m_audioStreamPlayer_CollectKey;
	private AudioStreamPlayer m_audioStreamPlayer_PlayerDeath;

	private Actor m_player;
	private CameraController m_cameraController;

	private Label m_coinsCollectedLabel;
	private Label m_dividerLabel;
	private Label m_coinsLeftLabel;

	private Node2D m_levelHolder;

	#endregion // Noded



	#region Exports

	[Export] public string LevelToLoad { get; set; } = "default_level";
	[Export] public string CurrentLevel { get; set; } = "default_level";
	[Export] public string PreviousLevel { get; set; } = "default_level";

	#endregion // Exports



	#region Properties

	public float ElapsedTime { get; set; } = 0f;
	public int TotalCoinsCollected { get; set; } = 0;
	public int CoinsCollectable { get; set; } = 0;

	#endregion // Properties



	#region Fields

	private int m_coinsCollected { get; set; } = 0;

	private bool m_isLoadLevel_Phase1 { get; set; } = false;
	private bool m_isLoadLevel_Phase2 { get; set; } = false;
	private bool m_isLoadLevel_Phase3 { get; set; } = false;
	private bool m_isLoadLevel_Complete { get; set; } = false;

	private int m_frameCounter { get; set; } = 0;
	private int m_playerDeathFrameCounter { get; set; } = 0;

	private List<Snapshot> m_snapshots = new List<Snapshot>();

	private bool m_wasReversing = false;
	private bool m_isReversing = false;
	private float m_reverseLimit = 300;

	private Label m_elapsedTimeLabel;
	private Label m_totalCoinsCollectedLabel;

	#endregion // Fields



	#region Node2D methods

	public override void _Ready ()
	{
		m_audioStreamPlayer_CollectCoin = GetNode<AudioStreamPlayer>("AudioStreamPlayers/AudioStreamPlayer_CollectCoin");
		m_audioStreamPlayer_CollectKey = GetNode<AudioStreamPlayer>("AudioStreamPlayers/AudioStreamPlayer_CollectKey");
		m_audioStreamPlayer_PlayerDeath = GetNode<AudioStreamPlayer>("AudioStreamPlayers/AudioStreamPlayer_PlayerDied");

		m_player = GetNode<Actor>("Player");
		m_cameraController = GetNode<CameraController>("Camera2D");
		m_levelHolder = GetNode<Node2D>("LevelHolder");

		m_coinsCollectedLabel = GetNode<Label>("CanvasLayer/Panel/CoinsCollected");
		m_dividerLabel = GetNode<Label>("CanvasLayer/Panel/Divider");
		m_coinsLeftLabel = GetNode<Label>("CanvasLayer/Panel/CoinsLeft");

		m_elapsedTimeLabel = GetNode<Label>("CanvasLayer/Panel2/Label");
		m_totalCoinsCollectedLabel = GetNode<Label>("CanvasLayer/Panel2/Label2");

		LoadLevel(LevelToLoad);
	}

	public override void _Process (float delta)
	{
		base._Process(delta);

		ElapsedTime += delta;

		m_elapsedTimeLabel.Text = ElapsedTime.ToString("0.00");

		m_frameCounter++;

		if (m_isLoadLevel_Phase1)
		{
			m_isLoadLevel_Phase1 = false;
			LoadLevel_Phase1();
			m_isLoadLevel_Phase2 = true;
			return;
		}
		else if (m_isLoadLevel_Phase2)
		{
			m_isLoadLevel_Phase2 = false;
			LoadLevel_Phase2();
			m_isLoadLevel_Phase3 = true;
			return;
		}
		else if (m_isLoadLevel_Phase3)
		{
			m_isLoadLevel_Phase3 = false;
			LoadLevel_Phase3();
			m_isLoadLevel_Complete = true;
			return;
		}
		else if (m_isLoadLevel_Complete)
		{
			m_isLoadLevel_Complete = false;
			LoadLevel_Complete();
			return;
		}
		else if (m_player.IsDying)
		{
			if (m_frameCounter >= m_playerDeathFrameCounter)
			{
				ReloadLevel();
				m_player.IsDying = false;
				m_player.IsCollisionEnabled = true;
				m_player.IsInputEnabled = true;
				m_player.IsApplyGravity = true;
				m_player.IsUpdateAnimation = true;
			}
			return;
		}

		m_wasReversing = m_isReversing;
		m_isReversing = false;

		if (Input.IsKeyPressed((int)KeyList.K))
		{
			m_isReversing = true;
		}

		if (m_isReversing)
		{
			if (!m_wasReversing)
			{
				m_player.SetProcess(false);
				m_player.SetPhysicsProcess(false);
			}
			if (m_snapshots.Count > 0)
			{
				SetPlayerToSnapshot(m_snapshots[m_snapshots.Count - 1]);
				m_snapshots.RemoveAt(m_snapshots.Count - 1);
			}
		}
		else
		{
			if (m_wasReversing)
			{
				m_player.SetProcess(true);
				m_player.SetPhysicsProcess(true);
			}

			if (m_snapshots.Count < m_reverseLimit - 1)
			{
				m_snapshots.Add(GetSnapshot());
			}
			else
			{
				m_snapshots.RemoveAt(0);
				m_snapshots.Add(GetSnapshot());
			}
		}
	}

	#endregion // Node2D methods



	#region Area2D callback methods

	private void OnPlayerEntered (Area2D area)
	{
		if (m_player.IsDying) return;

		if (area.GetParent() != null && area.GetParent().IsInGroup("LoadCreditsZone"))
		{
			GetTree().ChangeScene("res://scenes/credits_scene.tscn");
			return;
		}
		else
		{
			Node2D parent = area.GetParent<Node2D>();
			if (parent != null && !parent.IsQueuedForDeletion())
			{
				if (parent.IsInGroup("Coins"))
				{
					m_audioStreamPlayer_CollectCoin.Play();
					m_coinsCollected++;
					parent.QueueFree();
					m_coinsCollectedLabel.Text = m_coinsCollected.ToString();
				}
				else if (
					(parent.GetParent<Node2D>() != null && !parent.GetParent<Node2D>().IsQueuedForDeletion() && parent.GetParent<Node2D>().IsInGroup("DeathZones"))
					||
					(parent.IsInGroup("DeathZones"))
					)
				{
					if (!m_player.IsDying)
					{
						m_audioStreamPlayer_PlayerDeath.Play();
						m_playerDeathFrameCounter = m_frameCounter + 120;

						m_player.Reset();
						m_player.IsDying = true;
						m_player.IsCollisionEnabled = false;
						m_player.IsInputEnabled = false;
						m_player.IsApplyGravity = false;
						m_player.IsUpdateAnimation = false;
						m_player.GetNode<AnimatedSprite>("AnimatedSprite").Play("death");
					}
				}
				else if (parent.IsInGroup("SilverKeys"))
				{
					m_audioStreamPlayer_CollectKey.Play();
					parent.QueueFree();
					for (int i = 0; i < m_levelHolder.GetNode("Level/SilverLocks").GetChildCount(); i++)
					{
						Node2D silverLock = m_levelHolder.GetNode<Node2D>("Level/SilverLocks").GetChild<Node2D>(i);
						AnimatedSprite animatedSprite = silverLock.GetNode<AnimatedSprite>("AnimatedSprite");
						animatedSprite.Play("unlock");
						silverLock.GetNode<StaticBody2D>("StaticBody2D").QueueFree();
					}
				}
				else if (parent.IsInGroup("GoldKeys"))
				{
					m_audioStreamPlayer_CollectKey.Play();
					parent.QueueFree();
					for (int i = 0; i < m_levelHolder.GetNode("Level/GoldLocks").GetChildCount(); i++)
					{
						Node2D goldLock = m_levelHolder.GetNode<Node2D>("Level/GoldLocks").GetChild<Node2D>(i);
						AnimatedSprite animatedSprite = goldLock.GetNode<AnimatedSprite>("AnimatedSprite");
						animatedSprite.Play("unlock");
						goldLock.GetNode<StaticBody2D>("StaticBody2D").QueueFree();
					}
				}
				else if (parent.IsInGroup("LoadLevelZones"))
				{
					string levelName = ((LoadLevelZone)parent).LevelName;
					LoadLevel(levelName);
				}
			}
		}
	}

	#endregion // Area2D callbacks



	#region Private methods

	private void LoadLevel (string levelName)
	{
		if (levelName != CurrentLevel)
		{
			TotalCoinsCollected += m_coinsCollected;
			m_totalCoinsCollectedLabel.Text = "Coins: " + TotalCoinsCollected.ToString();
			PreviousLevel = CurrentLevel;
		}

		m_isLoadLevel_Phase1 = true;
		m_isLoadLevel_Phase2 = false;
		m_isLoadLevel_Phase3 = false;
		m_isLoadLevel_Complete = false;

		LevelToLoad = levelName;
		CurrentLevel = levelName;

		m_player.Visible = false;
		Node2D level = m_levelHolder.GetNode<Node2D>("Level");
		level.Visible = false;
	}

	private void LoadLevel_Phase1 ()
	{
		RemoveChild(m_player);
		m_player.Reset();

		Node2D oldLevel = m_levelHolder.GetNode<Node2D>("Level");
		m_levelHolder.RemoveChild(oldLevel);
		oldLevel.QueueFree();
	}

	private void LoadLevel_Phase2 ()
	{
		PackedScene levelPackedScene = GD.Load<PackedScene>("res://packed_scenes/levels/" + LevelToLoad + ".tscn");
		Node2D level = (Node2D)levelPackedScene.Instance();
		level.Name = "Level";
		level.Visible = false;
		m_levelHolder.AddChild(level);
	}

	private void LoadLevel_Phase3 ()
	{
		Node2D level = m_levelHolder.GetNode<Node2D>("Level");
		Node2D coins = level.GetNode<Node2D>("Coins");
		Node2D spawnPoint = level.GetNode<Node2D>("SpawnPoint");

		m_coinsCollected = 0;
		m_coinsCollectedLabel.Text = "0";
		m_coinsLeftLabel.Text = coins.GetChildCount().ToString();

		if (PreviousLevel != CurrentLevel)
		{
			CoinsCollectable += coins.GetChildCount();
		}

		m_player.Position = spawnPoint.Position;
		AddChild(m_player);

		m_cameraController.Position = m_player.Position;
		m_cameraController.Target = m_player;

		Node2D tileMaps = level.GetNode<Node2D>("TileMaps");
		TileMap cameraBoundsTileMap = tileMaps.GetNode<TileMap>("TileMap_CameraBounds");
		Rect2 cameraBounds = cameraBoundsTileMap.GetUsedRect();
		float cellSizeX = cameraBoundsTileMap.CellSize.x;
		float cellSizeY = cameraBoundsTileMap.CellSize.y;

		float posX = cameraBounds.Position.x * cellSizeX;
		float posY = cameraBounds.Position.y * cellSizeY;
		float sizeX = cameraBounds.Size.x * cellSizeX;
		float sizeY = cameraBounds.Size.y * cellSizeY;

		m_cameraController.BoundsRect = new Rect2(posX, posY, sizeX, sizeY);
	}

	private void LoadLevel_Complete ()
	{
		m_snapshots.Clear();

		Node2D level = m_levelHolder.GetNode<Node2D>("Level");
		level.Visible = true;
		m_player.Visible = true;
	}

	private void ReloadLevel ()
	{
		LoadLevel(CurrentLevel);
	}

	private Snapshot GetSnapshot ()
	{
		Snapshot snapshot = new Snapshot();

		snapshot.Position = m_player.Position;

		snapshot.IsFacingRight = m_player.GetNode<AnimatedSprite>("AnimatedSprite").FlipH;

		snapshot.AnimationName = m_player.GetNode<AnimatedSprite>("AnimatedSprite").Animation;
		snapshot.AnimationFrame = m_player.GetNode<AnimatedSprite>("AnimatedSprite").Frame;

		snapshot.Acceleration = m_player.Acceleration;
		snapshot.Deceleration = m_player.Deceleration;

		snapshot.Velocity = m_player.Velocity;
		snapshot.MaxVelocity = m_player.MaxVelocity;
		snapshot.IsClampVelocity = m_player.IsClampVelocity;

		snapshot.Gravity = m_player.Gravity;
		snapshot.IsApplyGravity = m_player.IsApplyGravity;

		snapshot.InputDirection = m_player.InputDirection;
		snapshot.IsInputJump = m_player.IsInputJump;
		snapshot.WasInputJump = m_player.WasInputJump;
		snapshot.IsInputEnabled = m_player.IsInputEnabled;

		snapshot.IsResolveInputDirectionX = m_player.IsResolveInputDirectionX;
		snapshot.IsResolveInputDirectionY = m_player.IsResolveInputDirectionY;

		snapshot.JumpCount = m_player.JumpCount;
		snapshot.JumpLimit = m_player.JumpLimit;
		snapshot.JumpHeight = m_player.JumpHeight;
		snapshot.IsJumpQueued = m_player.IsJumpQueued;

		snapshot.RayCount = m_player.RayCount;
		snapshot.Margin = m_player.Margin;

		snapshot.IsCollisionLeft = m_player.IsCollisionLeft;
		snapshot.IsCollisionRight = m_player.IsCollisionRight;
		snapshot.IsCollisionUp = m_player.IsCollisionUp;
		snapshot.IsCollisionDown = m_player.IsCollisionDown;

		snapshot.WasCollisionLeft = m_player.WasCollisionLeft;
		snapshot.WasCollisionRight = m_player.WasCollisionRight;
		snapshot.WasCollisionUp = m_player.WasCollisionUp;
		snapshot.WasCollisionDown = m_player.WasCollisionDown;

		snapshot.IsCollisionEnabled = m_player.IsCollisionEnabled;

		snapshot.IsJumping = m_player.IsJumping;
		snapshot.IsFalling = m_player.IsFalling;
		snapshot.IsDying = m_player.IsDying;

		snapshot.IsUpdateAnimation = m_player.IsUpdateAnimation;

		return snapshot;
	}

	private void SetPlayerToSnapshot (Snapshot snapshot)
	{
		m_player.Position = snapshot.Position;

		m_player.GetNode<AnimatedSprite>("AnimatedSprite").FlipH = snapshot.IsFacingRight;

		m_player.GetNode<AnimatedSprite>("AnimatedSprite").Animation = snapshot.AnimationName;
		m_player.GetNode<AnimatedSprite>("AnimatedSprite").Frame = snapshot.AnimationFrame;

		m_player.Acceleration = snapshot.Acceleration;
		m_player.Deceleration = snapshot.Deceleration;

		m_player.Velocity = snapshot.Velocity;
		m_player.MaxVelocity = snapshot.MaxVelocity;
		m_player.IsClampVelocity = snapshot.IsClampVelocity;

		m_player.Gravity = snapshot.Gravity;
		m_player.IsApplyGravity = snapshot.IsApplyGravity;

		m_player.InputDirection = snapshot.InputDirection;
		m_player.IsInputJump = snapshot.IsInputJump;
		m_player.WasInputJump = snapshot.WasInputJump;
		m_player.IsInputEnabled = snapshot.IsInputEnabled;

		m_player.IsResolveInputDirectionX = snapshot.IsResolveInputDirectionX;
		m_player.IsResolveInputDirectionY = snapshot.IsResolveInputDirectionY;

		m_player.JumpCount = snapshot.JumpCount;
		m_player.JumpLimit = snapshot.JumpLimit;
		m_player.JumpHeight = snapshot.JumpHeight;
		m_player.IsJumpQueued = snapshot.IsJumpQueued;

		m_player.RayCount = snapshot.RayCount;
		m_player.Margin = snapshot.Margin;

		m_player.IsCollisionLeft = snapshot.IsCollisionLeft;
		m_player.IsCollisionRight = snapshot.IsCollisionRight;
		m_player.IsCollisionUp = snapshot.IsCollisionUp;
		m_player.IsCollisionDown = snapshot.IsCollisionDown;

		m_player.WasCollisionLeft = snapshot.WasCollisionLeft;
		m_player.WasCollisionRight = snapshot.WasCollisionRight;
		m_player.WasCollisionUp = snapshot.WasCollisionUp;
		m_player.WasCollisionDown = snapshot.WasCollisionDown;

		m_player.IsCollisionEnabled = snapshot.IsCollisionEnabled;

		m_player.IsJumping = snapshot.IsJumping;
		m_player.IsFalling = snapshot.IsFalling;
		//m_player.IsDying = snapshot.IsDying;

		m_player.IsUpdateAnimation = snapshot.IsUpdateAnimation;
	}

	#endregion // Private methods

}
