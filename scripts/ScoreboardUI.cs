using Godot;

public partial class ScoreboardUI : CanvasLayer
{
	[Export] public float FlashDuration = 1.6f;

	private Label _scoreLabel;
	private Label _lastScoreLabel;
	private int _blueScore;
	private int _redScore;
	private float _flashTimer;
	private MatchManager _match;
	private Label _matchStateLabel;

	public override void _Ready()
	{
		_scoreLabel = GetNode<Label>("ScoreLabel");
		_lastScoreLabel = GetNode<Label>("LastScoreLabel");
		_match = GetNodeOrNull<MatchManager>("../MatchManager");
		_matchStateLabel = GetNodeOrNull<Label>("MatchStateLabel");
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
		if (_match != null)
		{
			int seconds = Mathf.CeilToInt(_match.TimeRemaining);
			int minutes = seconds / 60;
			_scoreLabel.Text = $"AZUL {_blueScore}  -  {_redScore} ROJO    {minutes:00}:{seconds % 60:00}";
			if (_matchStateLabel != null)
				_matchStateLabel.Text = _match.PhaseLabel;
		}
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

	private void OnShotScored(int points, int scorerTeamId)
	{
		if (scorerTeamId == 1)
			_redScore += points;
		else
			_blueScore += points;

		UpdateScoreLabel();
		_lastScoreLabel.Text = $"+{points} {(scorerTeamId == 1 ? "ROJO" : "AZUL")}";
		_lastScoreLabel.Modulate = Colors.White;
		_lastScoreLabel.Visible = true;
		_flashTimer = FlashDuration;
	}

	private void UpdateScoreLabel()
	{
		_scoreLabel.Text = $"AZUL {_blueScore}  -  {_redScore} ROJO";
	}
}
