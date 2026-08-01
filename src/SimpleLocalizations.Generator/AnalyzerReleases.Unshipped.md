; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SL1001 | SimpleLocalizations | Warning | A vocabulary key is not lowercase and dotted.
SL1002 | SimpleLocalizations | Warning | A vocabulary key is a prefix of another key.
SL1003 | SimpleLocalizations | Warning | A vocabulary resource declares no key type.
SL1004 | SimpleLocalizations | Warning | A vocabulary resource could not be read.
SL1005 | SimpleLocalizations | Warning | A vocabulary resource authors no keys.
SL1006 | SimpleLocalizations | Warning | A vocabulary key type declaration is not a default followed by families.
SL1007 | SimpleLocalizations | Warning | Two vocabulary keys produce one member name.
SL1008 | SimpleLocalizations | Warning | A vocabulary key names the class it nests in.
SL1010 | SimpleLocalizations | Warning | A vocabulary key is produced outside its declaration.
SL1011 | SimpleLocalizations | Warning | A declared family member has no key authored for it.
SL1012 | SimpleLocalizations | Warning | A key is authored outside the families its type is filed under.
SL1013 | SimpleLocalizations | Warning | A vocabulary key type is not partial.
SL1014 | SimpleLocalizations | Warning | A declared family enumerates no members.
SL1015 | SimpleLocalizations | Warning | A vocabulary key type is nested in a type its body cannot be written into.
