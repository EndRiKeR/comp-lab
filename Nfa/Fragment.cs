namespace comp_lab1;

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