using Godot;

namespace Orbitals;


public partial class GameFlow : Control
{
    [Export] public Node ScreenHost { get; set; }
    [Export] public PackedScene StartScene { get; set; }
    [Export] public PackedScene GameplayScene { get; set; }
    [Export] public PackedScene EndScene { get; set; }
    public Node CurrentScreen { get; private set; }
    public RunResult LastResult { get; private set; }
    private bool _transitionPending;

    
    public override void _Ready() => ShowMenu();

    
    private void RequestRound() => RequestTransition(StartRound);
    private void RequestMenu() => RequestTransition(ShowMenu);

    
    private void RequestTransition(System.Action transition)
    {
        if (_transitionPending) return;
        _transitionPending = true;
        Callable.From(transition).CallDeferred();
    }

    
    private void StartRound()
    {
        LastResult = null;
        var round = GameplayScene.Instantiate<GameRound>();
        round.RoundFinished += OnRoundFinished;
        ReplaceScreen(round);
    }

    
    private void OnRoundFinished(RunResult result)
    {
        LastResult = result;
        RequestTransition(ShowResults);
    }

    
    private void ShowResults()
    {
        var screen = EndScene.Instantiate<EndScreen>();
        screen.Result = LastResult ?? new RunResult();
        screen.RetryRequested += RequestRound;
        screen.MenuRequested += RequestMenu;
        ReplaceScreen(screen);
    }

    
    private void ShowMenu()
    {
        var screen = StartScene.Instantiate<StartScreen>();
        screen.PlayRequested += RequestRound;
        ReplaceScreen(screen);
    }


    private void ReplaceScreen(Node screen)
    {
        if (GodotObject.IsInstanceValid(CurrentScreen))
        {
            if (CurrentScreen is GameRound round)
            {
                round.RoundFinished -= OnRoundFinished;
                round.StopRound();
            }
            ScreenHost.RemoveChild(CurrentScreen);
            CurrentScreen.QueueFree();
        }
        CurrentScreen = screen;
        ScreenHost.AddChild(screen);
        _transitionPending = false;
    }
}
