using Godot;

/// <summary>
/// Attach this class to the scene tree to listen to flight control inputs by the player.
/// </summary>
public partial class PlayerFlightInputs : Node, FlightInputs
{

    const string THROTTLE_INCREASE = "throttle_increase";
    const string THROTTLE_DECREASE = "throttle_decrease";
    const string YAW_LEFT = "yaw_left";
    const string YAW_RIGHT = "yaw_right";
    const string PITCH_UP = "pitch_up";
    const string PITCH_DOWN = "pitch_down";
    const string ROLL_LEFT = "roll_left";
    const string ROLL_RIGHT = "roll_right";

    [Export]
    public FlightControlComputer FlightControlComputer { get; set; } = null!;

    [Export]
    public ThrottleSlowdown ThrottleSlowdown = ThrottleSlowdown.IDLE;

    public bool Active { get; set; }
    // less than 0 = active braking
    public double Throttle { get; private set; }
    public double YawRate { get; private set; }
    public double PitchRate { get; private set; }
    public double RollRate { get; private set; }

    public override void _Ready()
    {
        if (FlightControlComputer == null)
        {
            GD.PrintErr("FlightControlComputer is not set.");
            GetTree().Quit();
            return;
        }

        FlightControlComputer.AddInput(this);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        Active = false;

//TODO auto reset
        // if (ThrottleSlowdown != ThrottleSlowdown.NONE)
        // {
        //     var targetThrottle = ThrottleSlowdown switch
        //     {
        //         ThrottleSlowdown.IDLE => 0.0,
        //         ThrottleSlowdown.BRAKING => -0.2,
        //         _ => 0.0,
        //     };
        //     Throttle = Mathf.MoveToward(Throttle, targetThrottle, delta);
        // }

        if (Input.IsActionPressed(THROTTLE_INCREASE))
        {
            Throttle = Mathf.Clamp(Mathf.MoveToward(Throttle, Throttle + 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(THROTTLE_DECREASE))
        {
            Throttle = Mathf.Clamp(Mathf.MoveToward(Throttle, Throttle - 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(YAW_LEFT))
        {
            YawRate = Mathf.Clamp(Mathf.MoveToward(YawRate, YawRate + 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(YAW_RIGHT))
        {
            YawRate = Mathf.Clamp(Mathf.MoveToward(YawRate, YawRate - 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(PITCH_UP))
        {
            PitchRate = Mathf.Clamp(Mathf.MoveToward(PitchRate, PitchRate - 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(PITCH_DOWN))
        {
            PitchRate = Mathf.Clamp(Mathf.MoveToward(PitchRate, PitchRate + 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(ROLL_LEFT))
        {
            RollRate = Mathf.Clamp(Mathf.MoveToward(RollRate, RollRate - 0.2, delta), -1, 1);
            Active = true;
        }

        if (Input.IsActionPressed(ROLL_RIGHT))
        {
            RollRate = Mathf.Clamp(Mathf.MoveToward(RollRate, RollRate + 0.2, delta), -1, 1);
            Active = true;
        }

        GD.Print($"Player Throttle: {Throttle}, YawRate: {YawRate}, PitchRate: {PitchRate}, RollRate: {RollRate}");
    }


    public void Update(in FlightControlComputer fcc, in double delta)
    {
        //TODO mirror flight computer to real outputs
        // Throttle = Mathf.MoveToward(Throttle, fcc.TargetThrottle, delta);
        // PitchRate = Mathf.MoveToward(PitchRate, fcc.TargetPitchRate, delta);
        // YawRate = Mathf.MoveToward(YawRate, fcc.TargetYawRate, delta);
        // RollRate = Mathf.MoveToward(RollRate, fcc.TargetRollRate, delta);
    }

    public double GetThrottle() => Throttle;

    public double GetYawRate() => YawRate;

    public double GetPitchRate() => PitchRate;

    public double GetRollRate() => RollRate;

    string FlightInputs.Name => "Player";
    public FlightInputType Type => FlightInputType.ACTIVE;
}

public enum ThrottleSlowdown
{
    // no slowdown
    NONE,
    // slow down to idle
    IDLE,
    // slow down to idle and then start slightly braking (-0.2)
    BRAKING
}