using Godot;

public class Bat : Node2D
{

	#region Nodes

	private Node2D m_view;
	private Node2D m_nodes;

	#endregion // Nodes



	#region Exports

	[Export] public float TimeToMove { get; set; } = 1f;

	#endregion // Exports



	#region Fields

	private int m_fromNodeIndex = 0;
	private int m_toNodeIndex = 1;

	private float m_timer = 0f;

	#endregion // Fields



	#region Node2D methods

	public override void _Ready ()
	{
		base._Ready();

		m_view = GetNode<Node2D>("View");
		m_nodes = GetNode<Node2D>("Nodes");

		m_view.GetNode<AnimatedSprite>("AnimatedSprite").Play("anim_0001");
		m_view.GetNode<AnimatedSprite>("AnimatedSprite").Frame = 0;
	}

	public override void _PhysicsProcess (float delta)
	{
		base._PhysicsProcess(delta);

		if (m_toNodeIndex >= m_nodes.GetChildCount()
			|| m_fromNodeIndex >= m_nodes.GetChildCount())
		{
			m_toNodeIndex = 0;
			m_fromNodeIndex = 1;
			m_timer = 0f;
			return;
		}

		if (m_timer < TimeToMove)
		{
			Node2D toNode = m_nodes.GetChild<Node2D>(m_toNodeIndex);
			Node2D fromNode = m_nodes.GetChild<Node2D>(m_fromNodeIndex);

			m_timer += delta;
			m_timer = Mathf.Clamp(m_timer, 0f, TimeToMove);

			float lerp = m_timer / TimeToMove;
			m_view.Position = fromNode.Position.LinearInterpolate(toNode.Position, lerp);

			if (m_timer == TimeToMove)
			{
				m_timer = 0f;

				m_fromNodeIndex = m_toNodeIndex;

				if (m_toNodeIndex + 1 == m_nodes.GetChildCount())
				{
					m_toNodeIndex = 0;
				}
				else
				{
					m_toNodeIndex++;
				}
			}
		}
	}

	#endregion // Node2D methods

}
