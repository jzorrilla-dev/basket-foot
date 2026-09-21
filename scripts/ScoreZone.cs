using Godot;

public partial class ScoreZone : Area3D
{
	[Signal] public delegate void ShotScoredEventHandler(int points, int scorerTeamId);

	[Export] public float ThreePointRadius = 6.75f;
	[Export] public float EntryRadius = 0.3f;
	[Export] public NodePath BallPath;
	[Export(PropertyHint.Range, "-1,1,1")] public int ScoringTeamId = -1;

	private Ball _ball;
	private float _prevBallY = float.NaN;

	public override void _Ready()
	{
		if (BallPath != null)
		{
			_ball = GetNode<Ball>(BallPath);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_ball == null)
		{
			return;
		}

		float y = _ball.GlobalPosition.Y;
		float hoopY = GlobalPosition.Y;

		if (!float.IsNaN(_prevBallY) && _prevBallY >= hoopY && y < hoopY
			&& _ball.HasContact && !_ball.ScoredFlag)
		{
			Vector2 hoop = new(GlobalPosition.X, GlobalPosition.Z);
			Vector2 ballPos = new(_ball.GlobalPosition.X, _ball.GlobalPosition.Z);

			if (ballPos.DistanceTo(hoop) <= EntryRadius)
			{
				Vector2 contact = new(_ball.LastContactPosition.X, _ball.LastContactPosition.Z);
				int points = contact.DistanceTo(hoop) > ThreePointRadius ? 3 : 2;
				int scorerTeamId = ResolveScoringTeamId();

				_ball.MarkScored();
				EmitSignal(SignalName.ShotScored, points, scorerTeamId);
				GD.Print($"Canasta! +{points} puntos ({Player.TeamName(scorerTeamId)})");
			}
		}

		_prevBallY = y;
	}

	private int ResolveScoringTeamId()
	{
		if (ScoringTeamId is 0 or 1)
			return ScoringTeamId;

		// Respaldo para escenas antiguas: Azul ataca el aro sur y Rojo el norte.
		return GlobalPosition.Z < 0.0f ? 0 : 1;
	}
}
