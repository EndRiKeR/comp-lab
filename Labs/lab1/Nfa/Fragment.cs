namespace comp_lab.Labs.lab1.Nfa;

public class Fragment
{
    public NfaState Head { get; }
    public List<NfaState> Tail { get; set; }

    public Fragment(NfaState head, List<NfaState> tail)
    {
        Head = head;
        Tail = tail;
    }
}

