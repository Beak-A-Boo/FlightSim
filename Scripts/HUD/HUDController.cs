using Godot;

public partial class HUDController : Node2D
{

    [Export]
    public FlightControlComputer FlightControlComputer { get; set; } = null!;

    [Export]
    public Label PositionLabel { get; set; } = null!;

    [Export]
    public Label VelocityLabel { get; set; } = null!;

    public override void _Ready()
    {
        if (FlightControlComputer == null)
        {
            GD.PrintErr("FlightControlComputer is not set.");
            GetTree().Quit();
            return;
        }
    }

    public override void _Process(double delta)
    {
        var pos = FlightControlComputer.GlobalPosition;
        PositionLabel.Text = $"Position: {pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}";

        var vel = FlightControlComputer.LinearVelocity.Length();
        VelocityLabel.Text = $"Velocity: {vel:F2}";
    }

}