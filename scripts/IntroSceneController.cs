using Godot;

public class IntroSceneController : Node2D
{

	private float m_timer = 0f;
	private float m_timeLimit = 1f;

	public override void _Ready ()
	{
		base._Ready();

		AudioStreamPlayer audioStreamPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer_IntroSound");
		AudioStream audioStream = audioStreamPlayer.Stream;
		m_timeLimit = audioStream.GetLength();
	}

	public override void _UnhandledKeyInput (InputEventKey @event)
	{
		GetTree().ChangeScene("res://scenes/title_scene.tscn");
	}

	public override void _Process (float delta)
	{
		base._Process(delta);

		if (m_timer < m_timeLimit)
		{
			m_timer += delta;
			m_timer = Mathf.Clamp(m_timer, 0f, m_timeLimit);

			if (m_timer == m_timeLimit)
			{
				GetTree().ChangeScene("res://scenes/title_scene.tscn");
			}
		}
	}

}
