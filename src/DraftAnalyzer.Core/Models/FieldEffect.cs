namespace DraftAnalyzer.Core.Models;

public sealed record FieldEffect(string Name, FieldEffectKind Kind, string? Side = null, int Layers = 1);
