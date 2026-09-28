using Godot;

public partial class MatchManager : Node
{
    public enum MatchPhase { FirstHalf, Halftime, SecondHalf, Overtime, SuddenDeath, Finished }
    [Export] public float HalfDuration = 300.0f;
    [Export] public float HalftimeDuration = 12.0f;
    [Export] public float OvertimeDuration = 60.0f;
    [Export] public NodePath BallPath;
    [Export] public NodePath PlayerPath;
    [Export] public NodePath Player2Path;
    [Export] public NodePath PlayerBlueAIPath;
    [Export] public float PostScoreRestartInset = 2.0f;
    public MatchPhase Phase { get; private set; } = MatchPhase.FirstHalf;
    public float TimeRemaining { get; private set; }
    public int BlueScore { get; private set; }
    public int RedScore { get; private set; }
    public bool AcceptScoring => Phase is MatchPhase.FirstHalf or MatchPhase.SecondHalf or MatchPhase.Overtime or MatchPhase.SuddenDeath;
    public bool IsGameplayActive => Phase != MatchPhase.Halftime && Phase != MatchPhase.Finished;
    public void RestartMatch()
    {
        Phase = MatchPhase.FirstHalf;
        TimeRemaining = HalfDuration;
        BlueScore = 0;
        RedScore = 0;
        ResetPlayersAndBall();
    }
    public string PhaseLabel => Phase switch { MatchPhase.FirstHalf => "1.er TIEMPO", MatchPhase.Halftime => "DESCANSO", MatchPhase.SecondHalf => "2.º TIEMPO", MatchPhase.Overtime => "PRÓRROGA", MatchPhase.SuddenDeath => "MUERTE SÚBITA", _ => "FINAL" };
    private Ball _ball;
    private float _transitionTimer;
    private MatchPhase _nextPhase;
    private bool _transitioning;
    public override void _Ready() { TimeRemaining = HalfDuration; _ball = GetNodeOrNull<Ball>(BallPath); foreach (Node node in GetTree().GetNodesInGroup("score_zones")) if (node is ScoreZone zone) zone.ShotScored += OnShotScored; }
    public override void _Process(double delta) { if (_transitioning) { _transitionTimer -= (float)delta; if (_transitionTimer <= 0) FinishTransition(); return; } if (!AcceptScoring || Phase == MatchPhase.SuddenDeath) return; TimeRemaining = Mathf.Max(0, TimeRemaining - (float)delta); if (TimeRemaining <= 0) EndPeriod(); }
    public int AttackingTeamForHoop(float z) => Phase == MatchPhase.FirstHalf ? (z < 0 ? 0 : 1) : (z < 0 ? 1 : 0);
    public Vector3 GetAttackingHoop(int teamId, Vector3 south, Vector3 north) => AttackingTeamForHoop(south.Z) == teamId ? south : north;
    public void AddScore(int points, int teamId) { if (!AcceptScoring) return; if (teamId == 0) BlueScore += points; else RedScore += points; if (Phase == MatchPhase.SuddenDeath) FinishMatch(); }
    private void OnShotScored(int points, int scorerTeamId)
    {
        if (_ball == null || !AcceptScoring)
            return;

        int restartTeamId = scorerTeamId == 0 ? 1 : 0;
        Vector3 scoredHoop = _ball.GlobalPosition.Z < 0.0f ? new Vector3(0, 3, -20) : new Vector3(0, 3, 20);
        Vector3 position = new(0, 1, scoredHoop.Z < 0.0f ? -18.0f : 18.0f);
        Player passer = FindClosestPlayer(restartTeamId, position);
        Player receiver = FindClosestPlayer(restartTeamId, position, passer);
        if (passer == null)
            return;

        Vector3 target = receiver != null ? receiver.GlobalPosition : position + (scoredHoop.Z < 0 ? Vector3.Back : Vector3.Forward) * 5.0f;
        _ball.FreezeForRestart();
        passer.PreparePostScoreRestart(position, target);
    }

    private static Player FindClosestPlayer(int teamId, Vector3 position, Player excluded = null)
    {
        Player best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Player player in Player.Players)
        {
            if (!IsInstanceValid(player) || player.TeamId != teamId || player == excluded)
                continue;
            float distance = player.GlobalPosition.DistanceSquaredTo(position);
            if (distance < bestDistance) { bestDistance = distance; best = player; }
        }
        return best;
    }
    private void EndPeriod() { if (Phase == MatchPhase.FirstHalf) BeginTransition(MatchPhase.SecondHalf); else if (Phase == MatchPhase.SecondHalf) { if (BlueScore == RedScore) BeginTransition(MatchPhase.Overtime); else FinishMatch(); } else if (Phase == MatchPhase.Overtime) { if (BlueScore == RedScore) { Phase = MatchPhase.SuddenDeath; TimeRemaining = 0; } else FinishMatch(); } }
    private void BeginTransition(MatchPhase next) { Phase = MatchPhase.Halftime; _nextPhase = next; _transitioning = true; _transitionTimer = HalftimeDuration; _ball?.FreezeForRestart(); }
    private void FinishTransition() { _transitioning = false; Phase = _nextPhase; TimeRemaining = Phase == MatchPhase.SecondHalf ? HalfDuration : OvertimeDuration; ResetPlayersAndBall(); }
    private void ResetPlayersAndBall() { Player[] players = { GetNodeOrNull<Player>(PlayerPath), GetNodeOrNull<Player>(Player2Path), GetNodeOrNull<Player>(PlayerBlueAIPath) }; Vector3[] positions = { new(-2.2f, 1, 5), new(2.2f, 1, 5), new(0, 1, 2) }; for (int i = 0; i < players.Length; i++) if (players[i] != null) { players[i].GlobalPosition = positions[i]; players[i].Velocity = Vector3.Zero; players[i].ResetMatchState(); } if (_ball != null) { _ball.GlobalPosition = new(0, 0.5f, 0); _ball.ResumeAfterRestart(); _ball.RecordContact(_ball.GlobalPosition); } }
    private void FinishMatch() { Phase = MatchPhase.Finished; TimeRemaining = 0; _ball?.FreezeForRestart(); }
}
