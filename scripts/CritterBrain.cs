using Godot;

public enum BehaviourState { Idle, Wander, ReturnHome }

public partial class CritterBrain : Node
{
    [Signal] public delegate void StateChangedEventHandler(BehaviourState previous, BehaviourState current);

    public BehaviourState State { get; private set; } = BehaviourState.Idle;
    private Critter _body;
    private readonly RandomNumberGenerator _rng = new();
    private float _idleTimer;
    private float _targetAngle;
    private SpeciesProfile Profile => _body.Profile;

    public override void _Ready()
    {
        _body = GetParent<Critter>();
        _body.Profile ??= new SpeciesProfile();
        _rng.Randomize();
        EnterState(State);
    }


    public void Tick(float dt)
    {
        if (!_body.InHome(_body.Angle)) ChangeState(BehaviourState.ReturnHome);
        else if (State == BehaviourState.Wander && !_body.InHome(_targetAngle))
            ChangeState(BehaviourState.Idle);

        switch (State)
        {
            case BehaviourState.Idle:
                _idleTimer -= dt;
                if (_idleTimer <= 0f) ChangeState(BehaviourState.Wander);
                break;
            case BehaviourState.Wander:
                if (WalkTo(_targetAngle, dt)) ChangeState(BehaviourState.Idle);
                break;
            case BehaviourState.ReturnHome:
                if (WalkTo(_body.HomeCenter, dt)) ChangeState(BehaviourState.Idle);
                break;
        }
    }

    public void OnEnvironmentChanged()
    {
        if (State == BehaviourState.Idle) EnterState(State);
        else ChangeState(BehaviourState.Idle);
    }


    private bool WalkTo(float angle, float dt)
        => _body.MoveTowardAngle(angle, Profile.WalkSpeed, dt, Profile.ArrivalDistance);

    private void ChangeState(BehaviourState next)
    {
        if (State == next) return;
        BehaviourState previous = State;
        ExitState(previous);
        State = next;
        EnterState(next);
        EmitSignal(SignalName.StateChanged, (int)previous, (int)next);
    }

    private void EnterState(BehaviourState state)
    {
        switch (state)
        {
            case BehaviourState.Idle:
                float minimum = Mathf.Max(0.1f, Profile.IdleTimeMin);
                float maximum = Mathf.Max(minimum, Profile.IdleTimeMax);
                _idleTimer = _rng.RandfRange(minimum, maximum);
                _body.StopMoving();
                break;
            case BehaviourState.Wander:
                _targetAngle = _body.RandomWanderAngle(_rng);
                break;
        }
    }

    private void ExitState(BehaviourState state)
    {
        _body.StopMoving();
        
    }
}
