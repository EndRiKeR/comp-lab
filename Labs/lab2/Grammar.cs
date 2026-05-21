using comp_lab.Labs.lab2.structs;

namespace comp_lab.Labs.lab2;

public struct Grammar // G
{
    public HashSet<Nonterm> N;                                  // A, B, C
    public HashSet<Term> E;                                     // a, b, c
    public Dictionary<Nonterm, List<List<GrammarPart>>> P;      // A -> aAb | e
    public Nonterm S;                                           // e -> A
}
