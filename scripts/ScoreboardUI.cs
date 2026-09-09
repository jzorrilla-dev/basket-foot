using Godot;

public partial class ScoreboardUI : CanvasLayer
{
	[Export] public float FlashDuration = 1.6f;

	private Label _scoreLabel;
	private Label _lastScoreLabel;
	private int _blueScore;
	private int _redScore;
	private float _flashTimer;

	public override void _Ready()
	{
		_scoreLabel = GetNode<Label>("ScoreLabel");
		_lastScoreLabel = GetNode<Label>("LastScoreLabel");
		_lastScoreLabel.Visible = false;
		UpdateScoreLabel();

		foreach (Node node in GetTree().GetNodesInGroup("score_zones"))
		{
			if (node is ScoreZone scoreZone)
			{
				scoreZone.ShotScored += OnShotScored;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!_lastScoreLabel.Visible)
			return;

		_flashTimer -= (float)delta;
		float alpha = Mathf.Clamp(_flashTimer / FlashDuration, 0.0f, 1.0f);
		_lastScoreLabel.Modulate = new Color(1, 1, 1, alpha);

		if (_flashTimer <= 0.0f)
		{
			_lastScoreLabel.Visible = false;
		}
	}

	private void OnShotScored(int points, bool scorerIsAI)
	{
		if (scorerIsAI)
			_redScore += points;
		else
			_blueScore += points;

		UpdateScoreLabel();
		_lastScoreLabel.Text = $"+{points} {(scorerIsAI ? "ROJO" : "AZUL")}";
		_lastScoreLabel.Modulate = Colors.White;
		_lastScoreLabel.Visible = true;
		_flashTimer = FlashDuration;
	}

	private void UpdateScoreLabel()
	{
		_scoreLabel.Text = $"AZUL {_blueScore}  -  {_redScore} ROJO";
	}
}
