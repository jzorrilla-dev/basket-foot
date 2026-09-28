using Godot;

public partial class AudioManager : Node
{
	private AudioStreamPlayer _music;
	private AudioStreamGeneratorPlayback _musicPlayback;
	private float _phase;
	private float _musicTime;

	public override void _Ready()
	{
		_music = new AudioStreamPlayer { Stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = 0.5f }, VolumeDb = -20.0f };
		AddChild(_music);
		_music.Play();
		_musicPlayback = (AudioStreamGeneratorPlayback)_music.GetStreamPlayback();
	}

	public override void _Process(double delta)
	{
		if (_musicPlayback == null) return;
		int frames = _musicPlayback.GetFramesAvailable();
		for (int i = 0; i < frames; i++)
		{
			float beat = Mathf.Sin(_phase) * 0.035f + Mathf.Sin(_phase * 1.5f) * 0.018f;
			_musicPlayback.PushFrame(new Vector2(beat, beat));
			_phase += Mathf.Tau * (110.0f + 18.0f * Mathf.Sin(_musicTime * 0.35f)) / 22050.0f;
			_musicTime += 1.0f / 22050.0f;
		}
	}

	public void PlaySfx(string kind)
	{
		float frequency = kind switch { "kick" => 135, "pass" => 420, "bounce" => 190, "jump" => 280, "step" => 95, "score" => 660, _ => 180 };
		float duration = kind switch { "score" => 0.55f, "kick" => 0.28f, "jump" => 0.24f, _ => 0.16f };
		var stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = duration + 0.05f };
		var player = new AudioStreamPlayer { Stream = stream, VolumeDb = kind == "step" ? -18 : -5 };
		AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
		var playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback();
		int count = Mathf.RoundToInt(22050.0f * duration);
		for (int i = 0; i < count && playback.GetFramesAvailable() > 0; i++)
		{
			float envelope = 1.0f - i / (float)count;
			float bend = kind == "score" ? 1.0f + 0.35f * (i / (float)count) : 1.0f;
			float harmonic = Mathf.Sin(Mathf.Tau * frequency * 2.01f * i / 22050.0f) * 0.22f;
			float sample = (Mathf.Sin(Mathf.Tau * frequency * bend * i / 22050.0f) + harmonic) * envelope * 0.38f;
			if (kind == "score" && i > count / 2)
				sample += Mathf.Sin(Mathf.Tau * frequency * 1.5f * i / 22050.0f) * envelope * 0.25f;
			playback.PushFrame(new Vector2(sample, sample));
		}
	}
}
