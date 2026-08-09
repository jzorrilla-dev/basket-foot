using Godot;

public partial class ScoreZone : Area3D
{
	[Export] public float ThreePointRadius = 6.75f;
	[Export] public NodePath BallPath;

	private Ball _ball;
	private int _score;

	public override void _Ready()
	{
		if (BallPath != null)
		{
			_ball = GetNode<Ball>(BallPath);
		}
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_ball == null || body != _ball || !_ball.HasContact)
		{
			return;
		}

		Vector2 hoop = new(GlobalPosition.X, GlobalPosition.Z);
		Vector2 contact = new(_ball.LastContactPosition.X, _ball.LastContactPosition.Z);
		int points = contact.DistanceTo(hoop) > ThreePointRadius ? 3 : 2;

		_score += points;
		GD.Print($"Canasta! +{points} puntos (total: {_score})");
	}
}
