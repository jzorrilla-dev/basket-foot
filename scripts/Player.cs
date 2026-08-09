using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 6.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public NodePath BallPath;
	[Export] public float GrabRadius = 1.0f;
	[Export] public float GrabMaxHeight = 1.2f;
	[Export] public Vector3 HoopPosition = new(0, 3.0f, -4.9f);
	[Export] public float KickAngleDegrees = 55.0f;
	[Export] public float MinKickSpeed = 5.0f;
	[Export] public float MaxKickSpeed = 18.0f;
	[Export] public float MinKickDistance = 3.0f;
	[Export] public float KickGrabCooldown = 0.4f;
	[Export] public Vector3 RespawnPosition = new(0, 1, 1);
	[Export] public float RespawnBelow = -3.0f;

	private Ball _ball;
	private float _kickCooldown;

	public override void _Ready()
	{
		if (BallPath != null)
		{
			_ball = GetNode<Ball>(BallPath);
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

			if (Input.IsActionJustPressed("kick"))
			{
				_ball.Release(ComputeKickVelocity());
				_kickCooldown = KickGrabCooldown;
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

		Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
		toBall.Y = 0;
		if (_kickCooldown <= 0 && toBall.Length() < GrabRadius && _ball.GlobalPosition.Y < GrabMaxHeight)
		{
			_ball.Grab(this);
		}
	}

	private Vector3 ComputeKickVelocity()
	{
		Vector3 dir = _ball.CarryDirection;

		Vector3 toHoop = HoopPosition - _ball.GlobalPosition;
		float distance = new Vector3(toHoop.X, 0, toHoop.Z).Length();
		float deltaHeight = HoopPosition.Y - _ball.GlobalPosition.Y;

		float angle = Mathf.DegToRad(KickAngleDegrees);
		float cosA = Mathf.Cos(angle);
		float tanA = Mathf.Tan(angle);

		float dEff = Mathf.Max(distance, MinKickDistance);
		float denom = 2.0f * cosA * cosA * (dEff * tanA - deltaHeight);
		float gravity = Mathf.Abs(GetGravity().Y);
		float speed = denom > 0.0001f ? Mathf.Sqrt(gravity * dEff * dEff / denom) : MinKickSpeed;
		speed = Mathf.Clamp(speed, MinKickSpeed, MaxKickSpeed);

		return new Vector3(dir.X * speed * cosA, speed * Mathf.Sin(angle), dir.Z * speed * cosA);
	}
}
