using Godot;

public partial class PlayerCamera : Camera3D
{
	[Export] public NodePath PlayerPath;
	[Export] public float DistanceBehind = 5.0f;
	[Export] public float Height = 3.2f;
	[Export] public float LookAhead = 4.0f;
	[Export] public float LookHeight = 1.2f;
	[Export] public float FollowResponse = 6.0f;
	[Export] public float LookResponse = 8.0f;

	private Player _player;
	private Vector3 _lookTarget;

	public override void _Ready()
	{
		if (PlayerPath != null)
		{
			_player = GetNode<Player>(PlayerPath);
		}
		if (_player != null)
		{
			_lookTarget = _player.GlobalPosition + _player.FacingDirection * LookAhead + Vector3.Up * LookHeight;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
		{
			return;
		}

		// La cámara se sitúa detrás del jugador, en la dirección opuesta a su
		// mirada (el frente del jugador apunta al aro más cercano).
		Vector3 facing = _player.FacingDirection;
		Vector3 idealPos = _player.GlobalPosition - facing * DistanceBehind + Vector3.Up * Height;
		GlobalPosition = GlobalPosition.Lerp(idealPos, 1.0f - Mathf.Exp(-FollowResponse * (float)delta));

		// Y mira hacia delante, hacia donde el jugador está tirando.
		Vector3 idealLook = _player.GlobalPosition + facing * LookAhead + Vector3.Up * LookHeight;
		_lookTarget = _lookTarget.Lerp(idealLook, 1.0f - Mathf.Exp(-LookResponse * (float)delta));
		LookAt(_lookTarget, Vector3.Up);
	}
}