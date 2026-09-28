using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	private ColorRect _shade;
	private PanelContainer _panel;
	private Button _pauseButton;
	private MatchManager _match;

	public override void _Ready()
	{
		_match = GetNodeOrNull<MatchManager>("../MatchManager");
		BuildMenu();
		SetPaused(false);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			SetPaused(!GetTree().Paused);
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildMenu()
	{
		_pauseButton = new Button { Text = "Pausa  [Esc]", CustomMinimumSize = new Vector2(150, 38), AnchorLeft = 1.0f, AnchorRight = 1.0f, OffsetLeft = -170.0f, OffsetTop = 18.0f, OffsetRight = -20.0f, OffsetBottom = 56.0f };
		_pauseButton.Pressed += () => SetPaused(true);
		AddChild(_pauseButton);
		_shade = new ColorRect { Color = new Color(0, 0, 0, 0.62f), AnchorsPreset = (int)Control.LayoutPreset.FullRect, MouseFilter = Control.MouseFilterEnum.Stop };
		AddChild(_shade);
		_panel = new PanelContainer { CustomMinimumSize = new Vector2(320, 0), AnchorsPreset = (int)Control.LayoutPreset.Center };
		AddChild(_panel);
		var box = new VBoxContainer { Theme = null };
		box.AddThemeConstantOverride("separation", 10);
		box.AddChild(new Label { Text = "PAUSA", HorizontalAlignment = HorizontalAlignment.Center });
		AddButton(box, "Continuar", () => SetPaused(false));
		AddButton(box, "Reiniciar partido", () => { _match?.RestartMatch(); SetPaused(false); });
		AddButton(box, "Salir del juego", () => GetTree().Quit());
		_panel.AddChild(box);
	}

	private static void AddButton(VBoxContainer box, string text, Action action)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(280, 42) };
		button.Pressed += action;
		box.AddChild(button);
	}

	private void SetPaused(bool paused)
	{
		GetTree().Paused = paused;
		_panel.Visible = paused;
		_shade.Visible = paused;
		_pauseButton.Visible = !paused;
		// El menú debe seguir recibiendo clics y Esc tanto en juego como pausado.
		ProcessMode = ProcessModeEnum.Always;
	}
}
