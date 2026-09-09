using Godot;

public partial class BoundaryDetector : Node3D
{
	[Signal] public delegate void RestartTriggeredEventHandler(string restartType, string playerName);

	[Export] public NodePath BallPath;
	[Export] public NodePath Player1Path;
	[Export] public NodePath Player2Path;
	[Export] public float CourtHalfWidth = 10.0f;
	[Export] public float CourtHalfLength = 20.0f;
	[Export] public Vector3 RestartPosition = new(0, 1.0f, 0);
	[Export] public float RestartCooldown = 1.5f;

	public enum BoundarySide { LateralLeft, LateralRight, GoalLineSouth, GoalLineNorth }

	private Ball _ball;
	private Player _player1;
	private Player _player2;
	private bool _restartPending;
	private BoundarySide _pendingSide;
	private Player _restartPlayer;
	private float _cooldownTimer;

	public override void _Ready()
	{
		_ball = GetNode<Ball>(BallPath);
		_player1 = GetNode<Player>(Player1Path);
		_player2 = GetNode<Player>(Player2Path);

		var lateralLeft = GetNode<Area3D>("LateralLeft");
		var lateralRight = GetNode<Area3D>("LateralRight");
		var goalSouth = GetNode<Area3D>("GoalLineSouth");
		var goalNorth = GetNode<Area3D>("GoalLineNorth");

		lateralLeft.BodyEntered += (body) => OnBoundaryEntered(body, BoundarySide.LateralLeft);
		lateralRight.BodyEntered += (body) => OnBoundaryEntered(body, BoundarySide.LateralRight);
		goalSouth.BodyEntered += (body) => OnBoundaryEntered(body, BoundarySide.GoalLineSouth);
		goalNorth.BodyEntered += (body) => OnBoundaryEntered(body, BoundarySide.GoalLineNorth);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_cooldownTimer > 0)
			_cooldownTimer -= (float)delta;

		if (_restartPending)
		{
			_restartPending = false;
			ExecuteRestart();
		}
	}

	private void OnBoundaryEntered(Node3D body, BoundarySide side)
	{
		if (body != _ball || _ball.IsCarried || _ball.IsFrozenForRestart || _restartPending || _cooldownTimer > 0)
			return;

		_restartPending = true;
		_pendingSide = side;

		_restartPlayer = DetermineRestartPlayer();
	}

	private void ExecuteRestart()
	{
		if (_ball.IsFrozenForRestart || _ball.IsCarried)
			return;

		_cooldownTimer = RestartCooldown;
		_ball.FreezeForRestart();
		_restartPlayer.PrepareCenterRestart(RestartPosition);

		string type = "Reposición";
		string playerName = _restartPlayer.IsAI ? "Equipo Rojo" : "Equipo Azul";
		EmitSignal(SignalName.RestartTriggered, type, playerName);
	}

	private Player DetermineRestartPlayer()
	{
		if (_ball.LastTouchPlayer == _player1)
			return _player2;

		if (_ball.LastTouchPlayer == _player2)
			return _player1;

		return _player1;
	}
}
