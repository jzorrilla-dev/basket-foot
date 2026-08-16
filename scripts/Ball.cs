using Godot;

public partial class Ball : RigidBody3D
{
	[Export] public Vector3 ResetPosition = new(0, 0.5f, 0);
	[Export] public float ResetBelow = -3.0f;
	[Export] public float CarryForward = 0.7f;
	[Export] public float CarryDrop = 0.75f;

	public Vector3 LastContactPosition { get; private set; }
	public bool HasContact { get; private set; }
	public bool ScoredFlag { get; private set; }
	public Node3D Carrier { get; private set; }
	public bool IsCarried => Carrier != null;
	public Vector3 CarryDirection => _carryDirection;

	private CollisionShape3D _collision;
	private Vector3 _carryDirection = Vector3.Forward;

	public override void _Ready()
	{
		_collision = GetNode<CollisionShape3D>("BallCollision");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsCarried)
		{
			FollowCarrier();
			return;
		}

		if (GlobalPosition.Y < ResetBelow)
		{
			LinearVelocity = Vector3.Zero;
			AngularVelocity = Vector3.Zero;
			GlobalPosition = ResetPosition;
			HasContact = false;
			ScoredFlag = false;
		}
	}

	public void MarkScored()
	{
		ScoredFlag = true;
	}

	public void RecordContact(Vector3 position)
	{
		LastContactPosition = position;
		HasContact = true;
		ScoredFlag = false;
	}

	public void Grab(Node3D carrier)
	{
		Carrier = carrier;
		Freeze = true;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		_collision.Disabled = true;
	}

	public void Release(Vector3 velocity)
	{
		Carrier = null;
		Freeze = false;
		_collision.Disabled = false;
		LinearVelocity = velocity;
	}

	private void FollowCarrier()
	{
		if (Carrier is Player player)
		{
			_carryDirection = player.FacingDirection;
		}
		else if (Carrier is CharacterBody3D body)
		{
			Vector3 horizontalVelocity = new(body.Velocity.X, 0, body.Velocity.Z);
			if (horizontalVelocity.Length() > 0.5f)
			{
				_carryDirection = horizontalVelocity.Normalized();
			}
		}

		Vector3 target = Carrier.GlobalPosition + _carryDirection * CarryForward;
		target.Y = Carrier.GlobalPosition.Y - CarryDrop;
		if (Carrier is Player chargingPlayer)
		{
			// Al cargar el tiro el balón se levanta y avanza un poco: el jugador
			// ve cuánta potencia lleva antes de soltar.
			float charge = chargingPlayer.KickCharge;
			target += _carryDirection * (charge * 0.5f);
			target.Y += charge * 0.35f;
		}
		GlobalPosition = target;
	}
}
