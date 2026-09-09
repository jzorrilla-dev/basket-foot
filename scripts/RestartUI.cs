using Godot;

public partial class RestartUI : CanvasLayer
{
	[Export] public float DisplayDuration = 2.5f;
	[Export] public float FadeOutDuration = 0.5f;

	private Label _label;
	private float _timer;
	private bool _fading;

	public override void _Ready()
	{
		_label = GetNode<Label>("RestartLabel");
		_label.Visible = false;

		var boundaryDetector = GetNode<BoundaryDetector>("../BoundaryDetector");
		boundaryDetector.RestartTriggered += OnRestartTriggered;
	}

	private void OnRestartTriggered(string restartType, string playerName)
	{
		_label.Text = $"{restartType} — {playerName}";
		_label.Visible = true;
		_label.Modulate = new Color(1, 1, 1, 1);
		_timer = DisplayDuration;
		_fading = false;
	}

	public override void _Process(double delta)
	{
		if (!_label.Visible)
			return;

		_timer -= (float)delta;

		if (_timer <= FadeOutDuration && !_fading)
			_fading = true;

		if (_fading)
		{
			float alpha = Mathf.Clamp(_timer / FadeOutDuration, 0f, 1f);
			_label.Modulate = new Color(1, 1, 1, alpha);
		}

		if (_timer <= 0)
		{
			_label.Visible = false;
			_fading = false;
		}
	}
}
