
namespace PlaneWaver
{
    public enum ValueLimiter { Clip, Repeat, PingPong }
    public enum InputOnNewValue { Replace, Accumulate }
    public enum InputSourceGroups { General, PrimaryActor, LinkedActors, ActorCollisions }
    public enum GeneralSources { StaticValue, TimeSinceStart, DeltaTime, SpawnAge, SpawnAgeNorm }
    public enum PrimaryActorSources { Scale, Mass, MassTimesScale, Speed, AngularSpeed, Acceleration, SlideMomentum, RollMomentum }
    public enum LinkedActorSources { DistanceX, DistanceY, DistanceZ, Radius, Polar, Elevation, RelativeSpeed, TangentialSpeed }
    public enum ActorCollisionSources { CollisionSpeed, CollisionForce }
}