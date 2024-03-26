using Godot;
using System;

public partial class PlayerInputs : Node
{
	// debug keys
	const string DEBUG_PAUSE_MENU = "DEBUG_pause_menu";
	const string DEBUG_RESET_POSITION = "DEBUG_reset_position";

	[Export]
	public Node3D PlayerAnchor { get; set; } = null!;

	private Transform3D PlayerAnchorReset { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(PlayerAnchor != null) {
			PlayerAnchorReset = PlayerAnchor.GlobalTransform;
		}
		else {
			GD.PushWarning("PlayerAnchor not set!");
		}
	}

	public override void _Input(InputEvent @event)
	{
		if(HandleDebugInputs(@event)) {
			return;
		}

		// text input?
	}

	private bool HandleDebugInputs(InputEvent @event)
	{
		if(@event.IsActionPressed(DEBUG_PAUSE_MENU)) {
			GD.Print("Saving and exiting game...");
			GetTree().Quit();
			return true;
		}
		if(@event.IsActionPressed(DEBUG_RESET_POSITION)) {
			GD.Print("Reset position");
			if(PlayerAnchor != null) {
				PlayerAnchor.GlobalTransform = PlayerAnchorReset;
			}
			else {
				GD.PushWarning("PlayerAnchor not set in PlayerInputs");
			}
		}

		// true to cancel further input processing
		return false;
	}
}
