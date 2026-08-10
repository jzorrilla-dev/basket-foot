using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 6.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public NodePath BallPath;
	[Export] public NodePath VisualPath;
	[Export] public float GrabRadius = 1.0f;
	[Export] public float GrabMaxHeight = 1.2f;
	[Export] public float MaxTurnSpeedDeg = 270.0f;
	[Export] public Vector3 HoopPosition = new(0, 3.0f, -4.9f);
	[Export] public float KickAngleDegrees = 65.0f;
	[Export] public float MinKickSpeed = 5.0f;
	[Export] public float MaxKickSpeed = 18.0f;
	[Export] public float MinKickDistance = 3.0f;
	[Export] public float KickAimPastCenter = 0.12f;
	[Export] public float KickGrabCooldown = 0.4f;
	[Export] public float BounceForwardSpeed = 2.0f;
	[Export] public float BounceUpSpeed = 3.5f;
	[Export] public float BounceGrabCooldown = 0.6f;
	[Export] public float VolleyRadius = 1.5f;
	[Export] public float VolleyMaxHeight = 2.0f;
	[Export] public Vector3 RespawnPosition = new(0, 1, 1);
	[Export] public float RespawnBelow = -3.0f;

	private Ball _ball;
	private Node3D _visual;
	private float _facingAngle;
	private float _kickCooldown;
	private bool _canVolley;

	public Vector3 FacingDirection => new(Mathf.Sin(_facingAngle), 0, -Mathf.Cos(_facingAngle));

	public override void _Ready()
	{
		if (BallPath != null)
		{
			_ball = GetNode<Ball>(BallPath);
		}
		if (VisualPath != null)
		{
			_visual = GetNode<Node3D>(VisualPath);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		if (GlobalPosition.Y < RespawnBelow)
		{
			GlobalPosition = RespawnPosition;
			Velocity = Vector3.Zero;
		}

		UpdateFacing(direction, (float)delta);

		if (_ball == null)
		{
			return;
		}

		if (_kickCooldown > 0)
		{
			_kickCooldown -= (float)delta;
		}

		if (_ball.IsCarried)
		{
			_ball.RecordContact(GlobalPosition);

			if (Input.IsActionJustPressed("bounce"))
			{
				Vector3 pop = FacingDirection * BounceForwardSpeed + Vector3.Up * BounceUpSpeed;
				_ball.Release(pop);
				_kickCooldown = BounceGrabCooldown;
				_canVolley = true;
			}
			else if (Input.IsActionJustPressed("kick"))
			{
				_ball.Release(ComputeKickVelocity(_ball.GlobalPosition, _ball.CarryDirection));
				_kickCooldown = KickGrabCooldown;
				_canVolley = false;
			}
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is Ball ball)
			{
				ball.RecordContact(GlobalPosition);
			}
		}

		if (_canVolley && Input.IsActionJustPressed("kick"))
		{
			Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
			toBall.Y = 0;
			if (toBall.Length() < VolleyRadius && _ball.GlobalPosition.Y < VolleyMaxHeight)
			{
				_ball.RecordContact(GlobalPosition);
				_ball.Release(ComputeKickVelocity(_ball.GlobalPosition, FacingDirection));
				_kickCooldown = KickGrabCooldown;
				_canVolley = false;
			}
		}

		Vector3 grabToBall = _ball.GlobalPosition - GlobalPosition;
		grabToBall.Y = 0;
		if (_kickCooldown <= 0 && grabToBall.Length() < GrabRadius && _ball.GlobalPosition.Y < GrabMaxHeight)
		{
			_ball.Grab(this);
		}
	}

	private void UpdateFacing(Vector3 moveDir, float delta)
	{
		float turn = Input.GetAxis("turn_left", "turn_right");
		if (turn != 0.0f)
		{
			// Giro manual con Q/E: tiene prioridad y permite dar vueltas completas.
			// Positivo = horario visto desde arriba (girar a la derecha).
			_facingAngle = Mathf.Wrap(_facingAngle + turn * Mathf.DegToRad(MaxTurnSpeedDeg) * delta, -Mathf.Pi, Mathf.Pi);
		}
		else if (moveDir.LengthSquared() > 0.001f)
		{
			float target = Mathf.Atan2(moveDir.X, -moveDir.Z);
			float diff = Mathf.AngleDifference(_facingAngle, target);
			float maxStep = Mathf.DegToRad(MaxTurnSpeedDeg) * delta;
			_facingAngle = Mathf.Abs(diff) <= maxStep ? target : _facingAngle + Mathf.Sign(diff) * maxStep;
		}

		if (_visual != null)
		{
			_visual.Rotation = new Vector3(0, _facingAngle, 0);
		}
	}

	private Vector3 ComputeKickVelocity(Vector3 launchPos, Vector3 dir)
	{
		Vector3 toHoop = HoopPosition - launchPos;
		float distance = new Vector3(toHoop.X, 0, toHoop.Z).Length();
		float deltaHeight = HoopPosition.Y - launchPos.Y;

		float angle = Mathf.DegToRad(KickAngleDegrees);
		float cosA = Mathf.Cos(angle);
		float tanA = Mathf.Tan(angle);

		// Apunta un poco detrás del centro del aro para que el balón pase con
		// margen sobre el aro frontal en vez de rozarlo al bajar.
		float dEff = Mathf.Max(distance + KickAimPastCenter, MinKickDistance);
		float denom = 2.0f * cosA * cosA * (dEff * tanA - deltaHeight);
		float gravity = Mathf.Abs(GetGravity().Y);
		float speed = denom > 0.0001f ? Mathf.Sqrt(gravity * dEff * dEff / denom) : MinKickSpeed;
		speed = Mathf.Clamp(speed, MinKickSpeed, MaxKickSpeed);

		return new Vector3(dir.X * speed * cosA, speed * Mathf.Sin(angle), dir.Z * speed * cosA);
	}
}
