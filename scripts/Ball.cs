using Godot;

public partial class Ball : RigidBody3D
{
	[Export] public Vector3 ResetPosition = new(0, 0.5f, 3);
	[Export] public float ResetBelow = -3.0f;
	[Export] public float CarryForward = 0.7f;
	[Export] public float CarryDrop = 0.75f;

	public Vector3 LastContactPosition { get; private set; }
	public bool HasContact { get; private set; }
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
		}
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

	public void RecordContact(Vector3 position)
	{
		LastContactPosition = position;
		HasContact = true;
	}

	private void FollowCarrier()
	{
		Vector3 horizontalVelocity = Vector3.Zero;
		if (Carrier is CharacterBody3D body)
		{
			horizontalVelocity = new Vector3(body.Velocity.X, 0, body.Velocity.Z);
		}

		if (horizontalVelocity.Length() > 0.5f)
		{
			_carryDirection = horizontalVelocity.Normalized();
		}

		Vector3 target = Carrier.GlobalPosition + _carryDirection * CarryForward;
		target.Y = Carrier.GlobalPosition.Y - CarryDrop;
		GlobalPosition = target;
	}
}
