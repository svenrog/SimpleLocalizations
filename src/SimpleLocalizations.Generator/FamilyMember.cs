namespace SimpleLocalizations.Generator;

/// <summary>One member of a <see cref="FamilySet"/>: what it is called, and the word it is worded by.</summary>
internal readonly struct FamilyMember : IEquatable<FamilyMember>
{
    public FamilyMember(string name, string word)
    {
        Name = name;
        Word = word;
    }

    /// <summary>The member's own name, which is how an arm of the lookup names it.</summary>
    public string Name { get; }

    /// <summary>The key segment it is authored under, as <see cref="VocabularyKeys.Members"/> spells it.</summary>
    public string Word { get; }

    public bool Equals(FamilyMember other) => Name == other.Name && Word == other.Word;

    public override bool Equals(object? obj) => obj is FamilyMember other && Equals(other);

    public override int GetHashCode() => (Name, Word).GetHashCode();
}
