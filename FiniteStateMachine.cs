using System.Security.Cryptography;

namespace comp_lab1;

public struct State
{
    public string Name;
}

public struct Destination
{
    public State Dest;
    public string Output;
}

public class FiniteStateMachine
{
    public List<State> States = new();
    public string Alphabet;
    public Dictionary<State, List<Destination>> Rules = new();
    public State? StartState = null;
    public List<State> EndStates;

    //
    // public void ProcessInput(FsmTick tick)
    // {
    //     char ch = tick.Input[0];
    //     tick.Input = tick.Input[1..];
    //     
    //     Rules[tick.CurrentState]
    //
    // }
}

public class FsmTick
{
    public State CurrentState;
    public string Input;
}