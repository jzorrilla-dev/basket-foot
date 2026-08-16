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
	[Export] public float MoveTurnSpeedDeg = 1440.0f;
	[Export] public float MaxLegSwingDeg = 28.0f;
	[Export] public float StrideFrequency = 6.0f;
	[Export] public float LegSwingResponse = 8.0f;
	[Export] public Vector3 HoopPosition = new(0, 3.0f, -13.1f);
	[Export] public Vector3 SecondHoopPosition = new(0, 3.0f, 13.1f);
	[Export] public float KickAngleDegrees = 65.0f;
	[Export] public float MinKickSpeed = 5.0f;
	[Export] public float MaxKickSpeed = 15.5f;
	[Export] public float ChargeTime = 0.8f;
	[Export] public float MinChargeToShoot = 0.05f;
	[Export] public float ShotSpreadDeg = 5.0f;
	[Export] public float SpreadDistance = 12.0f;
	[Export] public float VolleyKickSpeed = 10.0f;
	[Export] public float KickGrabCooldown = 0.4f;
	[Export] public float BounceForwardSpeed = 2.0f;
	[Export] public float BounceUpSpeed = 3.5f;
	[Export] public float BounceGrabCooldown = 0.6f;
	[Export] public float VolleyRadius = 1.5f;
	[Export] public float VolleyMaxHeight = 2.0f;
	[Export] public Vector3 RespawnPosition = new(0, 1, 1);
	[Export] public float RespawnBelow = -3.0f;
	[Export] public bool IsAI = false;
	[Export] public float AIShootDistance = 7.0f;
	[Export] public float AIMinShootDistance = 3.0f;
	[Export] public float AICarryTimeBeforeShot = 1.0f;
	[Export] public float AIGrabStopRadius = 0.9f;

	private Ball _ball;
	private Node3D _visual;
	private Node3D _legLeft;
	private Node3D _legRight;
	private float _facingAngle;
	private float _kickCooldown;
	private float _stridePhase;
	private float _strideAmp;
	private float _aiCarryTime;
	private float _kickCharge;
	private bool _charging;
	private bool _aiWantsShoot;
	private bool _canVolley;

	public Vector3 FacingDirection => new(Mathf.Sin(_facingAngle), 0, -Mathf.Cos(_facingAngle));
	public float KickCharge => _kickCharge;

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
		_legLeft = GetNodeOrNull<Node3D>("Visual/LegPivotLeft");
		_legRight = GetNodeOrNull<Node3D>("Visual/LegPivotRight");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (Input.IsActionJustPressed("jump") && IsOnFloor() && !IsAI)
		{
			velocity.Y = JumpVelocity;
		}

		bool kickPressed = false;
		bool kickHeld = false;
		bool kickReleased = false;
		bool bouncePressed = false;
		Vector2 inputDir;
		if (IsAI)
		{
			inputDir = ComputeAIInput(out kickPressed, out kickHeld, out kickReleased, out bouncePressed);
		}
		else
		{
			inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
			bouncePressed = Input.IsActionJustPressed("bounce");
			kickPressed = Input.IsActionJustPressed("kick");
			kickHeld = Input.IsActionPressed("kick");
			kickReleased = Input.IsActionJustReleased("kick");
		}
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
		UpdateLegs(direction, (float)delta);

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

			if (bouncePressed)
			{
				Vector3 pop = FacingDirection * BounceForwardSpeed + Vector3.Up * BounceUpSpeed;
				_ball.Release(pop);
				_kickCooldown = BounceGrabCooldown;
				_canVolley = true;
				_charging = false;
				_kickCharge = 0.0f;
			}
			else if (kickPressed && !_charging)
			{
				// El jugador decide la potencia: mantén K para cargar y suéltala
				// para disparar. La dirección la marca su frente (Q/E).
				_charging = true;
				_kickCharge = 0.0f;
			}

			if (_charging)
			{
				if (kickHeld)
				{
					_kickCharge = Mathf.Min(1.0f, _kickCharge + (float)delta / ChargeTime);
				}
				if (kickReleased)
				{
					if (_kickCharge >= MinChargeToShoot)
					{
						FireKick(_kickCharge);
					}
					_charging = false;
					_kickCharge = 0.0f;
					_kickCooldown = KickGrabCooldown;
					_canVolley = false;
				}
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

		if (_canVolley && kickPressed)
		{
			Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
			toBall.Y = 0;
			if (toBall.Length() < VolleyRadius && _ball.GlobalPosition.Y < VolleyMaxHeight)
			{
				_ball.RecordContact(GlobalPosition);
				Vector3 volleyDir = ApplyShotSpread(FacingDirection, DistanceToHoop(_ball.GlobalPosition));
				FireKickWithSpeed(VolleyKickSpeed, volleyDir);
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
		float turn = IsAI ? 0.0f : Input.GetAxis("turn_left", "turn_right");
		if (turn != 0.0f)
		{
			// Giro manual con Q/E: el jugador decide hacia dónde apuntar.
			// Positivo = horario visto desde arriba (girar a la derecha);
			// se refiere a _facingAngle, no al Rotation.y del visual (ver abajo).
			_facingAngle = Mathf.Wrap(_facingAngle + turn * Mathf.DegToRad(MaxTurnSpeedDeg) * delta, -Mathf.Pi, Mathf.Pi);
		}
		else if (IsAI && _aiWantsShoot)
		{
			// La IA encara el aro cuando se dispone a tirar.
			Vector3 toHoop = TargetHoop(GlobalPosition) - GlobalPosition;
			toHoop.Y = 0;
			if (toHoop.LengthSquared() > 0.01f)
			{
				_facingAngle = Mathf.Atan2(toHoop.X, -toHoop.Z);
			}
		}
		else if (moveDir.LengthSquared() > 0.001f)
		{
			// Sin apuntado automático: al moverse se mira hacia donde se camina
			// (A/D son strafe, S retrocede); con Q/E el jugador apunta a mano.
			float target = Mathf.Atan2(moveDir.X, -moveDir.Z);
			float diff = Mathf.AngleDifference(_facingAngle, target);
			float maxStep = Mathf.DegToRad(MoveTurnSpeedDeg) * delta;
			_facingAngle = Mathf.Abs(diff) <= maxStep ? target : _facingAngle + Mathf.Sign(diff) * maxStep;
		}

		if (_visual != null)
		{
			// OJO: _facingAngle positivo = horario visto desde arriba, pero el
			// Rotation.y de Godot positivo es antihorario. Sin el signo menos el
			// maniquí gira espejado (en sentido opuesto al balón cargado).
			_visual.Rotation = new Vector3(0, -_facingAngle, 0);
		}
	}

	private void UpdateLegs(Vector3 moveDir, float delta)
	{
		if (_legLeft == null || _legRight == null)
		{
			return;
		}

		// Amplitud proporcional a la velocidad real: quieto = pie firme.
		float speedRatio = Mathf.Clamp(new Vector2(Velocity.X, Velocity.Z).Length() / Speed, 0.0f, 1.0f);
		_strideAmp = Mathf.MoveToward(_strideAmp, Mathf.DegToRad(MaxLegSwingDeg) * speedRatio, LegSwingResponse * delta);
		_stridePhase += StrideFrequency * speedRatio * delta;

		float swing = _strideAmp * Mathf.Sin(_stridePhase);

		// Yaw: orienta el plano de balanceo hacia la dirección real de carrera
		// (en el marco local del Visual), así el strafe y el backpedal se ven bien
		// aunque el cuerpo mire siempre al aro.
		float yaw = 0.0f;
		if (_visual != null && moveDir.LengthSquared() > 0.001f)
		{
			Vector3 localDir = _visual.GlobalBasis.Inverse() * moveDir;
			yaw = Mathf.Atan2(-localDir.X, -localDir.Z);
		}

		// Piernas con fases opuestas; la cuaternión aplica pitch (Rx) y luego yaw (Ry):
// el plano de balanceo queda alineado con la dirección real de carrera.
		Quaternion yawQuat = new Quaternion(Vector3.Up, yaw);
		_legLeft.Transform = new Transform3D(new Basis(yawQuat * new Quaternion(Vector3.Right, swing)), _legLeft.Transform.Origin);
		_legRight.Transform = new Transform3D(new Basis(yawQuat * new Quaternion(Vector3.Right, -swing)), _legRight.Transform.Origin);
	}

	private Vector2 ComputeAIInput(out bool kickPressed, out bool kickHeld, out bool kickReleased, out bool bounce)
	{
		kickPressed = false;
		kickHeld = false;
		kickReleased = false;
		bounce = false;
		_aiWantsShoot = false;
		if (_ball == null)
		{
			return Vector2.Zero;
		}

		if (_ball.Carrier == this)
		{
			_aiCarryTime += (float)GetPhysicsProcessDeltaTime();
			Vector3 toHoop = TargetHoop(GlobalPosition) - GlobalPosition;
			toHoop.Y = 0;
			float distance = toHoop.Length();
			if (distance > AIShootDistance)
			{
				return new Vector2(toHoop.X, toHoop.Z);
			}
			if (distance < AIMinShootDistance)
			{
				return new Vector2(-toHoop.X, -toHoop.Z);
			}
			if (_aiCarryTime < AICarryTimeBeforeShot)
			{
				return Vector2.Zero;
			}

			// En rango: encara al aro, carga la potencia justa y suelta la patada.
			_aiWantsShoot = true;
			kickPressed = !_charging;
			kickHeld = true;
			if (_kickCharge >= RequiredCharge(distance))
			{
				kickReleased = true;
			}
			return Vector2.Zero;
		}

		_aiCarryTime = 0.0f;
		Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
		toBall.Y = 0;
		if (toBall.Length() < AIGrabStopRadius)
		{
			return Vector2.Zero;
		}
		return new Vector2(toBall.X, toBall.Z);
	}

	// Potencia justa (0..1) para que el balón, con el ángulo fijo de tiro,
	// llegue a la distancia dada al aro.
	private float RequiredCharge(float distance)
	{
		float angle = Mathf.DegToRad(KickAngleDegrees);
		float cosA = Mathf.Cos(angle);
		float tanA = Mathf.Tan(angle);
		float gravity = Mathf.Abs(GetGravity().Y);
		float deltaHeight = TargetHoop(GlobalPosition).Y - _ball.GlobalPosition.Y;
		float dEff = Mathf.Max(distance, 1.5f);
		float denom = 2.0f * cosA * cosA * (dEff * tanA - deltaHeight);
		float speed = denom > 0.0001f ? Mathf.Sqrt(gravity * dEff * dEff / denom) : MinKickSpeed;
		speed = Mathf.Clamp(speed, MinKickSpeed, MaxKickSpeed);
		return (speed - MinKickSpeed) / (MaxKickSpeed - MinKickSpeed);
	}

	// Disparo con la potencia elegida por el jugador (mantener K = cargar).
	private void FireKick(float powerRatio)
	{
		float speed = Mathf.Lerp(MinKickSpeed, MaxKickSpeed, powerRatio);
		FireKickWithSpeed(speed, ApplyShotSpread(FacingDirection, DistanceToHoop(_ball.GlobalPosition)));
	}

	private void FireKickWithSpeed(float speed, Vector3 dir)
	{
		float angle = Mathf.DegToRad(KickAngleDegrees);
		float cosA = Mathf.Cos(angle);
		_ball.Release(new Vector3(dir.X * speed * cosA, speed * Mathf.Sin(angle), dir.Z * speed * cosA));
	}

	private float DistanceToHoop(Vector3 from)
	{
		Vector3 flat = TargetHoop(from) - from;
		return new Vector3(flat.X, 0, flat.Z).Length();
	}

	// Con dos canastas, el jugador ataca siempre la que le queda más cerca.
	private Vector3 TargetHoop(Vector3 from)
	{
		Vector3 toFirst = HoopPosition - from;
		toFirst.Y = 0;
		Vector3 toSecond = SecondHoopPosition - from;
		toSecond.Y = 0;
		return toFirst.LengthSquared() <= toSecond.LengthSquared() ? HoopPosition : SecondHoopPosition;
	}

	// Dispersión del tiro: parado y cerca del aro = precisión; corriendo (y a
	// mayor distancia) = error aleatorio. Premia asentarse antes de tirar.
	private Vector3 ApplyShotSpread(Vector3 dir, float distance)
	{
		float speedRatio = Mathf.Clamp(new Vector2(Velocity.X, Velocity.Z).Length() / Speed, 0.0f, 1.0f);
		float distRatio = Mathf.Clamp(distance / SpreadDistance, 0.0f, 1.0f);
		float spreadDeg = ShotSpreadDeg * speedRatio * (0.35f + 0.65f * distRatio);
		if (spreadDeg <= 0.01f)
		{
			return dir;
		}

		float err = Mathf.DegToRad(spreadDeg) * (GD.Randf() * 2.0f - 1.0f);
		return RotateHorizontal(dir, err);
	}

	private static Vector3 RotateHorizontal(Vector3 dir, float rad)
	{
		float c = Mathf.Cos(rad);
		float s = Mathf.Sin(rad);
		return new Vector3(dir.X * c + dir.Z * s, 0, -dir.X * s + dir.Z * c);
	}
}
