public class NfaState
{
    public int Id { get; set; }
    public bool IsStart { get; set; } = false;
    public bool IsFinale { get; set; } = false;

    public NfaState(int id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        return Id == ((NfaState)obj!).Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}

