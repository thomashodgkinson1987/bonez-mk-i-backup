using Godot;

public class TitleSceneController : Node2D
{

	private void OnStartButtonPressed ()
	{
		GetTree().ChangeScene("res://scenes/game_scene.tscn");
	}

	private void OnQuitButtonPressed ()
	{
		GetTree().Quit();
	}

}
