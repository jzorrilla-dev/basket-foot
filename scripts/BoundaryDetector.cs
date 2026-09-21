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
	[Export] public float RestartInset = 0.8f;
	[Export] public float ThrowInForwardDistance = 5.0f;
	[Export] public float ThrowInInfieldDistance = 6.0f;

	public enum BoundarySide { LateralLeft, LateralRight, GoalLineSouth, GoalLineNorth }

	private Ball _ball;
	private bool _restartPending;
	private BoundarySide _pendingSide;
	private Vector3 _pendingExitPosition;
	private Player _restartPlayer;
	private int _pendingRestartTeamId;
	private bool _pendingCorner;
	private float _cooldownTimer;

	public override void _Ready()
	{
		_ball = GetNode<Ball>(BallPath);

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
		_pendingExitPosition = _ball.GlobalPosition;

		Vector3 restartPosition = RestartPositionForSide(side, _pendingExitPosition);
		_pendingRestartTeamId = DetermineRestartTeam(side);
		_pendingCorner = IsCornerRestart(side);
		_restartPlayer = DetermineRestartPlayer(restartPosition, _pendingRestartTeamId);
	}

	private void ExecuteRestart()
	{
		if (_ball.IsFrozenForRestart || _ball.IsCarried)
			return;

		_cooldownTimer = RestartCooldown;
		_ball.FreezeForRestart();

		Vector3 restartPosition = RestartPositionForSide(_pendingSide, _pendingExitPosition);
		bool lateral = _pendingSide == BoundarySide.LateralLeft || _pendingSide == BoundarySide.LateralRight;
		Vector3 target = RestartTargetForSide(_pendingSide, restartPosition, _restartPlayer.TeamId);
		if (lateral)
		{
			_restartPlayer.PrepareThrowInRestart(restartPosition, target);
		}
		else
		{
			_restartPlayer.PrepareKickInRestart(restartPosition, target);
		}

		string type = lateral ? "Saque lateral" : (_pendingCorner ? "Córner" : "Saque de arco");
		string playerName = Player.TeamName(_restartPlayer.TeamId);
		EmitSignal(SignalName.RestartTriggered, type, playerName);
	}

	private int DetermineRestartTeam(BoundarySide side)
	{
		if (side == BoundarySide.LateralLeft || side == BoundarySide.LateralRight)
		{
			return OpposingTeam(LastTouchTeamId());
		}

		int defendingTeamId = side == BoundarySide.GoalLineSouth ? 0 : 1;
		// Si el defensor desvió el balón fuera durante un ataque rival, es córner
		// para el atacante. Si el atacante fue el último, es saque de arco.
		return OpposingTeam(LastTouchTeamId(defendingTeamId));
	}

	private bool IsCornerRestart(BoundarySide side)
	{
		if (side == BoundarySide.LateralLeft || side == BoundarySide.LateralRight)
			return false;

		int defendingTeamId = side == BoundarySide.GoalLineSouth ? 0 : 1;
		return LastTouchTeamId(defendingTeamId) == defendingTeamId;
	}

	private int LastTouchTeamId(int fallback = 0)
	{
		return _ball.LastTouchPlayer is Player lastTouch ? lastTouch.TeamId : fallback;
	}

	private static int OpposingTeam(int teamId) => teamId == 0 ? 1 : 0;

	private Player DetermineRestartPlayer(Vector3 restartPosition, int restartTeamId)
	{

		Player closest = null;
		float bestDistanceSq = float.PositiveInfinity;
		foreach (Player player in Player.Players)
		{
			if (!IsInstanceValid(player) || player.TeamId != restartTeamId)
				continue;

			float distanceSq = player.GlobalPosition.DistanceSquaredTo(restartPosition);
			if (distanceSq < bestDistanceSq)
			{
				bestDistanceSq = distanceSq;
				closest = player;
			}
		}

		if (closest != null)
			return closest;

		foreach (Player player in Player.Players)
		{
			if (IsInstanceValid(player) && player.TeamId == restartTeamId)
				return player;
		}

		return GetNode<Player>(Player1Path);
	}

	private Vector3 RestartPositionForSide(BoundarySide side, Vector3 exitPosition)
	{
		float x = Mathf.Clamp(exitPosition.X, -CourtHalfWidth + RestartInset, CourtHalfWidth - RestartInset);
		float z = Mathf.Clamp(exitPosition.Z, -CourtHalfLength + RestartInset, CourtHalfLength - RestartInset);

		if (side == BoundarySide.LateralLeft)
			x = -CourtHalfWidth + RestartInset;
		else if (side == BoundarySide.LateralRight)
			x = CourtHalfWidth - RestartInset;
		else if (side == BoundarySide.GoalLineSouth)
		{
			z = -CourtHalfLength + RestartInset;
			if (_pendingCorner)
				x = exitPosition.X < 0.0f ? -CourtHalfWidth + RestartInset : CourtHalfWidth - RestartInset;
		}
		else if (side == BoundarySide.GoalLineNorth)
		{
			z = CourtHalfLength - RestartInset;
			if (_pendingCorner)
				x = exitPosition.X < 0.0f ? -CourtHalfWidth + RestartInset : CourtHalfWidth - RestartInset;
		}

		return new Vector3(x, 1.0f, z);
	}

	private Vector3 RestartTargetForSide(BoundarySide side, Vector3 restartPosition, int restartTeamId)
	{
		Vector3 target = restartPosition;
		if (side == BoundarySide.LateralLeft || side == BoundarySide.LateralRight)
		{
			float inward = side == BoundarySide.LateralLeft ? 1.0f : -1.0f;
			Vector3 attackHoop = restartTeamId == 0
				? new Vector3(0.0f, 3.0f, -CourtHalfLength)
				: new Vector3(0.0f, 3.0f, CourtHalfLength);
			Vector3 attackDir = attackHoop - restartPosition;
			attackDir.Y = 0.0f;
			attackDir = attackDir.LengthSquared() > 0.001f ? attackDir.Normalized() : Vector3.Forward;
			target += Vector3.Right * inward * ThrowInInfieldDistance + attackDir * ThrowInForwardDistance;
		}
		else
		{
			Player receiver = FindRestartReceiver(restartPosition, restartTeamId);
			if (receiver != null)
			{
				target = receiver.GlobalPosition;
			}
			else
			{
				target.Z = side == BoundarySide.GoalLineSouth ? -CourtHalfLength + 5.0f : CourtHalfLength - 5.0f;
				target.X = 0.0f;
			}
		}

		target.X = Mathf.Clamp(target.X, -CourtHalfWidth + RestartInset, CourtHalfWidth - RestartInset);
		target.Z = Mathf.Clamp(target.Z, -CourtHalfLength + RestartInset, CourtHalfLength - RestartInset);
		target.Y = 0.3f;
		return target;
	}

	private Player FindRestartReceiver(Vector3 restartPosition, int restartTeamId)
	{
		Player receiver = null;
		float bestDistanceSq = float.PositiveInfinity;
		foreach (Player player in Player.Players)
		{
			if (!IsInstanceValid(player) || player.TeamId != restartTeamId || player == _restartPlayer)
				continue;

			float distanceSq = player.GlobalPosition.DistanceSquaredTo(restartPosition);
			if (distanceSq < bestDistanceSq)
			{
				bestDistanceSq = distanceSq;
				receiver = player;
			}
		}
		return receiver;
	}
}
