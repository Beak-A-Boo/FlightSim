using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public struct ControlInputs : IReadOnlyFlightInputs
{
    public double Throttle;
    public double YawRate;
    public double PitchRate;
    public double RollRate;

    public readonly double GetThrottle() => Throttle;
    public readonly double GetYawRate() => YawRate;
    public readonly double GetPitchRate() => PitchRate;
    public readonly double GetRollRate() => RollRate;
}

public partial class FlightControlComputer : RigidBody3D
{

    public double TargetThrottle { get; private set; }
    public double TargetYawRate { get; private set; }
    public double TargetPitchRate { get; private set; }
    public double TargetRollRate { get; private set; }

    // only need throttle since the other properties are stored on the physics body directly
    public double Throttle { get; private set; }

    [Export]
    public double AutoPilotDisconnectThreshold = 0.18;

    [Export(PropertyHint.Range, "0,1")]
    public double UpdateSpeed = 0.12;

    [Export(PropertyHint.Range, "0,1000000000,exp")]
    public double ThrustScaling = 1000;

    [Export(PropertyHint.Range, "0.0001,1")]
    public double IdleThrust = 0.1;

    [Export(PropertyHint.Range, "0.0001,360")]
    public double MaxYawRate = 90;

    [Export(PropertyHint.Range, "0.0001,360")]
    public double MaxPitchRate = 90;

    [Export(PropertyHint.Range, "0.0001,360")]
    public double MaxRollRate = 90;

    private IList<FlightInputs> activeInputs = new List<FlightInputs>();
    private IList<FlightInputs> automaticInputs = new List<FlightInputs>();

    public override void _PhysicsProcess(double delta)
    {
        var receivedActiveInputs = 0;
        var playerControls = new ControlInputs();
        foreach (FlightInputs flightInput in activeInputs)
        {
            flightInput.Update(this, delta);

            if (flightInput.Active)
            {
                receivedActiveInputs++;

                playerControls.Throttle += flightInput.Throttle;
                playerControls.YawRate += flightInput.YawRate;
                playerControls.PitchRate += flightInput.PitchRate;
                playerControls.RollRate += flightInput.RollRate;
            }
        }
        var dualInput = receivedActiveInputs > 1;

        if (dualInput)
        {
            // TODO make this a warning indicator in the UI
            GD.Print("DUAL INPUT");

            // beed to average the inputs
            playerControls.Throttle /= receivedActiveInputs;
            playerControls.YawRate /= receivedActiveInputs;
            playerControls.PitchRate /= receivedActiveInputs;
            playerControls.RollRate /= receivedActiveInputs;
        }

        IList<KeyValuePair<ControlInputs, FlightInputs>> autopilotInputs = new List<KeyValuePair<ControlInputs, FlightInputs>>(automaticInputs.Count);
        foreach (FlightInputs flightInput in automaticInputs)
        {
            flightInput.Update(this, delta);

            if (flightInput.Active)
            {
                // always disconnect automatics on dual input
                if (dualInput)
                {
                    flightInput.Active = false;
                    continue;
                }

                var inputs = new ControlInputs();
                if (!QueryOrDisconnect(flightInput, playerControls, x => x.GetThrottle(), out inputs.Throttle)) continue;
                if (!QueryOrDisconnect(flightInput, playerControls, x => x.GetYawRate(), out inputs.YawRate)) continue;
                if (!QueryOrDisconnect(flightInput, playerControls, x => x.GetPitchRate(), out inputs.PitchRate)) continue;
                if (!QueryOrDisconnect(flightInput, playerControls, x => x.GetRollRate(), out inputs.RollRate)) continue;
                autopilotInputs.Add(new(inputs, flightInput));
            }
        }

        var autopilotActive = autopilotInputs.Count > 0;
        ControlInputs targetInputs = new ControlInputs()
        {
            Throttle = playerControls.Throttle,
            YawRate = playerControls.YawRate,
            PitchRate = playerControls.PitchRate,
            RollRate = playerControls.RollRate
        };

        if (autopilotActive)
        {
            double minT = 0;
            double maxT = 0;
            double minY = 0;
            double maxY = 0;
            double minP = 0;
            double maxP = 0;
            double minR = 0;
            double maxR = 0;
            foreach (var input in autopilotInputs)
            {
                minT = Mathf.Min(minT, input.Key.Throttle);
                maxT = Mathf.Max(maxT, input.Key.Throttle);
                minY = Mathf.Min(minY, input.Key.YawRate);
                maxY = Mathf.Max(maxY, input.Key.YawRate);
                minP = Mathf.Min(minP, input.Key.PitchRate);
                maxP = Mathf.Max(maxP, input.Key.PitchRate);
                minR = Mathf.Min(minR, input.Key.RollRate);
                maxR = Mathf.Max(maxR, input.Key.RollRate);
            }

            if (Mathf.Abs(maxT - minT) > AutoPilotDisconnectThreshold)
            {
                GD.Print("AP Throttle conflict");
                autopilotActive = false;
            }
            else if (Mathf.Abs(maxY - minY) > AutoPilotDisconnectThreshold)
            {
                GD.Print("AP Yaw conflict");
                autopilotActive = false;
            }
            else if (Mathf.Abs(maxP - minP) > AutoPilotDisconnectThreshold)
            {
                GD.Print("AP Pitch conflict");
                autopilotActive = false;
            }
            else if (Mathf.Abs(maxR - minR) > AutoPilotDisconnectThreshold)
            {
                GD.Print("AP Roll conflict");
                autopilotActive = false;
            }

            if (!autopilotActive)
            {
                GD.Print("Disconnecting all autopilots");
                foreach (var input in autopilotInputs)
                {
                    input.Value.Active = false;
                }

                // skip averaging and apply raw player inputs
                goto apply;
            }

            // finally average all autopilot inputs with each other and then with the player inputs
            targetInputs = new ControlInputs()
            {
                Throttle = (playerControls.GetThrottle() + autopilotInputs.Average(x => x.Key.GetThrottle())) / 2,
                YawRate = (playerControls.GetYawRate() + autopilotInputs.Average(x => x.Key.GetYawRate())) / 2,
                PitchRate = (playerControls.GetPitchRate() + autopilotInputs.Average(x => x.Key.GetPitchRate())) / 2,
                RollRate = (playerControls.GetRollRate() + autopilotInputs.Average(x => x.Key.GetRollRate())) / 2,
            };
        }

    apply:
        UpdateFlightState(in targetInputs, in delta);
    }

    private void UpdateFlightState(in ControlInputs targetInputs, in double delta)
    {
        //rotation first
        TargetYawRate = Mathf.Clamp(targetInputs.YawRate, -1, 1) * Mathf.DegToRad(MaxYawRate) * delta;
        TargetPitchRate = Mathf.Clamp(targetInputs.PitchRate, -1, 1) * Mathf.DegToRad(MaxPitchRate) * delta;
        TargetRollRate = Mathf.Clamp(targetInputs.RollRate, -1, 1) * Mathf.DegToRad(MaxRollRate) * delta;

        ApplyTorque(new Vector3((float)TargetYawRate, (float)TargetPitchRate, (float)TargetRollRate));

        // forward momentum last
        TargetThrottle = Mathf.Clamp(TargetThrottle + targetInputs.Throttle, -1, 1);
        Throttle = Mathf.MoveToward(Throttle, TargetThrottle, UpdateSpeed * delta);

        // apply minimum idle thrust unless actively braking
        var targetThrottle = Throttle;
        if (targetThrottle >= 0 && targetThrottle < IdleThrust)
        {
            targetThrottle = IdleThrust;
        }

        var forward = GlobalBasis.Z.Normalized();
        var thrust = forward * (float)(targetThrottle * ThrustScaling * delta);
        ApplyCentralForce(thrust);


        //TODO debug
        GD.Print($"Throttle: {targetThrottle} ({Throttle}), YawRate: {TargetYawRate}, PitchRate: {TargetPitchRate}, RollRate: {TargetRollRate}");
    }

    private bool QueryOrDisconnect(in FlightInputs inputs, in ControlInputs playerInputs, Func<IReadOnlyFlightInputs, double> query, out double value)
    {
        var currentValue = query(playerInputs);
        var raw = query(inputs);
        if (Mathf.Abs(raw - currentValue) > AutoPilotDisconnectThreshold)
        {
            // TODO 'Active' indicator for each autopilot in the UI
            GD.Print($"Disconnecting {inputs.Name}");
            inputs.Active = false;
            value = currentValue;
            return false;
        }

        value = (currentValue + raw) / 2;
        return true;
    }

    internal void AddInput(FlightInputs inputs)
    {
        switch (inputs.Type)
        {
            case FlightInputType.ACTIVE:
                activeInputs.Add(inputs);
                break;
            case FlightInputType.PASSIVE:
                automaticInputs.Add(inputs);
                break;
            default:
                GD.PushError($"Unknown FlightInputType: ${inputs.Name}");
                GetTree().Quit();
                return;
        };
    }
}