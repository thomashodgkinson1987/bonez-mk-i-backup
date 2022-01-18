using Godot;

public class CreditsSceneController : Node2D
{

	#region Nodes

	private Panel m_fader;

	#endregion // Nodes



	#region Exports

	[Export] private float m_timeLimit = 2f;

	#endregion // Exports



	#region Fields

	private bool m_hasKeyBeenPressed = false;

	private float m_elapsedTime = 0f;
	private float m_elapsedTime2 = 0f;

	#endregion // Fields



	#region Node2D methods

	public override void _Ready ()
	{
		base._Ready();

		m_fader = GetNode<Panel>("CanvasLayer/Fader");
	}

	public override void _Process (float delta)
	{
		base._Process(delta);

		m_elapsedTime2 += delta;

		if (m_hasKeyBeenPressed)
		{
			if (m_elapsedTime < m_timeLimit)
			{
				m_elapsedTime += delta;
				m_elapsedTime = Mathf.Clamp(m_elapsedTime, 0f, m_timeLimit);

				Color color = m_fader.Modulate;
				color.a = m_elapsedTime / m_timeLimit;
				m_fader.Modulate = color;

				if (m_elapsedTime == m_timeLimit)
				{
					LoadIntroScene();
				}
			}
		}
	}

	public override void _UnhandledKeyInput (InputEventKey @event)
	{
		base._UnhandledKeyInput(@event);

		if (m_elapsedTime2 >= 1f)
		{
			m_hasKeyBeenPressed = true;
		}
	}

	#endregion // Node2D methods



	#region Private methods

	private void LoadIntroScene ()
	{
		GetTree().ChangeScene("res://scenes/intro_scene.tscn");
	}

	#endregion // Private methods

}
