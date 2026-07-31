namespace SimpleLocalizations;

/// <summary>
/// One project's localized text, looked up by key rather than resolved against a catalog the caller holds.
/// <para>
/// A seam rather than a single catalog because of the project graph: text belongs to the project that wrote
/// it, and a spine cannot see the resources of the plug-ins above it. Sources are registered the way plug-ins
/// are — the injected list <em>is</em> the registry — so a new one ships its own resource file without
/// editing the spine.
/// </para>
/// <para>
/// Resolution happens at the <b>read</b> edge, never where the record was produced: something produced once
/// and read many times would otherwise freeze one reader's culture into stored data.
/// </para>
/// </summary>
public interface ILocalizedTextSource
{
    /// <summary>
    /// The template for <paramref name="name"/> in the ambient UI culture, or <see langword="false"/> when
    /// this source does not own it.
    /// </summary>
    bool TryGetTemplate(string name, out string template);
}
