namespace comp_lab1;

public class Fragment
{
    public State Head { get; }
    public List<State> Tail { get; set; }

    public Fragment(State head, List<State> tail)
    {
        Head = head;
        Tail = tail;
    }
}