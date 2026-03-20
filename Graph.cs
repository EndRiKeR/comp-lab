using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class State
{
    public int Id { get; }
    public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
    public bool IsAccept { get; set; }

    public State(int id)
    {
        Id = id;
    }

    public void AddTransition(char symbol, State target)
    {
        if (!Transitions.ContainsKey(symbol)) Transitions[symbol] = new List<State>();
        Transitions[symbol].Add(target);
    }
}

public class Fragment
{
    public State Start { get; }
    public List<State> Accept { get; set; }

    public Fragment(State start, List<State> accept)
    {
        Start = start;
        Accept = accept;
    }
}