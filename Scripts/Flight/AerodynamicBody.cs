using System;
using Godot;

public partial class AerodynamicBody : Node
{

    [Export]
    public RigidBody3D Body { get; set; } = null!;

    public virtual Vector3 GetForwardVelocity()
    {
        return Body.GlobalTransform.Inverse() * Body.LinearVelocity;
    }

    public virtual Vector3 GetAirSpeed()
    {
        return -GetForwardVelocity();
    }

    public virtual Vector3 GetForward()
    {
        return Body.GlobalBasis.Z.Normalized();
    }

    public virtual Vector3 GetGlobalUp()
    {
        return Vector3.Up;
    }

    public override void _PhysicsProcess(double delta)
    {
        var airSpeed = GetAirSpeed().Length();

        var lift = GetGlobalUp() * airSpeed * 0.005F;

        Body.ApplyCentralForce(lift);

    }

}