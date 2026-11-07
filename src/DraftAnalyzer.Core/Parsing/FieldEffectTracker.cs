using DraftAnalyzer.Core.Models;

namespace DraftAnalyzer.Core.Parsing;

internal static class FieldEffectTracker
{
    public static IReadOnlyList<FieldEffect> Apply(
        IReadOnlyList<FieldEffect> effects, string command, string[] arguments)
    {
        if (command == "-swapsideconditions")
        {
            return Array.AsReadOnly(effects.Select(effect => effect.Side switch
            {
                "p1" => effect with { Side = "p2" },
                "p2" => effect with { Side = "p1" },
                _ => effect
            }).ToArray());
        }

        if (command == "-weather")
        {
            string weather = arguments[0];
            if (arguments.Contains("[upkeep]"))
                return effects;

            var updated = effects.Where(effect => effect.Kind != FieldEffectKind.Weather).ToList();
            if (!weather.Equals("none", StringComparison.OrdinalIgnoreCase))
                updated.Add(new FieldEffect(weather, FieldEffectKind.Weather));
            return updated.AsReadOnly();
        }

        bool sideScoped = command is "-sidestart" or "-sideend";
        string? side = sideScoped ? arguments[0].Split(':', 2)[0] : null;
        string label = arguments[sideScoped ? 1 : 0];
        string name = label.StartsWith("move: ", StringComparison.OrdinalIgnoreCase)
            ? label["move: ".Length..]
            : label;

        var next = effects.ToList();
        int index = next.FindIndex(effect =>
            effect.Side == side && effect.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (command is "-fieldend" or "-sideend")
        {
            if (index < 0)
                return effects;

            next.RemoveAt(index);
        }
        else if (index < 0)
        {
            next.Add(new FieldEffect(name, Classify(name, sideScoped), side));
        }
        else if (name is "Spikes" or "Toxic Spikes")
        {
            FieldEffect existing = next[index];
            next[index] = existing with { Layers = existing.Layers + 1 };
        }
        else
        {
            return effects;
        }

        return next.AsReadOnly();
    }

    private static FieldEffectKind Classify(string name, bool sideScoped)
    {
        if (sideScoped)
        {
            return name is "Spikes" or "Toxic Spikes" or "Stealth Rock" or "Sticky Web" or "Steelsurge"
                ? FieldEffectKind.Hazard
                : FieldEffectKind.SideCondition;
        }

        if (name.EndsWith(" Terrain", StringComparison.OrdinalIgnoreCase))
            return FieldEffectKind.Terrain;
        if (name.EndsWith(" Room", StringComparison.OrdinalIgnoreCase))
            return FieldEffectKind.Room;
        return FieldEffectKind.OtherFieldCondition;
    }
}
