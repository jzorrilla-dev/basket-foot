using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
	public enum AIRole { Primary, Support }

	private static readonly List<Player> AllPlayers = new();

	[Export] public float WalkSpeed = 3.0f;
	[Export] public float JogSpeed = 6.0f;
	[Export] public float SprintSpeed = 9.0f;
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
	[Export] public Vector3 HoopPosition = new(0, 3.0f, -20.0f);
	[Export] public Vector3 SecondHoopPosition = new(0, 3.0f, 20.0f);
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
	[Export] public float HeaderMinHeight = 0.7f;
	[Export] public float HeaderMaxHeight = 1.4f;
	[Export] public float HeaderRadius = 1.3f;
	[Export] public float HeaderSpeed = 8.0f;
	[Export] public float HeaderAngleDegrees = 30.0f;
	[Export] public Vector3 RespawnPosition = new(0, 1, 1);
	[Export] public float RespawnBelow = -3.0f;
	[Export] public int TeamId = 0;
	[Export] public bool IsAI = false;
	[Export] public AIRole Role = AIRole.Primary;
	[Export] public float AIShootDistance = 7.0f;
	[Export] public float AIMinShootDistance = 3.0f;
	[Export] public float AICarryTimeBeforeShot = 1.0f;
	[Export] public float AIGrabStopRadius = 0.9f;
	[Export] public float AISpeedFactor = 0.72f;
	[Export] public float AIPostAvoidDuration = 0.5f;
	[Export] public float CourtHalfWidth = 10.0f;
	[Export] public float CourtHalfLength = 20.0f;
	[Export] public float StealRadius = 2.2f;
	[Export] public float StealForwardReach = 1.0f;
	[Export] public float StealCooldown = 0.7f;
	[Export] public float LostBallStealLockout = 1.1f;
	[Export] public float AIBallControlLockout = 3.0f;
	[Export] public float AIStealRadius = 1.25f;
	[Export] public float AIStealForwardReach = 0.65f;
	[Export] public float AIStealWindup = 0.28f;
	[Export] public float StealDuration = 0.32f;
	[Export] public float SlideStealRadius = 3.6f;
	[Export] public float SlideStealForwardReach = 2.2f;
	[Export] public float SlideStealDuration = 0.55f;
	[Export] public float SlideStealSpeed = 8.5f;
	[Export] public float CourtClampMargin = 0.35f;
	[Export] public float PassMinSpeed = 7.0f;
	[Export] public float PassMaxSpeed = 13.0f;
	[Export] public float PassLift = 0.75f;
	[Export] public float PassLeadTime = 0.25f;
	[Export] public float PassMaxDistance = 16.0f;
	[Export] public float PassReceiveDuration = 1.45f;
	[Export] public float PassInterceptRadius = 1.5f;
	[Export] public float AIPassConsiderTime = 0.6f;
	[Export] public float AIPassCooldown = 1.2f;
	[Export] public float AIPassDistanceAdvantage = 0.35f;
	[Export] public float AIPassLaneRiskLimit = 1.1f;
	[Export] public float AIPassCarryTime = 0.9f;
	[Export] public float TeamSpacingRadius = 2.6f;
	[Export] public float OpponentSpacingRadius = 1.9f;
	[Export] public float RivalAIShotSpreadDeg = 2.0f;

	private Ball _ball;
	private Node3D _visual;
	private Node3D _legLeft;
	private Node3D _legRight;
	private float _facingAngle;
	private float _kickCooldown;
	private float _stridePhase;
	private float _strideAmp;
	private float _aiCarryTime;
	private float _aiPassCooldownTimer;
	private float _kickCharge;
	private bool _charging;
	private bool _aiWantsShoot;
	private bool _canVolley;
	private float _currentSpeed;
	private float _postAvoidTimer;
	private float _stealCooldown;
	private float _ballControlLockout;
	private float _stealTimer;
	private bool _slidingSteal;
	private Vector3 _slideDirection;
	private Vector3 _postAvoidDirection;
	private float _receiveTimer;
	private Vector3 _receiveTarget;
	private float _aiStealWindupTimer;

	public Vector3 FacingDirection => new(Mathf.Sin(_facingAngle), 0, -Mathf.Cos(_facingAngle));
	public float KickCharge => _kickCharge;
	public static IReadOnlyList<Player> Players => AllPlayers;
	public static string TeamName(int teamId) => teamId == 0 ? "Equipo Azul" : "Equipo Rojo";

	public override void _Ready()
	{
		if (!AllPlayers.Contains(this))
		{
			AllPlayers.Add(this);
		}

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

	public override void _ExitTree()
	{
		AllPlayers.Remove(this);
	}

	public void PrepareCenterRestart(Vector3 position)
	{
		GlobalPosition = position;
		Velocity = Vector3.Zero;
		_charging = false;
		_kickCharge = 0.0f;
		_aiCarryTime = 0.0f;
		_canVolley = false;
		_kickCooldown = KickGrabCooldown;

		Vector3 toHoop = AttackingHoop() - position;
		toHoop.Y = 0.0f;
		if (toHoop.LengthSquared() > 0.001f)
		{
			_facingAngle = Mathf.Atan2(toHoop.X, -toHoop.Z);
		}

		_ball.ResumeAfterRestart();
		_ball.RecordContact(GlobalPosition);
		_ball.RecordTouchPlayer(this);
		_ball.Grab(this);

		if (_visual != null)
		{
			_visual.Rotation = new Vector3(0, -_facingAngle, 0);
		}
	}

	public void PrepareThrowInRestart(Vector3 position, Vector3 target)
	{
		PrepareSetPieceRestart(position, target);
		Vector3 releasePosition = GlobalPosition + Vector3.Up * 1.45f + FacingDirection * 0.25f;
		_ball.GlobalPosition = releasePosition;
		_ball.RecordContact(GlobalPosition);
		_ball.RecordTouchPlayer(this);
		_ball.Release(LaunchVelocity(releasePosition, target, 7.8f, 2.7f));
		_kickCooldown = KickGrabCooldown;
	}

	public void PrepareKickInRestart(Vector3 position, Vector3 target)
	{
		PrepareSetPieceRestart(position, target);
		Vector3 releasePosition = GlobalPosition + FacingDirection * 0.8f;
		releasePosition.Y = 0.28f;
		_ball.GlobalPosition = releasePosition;
		_ball.RecordContact(GlobalPosition);
		_ball.RecordTouchPlayer(this);
		_ball.Release(LaunchVelocity(releasePosition, target, 10.5f, 0.55f));
		_kickCooldown = KickGrabCooldown;
	}

	private void PrepareSetPieceRestart(Vector3 position, Vector3 target)
	{
		GlobalPosition = position;
		Velocity = Vector3.Zero;
		_charging = false;
		_kickCharge = 0.0f;
		_aiCarryTime = 0.0f;
		_canVolley = false;
		_receiveTimer = 0.0f;

		Vector3 toTarget = target - position;
		toTarget.Y = 0.0f;
		if (toTarget.LengthSquared() > 0.001f)
		{
			_facingAngle = Mathf.Atan2(toTarget.X, -toTarget.Z);
		}

		_ball.ResumeAfterRestart();

		if (_visual != null)
		{
			_visual.Rotation = new Vector3(0, -_facingAngle, 0);
		}
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
		bool passPressed = false;
		bool stealPressed = false;
		bool slideStealPressed = false;
		Vector3 direction;
		if (IsAI)
		{
			if (_postAvoidTimer > 0.0f)
			{
				// Esquivando el poste: usar dirección de evasión.
				direction = _postAvoidDirection;
				_currentSpeed = JogSpeed * AISpeedFactor;
			}
			else
			{
				Vector2 aiInput = ComputeAIInput(out kickPressed, out kickHeld, out kickReleased, out bouncePressed, out passPressed);
				direction = new Vector3(aiInput.X, 0, aiInput.Y).Normalized();
				_currentSpeed = direction.LengthSquared() > 0.001f ? JogSpeed * AISpeedFactor : 0.0f;
			}
		}
		else
		{
			// S gira 180° en vez de mover hacia atrás.
			if (Input.IsActionJustPressed("move_back"))
			{
				_facingAngle = Mathf.Wrap(_facingAngle + Mathf.Pi, -Mathf.Pi, Mathf.Pi);
			}

			// W avanza, A/D strafe; sin retroceso.
			float strafe = Input.GetAxis("move_left", "move_right");
			bool moveForward = Input.IsActionPressed("move_forward");
			Vector2 inputDir = new Vector2(strafe, moveForward ? -1.0f : 0.0f);

			bouncePressed = Input.IsActionJustPressed("bounce");
			passPressed = Input.IsActionJustPressed("pass");
			stealPressed = Input.IsActionJustPressed("steal");
			slideStealPressed = stealPressed && Input.IsKeyPressed(Key.Shift);
			kickPressed = Input.IsActionJustPressed("kick");
			kickHeld = Input.IsActionPressed("kick");
			kickReleased = Input.IsActionJustReleased("kick");

			// Velocidad según modificadores: Ctrl=caminar, default=trote, Shift=correr.
			if (Input.IsKeyPressed(Key.Ctrl))
				_currentSpeed = WalkSpeed;
			else if (Input.IsKeyPressed(Key.Shift))
				_currentSpeed = SprintSpeed;
			else
				_currentSpeed = JogSpeed;

			// Movimiento relativo al facing (la cámara sigue el FacingDirection).
			Vector3 forward = FacingDirection;
			Vector3 right = new(Mathf.Cos(_facingAngle), 0, Mathf.Sin(_facingAngle));
			direction = (forward * -inputDir.Y + right * inputDir.X).Normalized();
		}

		if (_slidingSteal)
		{
			direction = _slideDirection;
			velocity.X = _slideDirection.X * SlideStealSpeed;
			velocity.Z = _slideDirection.Z * SlideStealSpeed;
		}
		else if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * _currentSpeed;
			velocity.Z = direction.Z * _currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, _currentSpeed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, _currentSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
		ClampInsideCourt();

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
		if (_aiPassCooldownTimer > 0.0f)
		{
			_aiPassCooldownTimer -= (float)delta;
		}
		if (_stealCooldown > 0)
		{
			_stealCooldown -= (float)delta;
		}
		if (!IsAI || _ball.Carrier == this || _ball.Carrier == null)
		{
			_aiStealWindupTimer = 0.0f;
		}
		if (_ballControlLockout > 0)
		{
			_ballControlLockout -= (float)delta;
		}
		if (_receiveTimer > 0)
		{
			_receiveTimer -= (float)delta;
		}
		if (_stealTimer > 0)
		{
			_stealTimer -= (float)delta;
			if (_stealTimer <= 0.0f)
			{
				_slidingSteal = false;
				if (_visual != null)
				{
					_visual.Position = Vector3.Zero;
				}
			}
		}

		if (_ball.IsCarried)
		{
			if (_ball.Carrier != this)
			{
				_charging = false;
				_kickCharge = 0.0f;
				_canVolley = false;
				StartSteal(stealPressed, slideStealPressed, direction);
				TrySteal();
				return;
			}

			_ball.RecordContact(GlobalPosition);
			_ball.RecordTouchPlayer(this);

			if (passPressed && TryPass())
			{
				_kickCooldown = KickGrabCooldown;
				_canVolley = false;
				_charging = false;
				_kickCharge = 0.0f;
			}
			else if (bouncePressed)
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

		StartSteal(stealPressed, slideStealPressed, direction);

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is Ball ball)
			{
				ball.RecordContact(GlobalPosition);
				ball.RecordTouchPlayer(this);
			}

			// IA evita el poste de la canasta: detectar colisión y esquivar.
			if (IsAI && _postAvoidTimer <= 0.0f)
			{
				var collider = GetSlideCollision(i).GetCollider();
				if (collider is StaticBody3D sb && ((string)sb.Name == "Basket" || (string)sb.Name == "Basket2"))
				{
					// Dirección perpendicular aleatoria (izq/der) relativa al poste.
					Vector3 toPost = sb.GlobalPosition - GlobalPosition;
					toPost.Y = 0;
					Vector3 perp = toPost.Cross(Vector3.Up).Normalized();
					if (perp.LengthSquared() < 0.001f)
						perp = Vector3.Right;
					_postAvoidDirection = GD.Randf() > 0.5f ? perp : -perp;
					_postAvoidTimer = AIPostAvoidDuration;
				}
			}
		}

		// Timer de evasión de poste.
		if (_postAvoidTimer > 0.0f)
		{
			_postAvoidTimer -= (float)delta;
		}

		// Cabeceo: balón suelto a altura de cabeza, presionar K.
		if (kickPressed && _kickCooldown <= 0.0f && _ball != null)
		{
			Vector3 toBallH = _ball.GlobalPosition - GlobalPosition;
			float ballY = _ball.GlobalPosition.Y;
			float horizontalDist = new Vector2(toBallH.X, toBallH.Z).Length();
			if (horizontalDist < HeaderRadius && ballY > HeaderMinHeight && ballY < HeaderMaxHeight)
			{
				_ball.RecordContact(GlobalPosition);
				_ball.RecordTouchPlayer(this);
				Vector3 headerDir = ApplyShotSpread(FacingDirection, DistanceToHoop(_ball.GlobalPosition));
				FireKickWithSpeed(HeaderSpeed, headerDir, HeaderAngleDegrees);
				_kickCooldown = KickGrabCooldown;
				kickPressed = false;
			}
		}

		if (_canVolley && kickPressed)
		{
			Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
			toBall.Y = 0;
			if (toBall.Length() < VolleyRadius && _ball.GlobalPosition.Y < VolleyMaxHeight)
			{
				_ball.RecordContact(GlobalPosition);
				_ball.RecordTouchPlayer(this);
				Vector3 volleyDir = ApplyShotSpread(FacingDirection, DistanceToHoop(_ball.GlobalPosition));
				FireKickWithSpeed(VolleyKickSpeed, volleyDir);
				_kickCooldown = KickGrabCooldown;
				_canVolley = false;
			}
		}

		TrySteal();

		Vector3 grabToBall = _ball.GlobalPosition - GlobalPosition;
		grabToBall.Y = 0;
		if (_kickCooldown <= 0 && _ballControlLockout <= 0.0f && grabToBall.Length() < GrabRadius && _ball.GlobalPosition.Y < GrabMaxHeight)
		{
			_ball.RecordTouchPlayer(this);
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
			Vector3 from = _ball != null ? _ball.GlobalPosition : GlobalPosition;
			Vector3 toHoop = AttackingHoop() - from;
			toHoop.Y = 0;
			if (toHoop.LengthSquared() > 0.01f)
			{
				_facingAngle = Mathf.Atan2(toHoop.X, -toHoop.Z);
			}
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

		if (_stealTimer > 0.0f)
		{
			float totalDuration = _slidingSteal ? SlideStealDuration : StealDuration;
			float progress = 1.0f - Mathf.Clamp(_stealTimer / totalDuration, 0.0f, 1.0f);
			float extension = Mathf.Sin(progress * Mathf.Pi);
			float kickPitch = Mathf.Lerp(Mathf.DegToRad(-12.0f), Mathf.DegToRad(-78.0f), extension);
			float bracePitch = Mathf.Lerp(Mathf.DegToRad(6.0f), Mathf.DegToRad(34.0f), extension);
			if (_slidingSteal)
			{
				kickPitch = Mathf.Lerp(Mathf.DegToRad(-28.0f), Mathf.DegToRad(-102.0f), extension);
				bracePitch = Mathf.Lerp(Mathf.DegToRad(18.0f), Mathf.DegToRad(72.0f), extension);
				if (_visual != null)
				{
					_visual.Position = new Vector3(0.0f, -0.28f * extension, 0.0f);
				}
			}

			float stealYaw = 0.0f;
			Vector3 attackDir = _slidingSteal ? _slideDirection : FacingDirection;
			if (_visual != null && attackDir.LengthSquared() > 0.001f)
			{
				Vector3 localDir = _visual.GlobalBasis.Inverse() * attackDir;
				stealYaw = Mathf.Atan2(-localDir.X, -localDir.Z);
			}

			Quaternion stealYawQuat = new Quaternion(Vector3.Up, stealYaw);
			_legRight.Transform = new Transform3D(new Basis(stealYawQuat * new Quaternion(Vector3.Right, kickPitch)), _legRight.Transform.Origin);
			_legLeft.Transform = new Transform3D(new Basis(stealYawQuat * new Quaternion(Vector3.Right, bracePitch)), _legLeft.Transform.Origin);
			return;
		}

		if (_visual != null)
		{
			_visual.Position = Vector3.Zero;
		}

		// Amplitud proporcional a la velocidad real: quieto = pie firme.
		float speedRatio = Mathf.Clamp(new Vector2(Velocity.X, Velocity.Z).Length() / SprintSpeed, 0.0f, 1.0f);
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

	private Vector2 ComputeAIInput(out bool kickPressed, out bool kickHeld, out bool kickReleased, out bool bounce, out bool pass)
	{
		kickPressed = false;
		kickHeld = false;
		kickReleased = false;
		bounce = false;
		pass = false;
		_aiWantsShoot = false;
		if (_ball == null)
		{
			return Vector2.Zero;
		}

		if (_ball.Carrier == this)
		{
			_aiCarryTime += (float)GetPhysicsProcessDeltaTime();
			Vector3 toHoop = AttackingHoop() - GlobalPosition;
			toHoop.Y = 0;
			float distance = DistanceToHoop(_ball.GlobalPosition);
			if (_aiCarryTime >= AIPassConsiderTime
				&& _aiPassCooldownTimer <= 0.0f
				&& ShouldAIPass(distance))
			{
				pass = true;
				return Vector2.Zero;
			}
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
		if (_receiveTimer > 0.0f)
		{
			Vector3 toReceive = _receiveTarget - GlobalPosition;
			toReceive.Y = 0.0f;
			if (toReceive.Length() >= AIGrabStopRadius)
			{
				Vector3 receiveInput = KeepInsideCourt(toReceive + SeparationVector() * 0.7f);
				return new Vector2(receiveInput.X, receiveInput.Z);
			}
		}

		Vector3 target = ChooseAITarget();
		Vector3 toTarget = target - GlobalPosition;
		toTarget.Y = 0;
		if (toTarget.Length() < AIGrabStopRadius)
		{
			return Vector2.Zero;
		}
		Vector3 input = KeepInsideCourt(toTarget + SeparationVector() * 2.2f);
		return new Vector2(input.X, input.Z);
	}

	private Vector3 ChooseAITarget()
	{
		if (_ball.Carrier is Player carrier)
		{
			if (carrier.TeamId == TeamId)
			{
				return SupportAttackPosition(carrier);
			}

			return Role == AIRole.Primary ? carrier.GlobalPosition : SupportDefensePosition(carrier);
		}

		Player closestTeamMate = ClosestPlayerToBall(TeamId);
		if (closestTeamMate == this)
		{
			return _ball.GlobalPosition;
		}

		return Role == AIRole.Primary ? SupportDefensePosition(null) : SupportAttackPosition(null);
	}

	private Vector3 SupportAttackPosition(Player carrier)
	{
		Vector3 hoop = AttackingHoop();
		Vector3 baseFrom = carrier != null ? carrier.GlobalPosition : _ball.GlobalPosition;
		Vector3 towardHoop = hoop - baseFrom;
		towardHoop.Y = 0.0f;
		if (towardHoop.LengthSquared() < 0.01f)
			towardHoop = TeamId == 0 ? Vector3.Back : Vector3.Forward;
		towardHoop = towardHoop.Normalized();
		Vector3 lateral = new(-towardHoop.Z, 0.0f, towardHoop.X);
		float side = Role == AIRole.Support ? 1.0f : -1.0f;
		// El apoyo se ofrece por delante y abierto, creando una línea de pase
		// progresiva en lugar de quedarse detrás del jugador con balón.
		Vector3 target = baseFrom + towardHoop * 4.5f + lateral * side * 4.5f;
		target.X = Mathf.Clamp(target.X, -CourtHalfWidth + 1.0f, CourtHalfWidth - 1.0f);
		target.Z = Mathf.Clamp(target.Z, -CourtHalfLength + 1.0f, CourtHalfLength - 1.0f);
		target.Y = GlobalPosition.Y;
		return target;
	}

	private Vector3 SupportDefensePosition(Player carrier)
	{
		Vector3 ownHoop = DefendingHoop();
		Vector3 threat = carrier != null ? carrier.GlobalPosition : _ball.GlobalPosition;
		Vector3 target = ownHoop.Lerp(threat, Role == AIRole.Support ? 0.28f : 0.48f);
		target.X += Role == AIRole.Support ? 4.0f : -3.2f;
		target.X = Mathf.Clamp(target.X, -CourtHalfWidth + 1.0f, CourtHalfWidth - 1.0f);
		target.Z = Mathf.Clamp(target.Z, -CourtHalfLength + 1.0f, CourtHalfLength - 1.0f);
		target.Y = GlobalPosition.Y;
		return target;
	}

	private Vector3 SeparationVector()
	{
		Vector3 push = Vector3.Zero;
		foreach (Player other in AllPlayers)
		{
			if (other == this || !IsInstanceValid(other))
				continue;

			Vector3 away = GlobalPosition - other.GlobalPosition;
			away.Y = 0.0f;
			float distance = away.Length();
			float desiredSpacing = other.TeamId == TeamId ? TeamSpacingRadius : OpponentSpacingRadius;
			if (distance > 0.01f && distance < desiredSpacing)
			{
				push += away.Normalized() * (desiredSpacing - distance);
			}
		}
		return push;
	}

	private static Player ClosestPlayerToBall(int teamId)
	{
		Player closest = null;
		float bestDistanceSq = float.PositiveInfinity;
		foreach (Player player in AllPlayers)
		{
			if (!IsInstanceValid(player) || player.TeamId != teamId || player._ball == null)
				continue;

			float distanceSq = player.GlobalPosition.DistanceSquaredTo(player._ball.GlobalPosition);
			if (distanceSq < bestDistanceSq)
			{
				bestDistanceSq = distanceSq;
				closest = player;
			}
		}
		return closest;
	}

	private void StartSteal(bool stealPressed, bool slideStealPressed, Vector3 moveDirection)
	{
		if (!stealPressed || _stealCooldown > 0.0f || _stealTimer > 0.0f)
			return;

		if (IsAI)
			return;

		float speed = new Vector2(Velocity.X, Velocity.Z).Length();
		_slidingSteal = slideStealPressed || speed > JogSpeed + 0.5f;
		_stealTimer = _slidingSteal ? SlideStealDuration : StealDuration;
		_stealCooldown = _slidingSteal ? StealCooldown + 0.35f : StealCooldown;
		_slideDirection = moveDirection.LengthSquared() > 0.001f ? moveDirection.Normalized() : FacingDirection;

		// El humano tiene prioridad al iniciar un robo: evita que la IA recupere
		// la pelota en el mismo instante y permite comprobar el robo sin disputa.
		if (_ball.Carrier is Player aiCarrier && aiCarrier.IsAI)
		{
			Vector3 toCarrier = aiCarrier.GlobalPosition - GlobalPosition;
			toCarrier.Y = 0.0f;
			float challengeRadius = _slidingSteal ? SlideStealRadius : StealRadius;
			if (toCarrier.Length() <= challengeRadius)
			{
				aiCarrier.DisableBallControl();
				_ball.RecordContact(GlobalPosition);
				_ball.RecordTouchPlayer(this);
				_ball.Grab(this);
				_stealTimer = 0.0f;
				_slidingSteal = false;
			}
		}
	}

	private void TrySteal()
	{
		if (_ballControlLockout > 0.0f || _ball.Carrier == this)
			return;
		if (IsAI && (_stealCooldown > 0.0f || _ball.Carrier == null))
			return;
		if (!IsAI && _stealTimer <= 0.0f)
			return;

		Vector3 toBall = _ball.GlobalPosition - GlobalPosition;
		toBall.Y = 0.0f;
		Vector3 toCarrier = _ball.Carrier != null
			? _ball.Carrier.GlobalPosition - GlobalPosition
			: toBall;
		toCarrier.Y = 0.0f;
		float radius = IsAI ? AIStealRadius : (_slidingSteal ? SlideStealRadius : StealRadius);
		float forwardReach = IsAI ? AIStealForwardReach : (_slidingSteal ? SlideStealForwardReach : StealForwardReach);

		Vector3 stealDir = _slidingSteal ? _slideDirection : FacingDirection;
		if (IsAI)
		{
			stealDir = toCarrier.LengthSquared() > 0.001f ? toCarrier.Normalized() : FacingDirection;
		}
		float closestDistance = Mathf.Min(toCarrier.Length(), toBall.Length());
		if (closestDistance > radius)
		{
			if (IsAI)
				_aiStealWindupTimer = 0.0f;
			return;
		}

		if (IsAI)
		{
			// La IA sigue usando un alcance direccional para que no robe desde
			// cualquier ángulo mientras persigue al rival.
			Vector3 footReach = GlobalPosition + stealDir * forwardReach;
			Vector3 carrierPos = _ball.Carrier.GlobalPosition;
			Vector3 ballPos = _ball.GlobalPosition;
			footReach.Y = carrierPos.Y = ballPos.Y = 0.0f;
			if (footReach.DistanceTo(carrierPos) > AIStealRadius && footReach.DistanceTo(ballPos) > AIStealRadius)
			{
				_aiStealWindupTimer = 0.0f;
				return;
			}

			_aiStealWindupTimer += (float)GetPhysicsProcessDeltaTime();
			if (_aiStealWindupTimer < AIStealWindup)
				return;
		}

		Player previousCarrier = _ball.Carrier as Player;
		_ball.RecordContact(GlobalPosition);
		_ball.RecordTouchPlayer(this);
		_ball.Grab(this, IsAI);
		_stealCooldown = StealCooldown;
		_aiStealWindupTimer = 0.0f;
		if (previousCarrier != null)
		{
			previousCarrier.LockStealingAfterLoss();
		}
		_kickCooldown = 0.0f;
		_aiCarryTime = 0.0f;
		_slidingSteal = false;
		_stealTimer = 0.0f;
	}

	private void LockStealingAfterLoss()
	{
		_stealCooldown = Mathf.Max(_stealCooldown, LostBallStealLockout);
		_stealTimer = 0.0f;
		_slidingSteal = false;
	}

	private void DisableBallControl()
	{
		_ballControlLockout = Mathf.Max(_ballControlLockout, AIBallControlLockout);
		LockStealingAfterLoss();
	}

	private bool TryPass()
	{
		Player receiver = FindBestPassReceiver();
		if (receiver == null)
			return false;

		Vector3 target = PredictPassTarget(receiver);
		Vector3 flat = target - _ball.GlobalPosition;
		flat.Y = 0.0f;
		float distance = flat.Length();
		if (distance < 0.5f || distance > PassMaxDistance)
			return false;

		Vector3 dir = flat.Normalized();
		float speedRatio = Mathf.Clamp(distance / PassMaxDistance, 0.0f, 1.0f);
		float speed = Mathf.Lerp(PassMinSpeed, PassMaxSpeed, speedRatio);
		Vector3 velocity = dir * speed + Vector3.Up * PassLift;

		_ball.RecordContact(GlobalPosition);
		_ball.RecordTouchPlayer(this);
		_ball.Release(velocity);
		receiver.StartReceivingPass(target);
		_aiPassCooldownTimer = AIPassCooldown;
		return true;
	}

	private Player FindBestPassReceiver()
	{
		Player best = null;
		float bestScore = float.NegativeInfinity;
		Vector3 hoop = AttackingHoop();
		float myHoopDistance = FlatDistance(GlobalPosition, hoop);

		foreach (Player player in AllPlayers)
		{
			if (player == this || !IsInstanceValid(player) || player.TeamId != TeamId)
				continue;

			Vector3 target = PredictPassTarget(player);
			float distance = FlatDistance(GlobalPosition, target);
			if (distance > PassMaxDistance)
				continue;

			float receiverHoopDistance = FlatDistance(player.GlobalPosition, hoop);
			float progress = myHoopDistance - receiverHoopDistance;
			float lanePenalty = PassLaneRisk(target) * 4.0f;
			float score = progress - distance * 0.15f - lanePenalty;
			if (!IsAI)
			{
				Vector3 toReceiver = player.GlobalPosition - GlobalPosition;
				toReceiver.Y = 0.0f;
				if (toReceiver.LengthSquared() > 0.001f)
				{
					score += FacingDirection.Dot(toReceiver.Normalized()) * 2.0f;
				}
			}

			if (score > bestScore)
			{
				bestScore = score;
				best = player;
			}
		}

		return best;
	}

	private bool ShouldAIPass(float distanceToHoop)
	{
		if (distanceToHoop <= AIShootDistance && distanceToHoop >= AIMinShootDistance)
			return false;

		Player receiver = FindBestPassReceiver();
		if (receiver == null)
			return false;

		Vector3 hoop = AttackingHoop();
		float myHoopDistance = FlatDistance(GlobalPosition, hoop);
		float receiverHoopDistance = FlatDistance(receiver.GlobalPosition, hoop);
		bool receiverImprovesAttack = receiverHoopDistance + AIPassDistanceAdvantage < myHoopDistance;
		bool receiverIsOpen = PassLaneRisk(PredictPassTarget(receiver)) < AIPassLaneRiskLimit;
		bool hasHeldLongEnough = _aiCarryTime >= AIPassCarryTime;
		return hasHeldLongEnough && receiverImprovesAttack && receiverIsOpen;
	}

	private Vector3 PredictPassTarget(Player receiver)
	{
		Vector3 receiverVelocity = receiver.Velocity;
		receiverVelocity.Y = 0.0f;
		Vector3 target = receiver.GlobalPosition + receiverVelocity * PassLeadTime;
		target.X = Mathf.Clamp(target.X, -CourtHalfWidth + CourtClampMargin, CourtHalfWidth - CourtClampMargin);
		target.Z = Mathf.Clamp(target.Z, -CourtHalfLength + CourtClampMargin, CourtHalfLength - CourtClampMargin);
		target.Y = receiver.GlobalPosition.Y;
		return target;
	}

	private void StartReceivingPass(Vector3 target)
	{
		_receiveTarget = target;
		_receiveTimer = PassReceiveDuration;
		_ballControlLockout = 0.0f;
	}

	private float PassLaneRisk(Vector3 target)
	{
		Vector3 start = GlobalPosition;
		start.Y = 0.0f;
		Vector3 end = target;
		end.Y = 0.0f;
		Vector3 lane = end - start;
		float laneLengthSq = lane.LengthSquared();
		if (laneLengthSq < 0.001f)
			return 0.0f;

		float risk = 0.0f;
		foreach (Player player in AllPlayers)
		{
			if (player == this || !IsInstanceValid(player) || player.TeamId == TeamId)
				continue;

			Vector3 point = player.GlobalPosition;
			point.Y = 0.0f;
			float t = Mathf.Clamp((point - start).Dot(lane) / laneLengthSq, 0.0f, 1.0f);
			Vector3 closest = start + lane * t;
			float distance = point.DistanceTo(closest);
			if (distance < PassInterceptRadius)
			{
				risk += (PassInterceptRadius - distance) / PassInterceptRadius;
			}
		}
		return risk;
	}

	private static float FlatDistance(Vector3 a, Vector3 b)
	{
		return new Vector2(a.X - b.X, a.Z - b.Z).Length();
	}

	private void ClampInsideCourt()
	{
		Vector3 before = GlobalPosition;
		float x = Mathf.Clamp(GlobalPosition.X, -CourtHalfWidth + CourtClampMargin, CourtHalfWidth - CourtClampMargin);
		float z = Mathf.Clamp(GlobalPosition.Z, -CourtHalfLength + CourtClampMargin, CourtHalfLength - CourtClampMargin);
		bool clampedX = !Mathf.IsEqualApprox(x, before.X);
		bool clampedZ = !Mathf.IsEqualApprox(z, before.Z);
		if (!clampedX && !clampedZ)
			return;

		GlobalPosition = new Vector3(x, before.Y, z);
		Velocity = new Vector3(
			clampedX ? 0.0f : Velocity.X,
			Velocity.Y,
			clampedZ ? 0.0f : Velocity.Z
		);
	}

	private Vector3 KeepInsideCourt(Vector3 desired)
	{
		Vector3 input = desired;
		const float margin = 0.8f;

		if (GlobalPosition.X < -CourtHalfWidth + margin && input.X < 0.0f)
			input.X = 0.0f;
		else if (GlobalPosition.X > CourtHalfWidth - margin && input.X > 0.0f)
			input.X = 0.0f;

		if (GlobalPosition.Z < -CourtHalfLength + margin && input.Z < 0.0f)
			input.Z = 0.0f;
		else if (GlobalPosition.Z > CourtHalfLength - margin && input.Z > 0.0f)
			input.Z = 0.0f;

		if (input.LengthSquared() <= 0.001f)
			input = new Vector3(-GlobalPosition.X, 0.0f, -GlobalPosition.Z);

		return input;
	}

	// Potencia justa (0..1) para que el balón, con el ángulo fijo de tiro,
	// llegue a la distancia dada al aro.
	private float RequiredCharge(float distance)
	{
		float angle = Mathf.DegToRad(KickAngleDegrees);
		float cosA = Mathf.Cos(angle);
		float tanA = Mathf.Tan(angle);
		float gravity = Mathf.Abs(GetGravity().Y);
		float deltaHeight = AttackingHoop().Y - _ball.GlobalPosition.Y;
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
		Vector3 dir = IsAI ? ApplyAIShotSpread(AimDirectionToHoop()) : ApplyShotSpread(FacingDirection, DistanceToHoop(_ball.GlobalPosition));
		FireKickWithSpeed(speed, dir);
	}

	private void FireKickWithSpeed(float speed, Vector3 dir)
	{
		FireKickWithSpeed(speed, dir, KickAngleDegrees);
	}

	private void FireKickWithSpeed(float speed, Vector3 dir, float angleDeg)
	{
		float angle = Mathf.DegToRad(angleDeg);
		float cosA = Mathf.Cos(angle);
		_ball.Release(new Vector3(dir.X * speed * cosA, speed * Mathf.Sin(angle), dir.Z * speed * cosA));
	}

	private static Vector3 LaunchVelocity(Vector3 from, Vector3 target, float horizontalSpeed, float upSpeed)
	{
		Vector3 flat = target - from;
		flat.Y = 0.0f;
		Vector3 dir = flat.LengthSquared() > 0.001f ? flat.Normalized() : Vector3.Forward;
		return dir * horizontalSpeed + Vector3.Up * upSpeed;
	}

	private float DistanceToHoop(Vector3 from)
	{
		Vector3 flat = AttackingHoop() - from;
		return new Vector3(flat.X, 0, flat.Z).Length();
	}

	private Vector3 AimDirectionToHoop()
	{
		Vector3 toHoop = AttackingHoop() - _ball.GlobalPosition;
		toHoop.Y = 0.0f;
		return toHoop.LengthSquared() > 0.001f ? toHoop.Normalized() : FacingDirection;
	}

	private Vector3 ApplyAIShotSpread(Vector3 dir)
	{
		if (TeamId != 1 || RivalAIShotSpreadDeg <= 0.01f)
		{
			return dir;
		}

		float err = Mathf.DegToRad(RivalAIShotSpreadDeg) * (GD.Randf() * 2.0f - 1.0f);
		return RotateHorizontal(dir, err);
	}

	// Azul defiende el norte y ataca el sur; Rojo defiende el sur y ataca el
	// norte. Estas referencias son fijas durante todo el partido.
	private Vector3 AttackingHoop()
	{
		return TeamId == 0 ? HoopPosition : SecondHoopPosition;
	}

	private Vector3 DefendingHoop()
	{
		return TeamId == 0 ? SecondHoopPosition : HoopPosition;
	}

	// Dispersión del tiro: parado y cerca del aro = precisión; corriendo (y a
	// mayor distancia) = error aleatorio. Premia asentarse antes de tirar.
	private Vector3 ApplyShotSpread(Vector3 dir, float distance)
	{
		float speedRatio = Mathf.Clamp(new Vector2(Velocity.X, Velocity.Z).Length() / SprintSpeed, 0.0f, 1.0f);
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
