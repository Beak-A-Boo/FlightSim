public interface FlightInputs: IReadOnlyFlightInputs
{

    FlightInputType Type { get; }
    string Name { get; }

    bool Active { get; set; }

    double Throttle { get; }
    double YawRate { get; }
    double PitchRate { get; }
    double RollRate { get; }

    void Update(in FlightControlComputer fcc, in double delta);
}

public enum FlightInputType {
    // players
    ACTIVE,

    // auto pilot etc
    PASSIVE
}

public interface IReadOnlyFlightInputs {
    double GetThrottle();

    double GetYawRate();

    double GetPitchRate();

    double GetRollRate();
}