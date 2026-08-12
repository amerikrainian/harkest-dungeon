using Assets.Code.Buff;
using Assets.Code.Duration;

namespace DD2A11y.Game {
    /// <summary>Per-buff description text honoring the game's "buff_tooltip_&lt;id&gt;_override"
    /// strings with the same precedence its tooltip composers apply. Some buffs are named only
    /// by that override (the Weapon Rack's positive-token immunity is "Cannot Gain Positive
    /// Tokens"; its stat carries no formatted string at all), and the plain describers hand
    /// the raw stat loc key to speech for those.</summary>
    public static class BuffText {
        public static string Description(BuffInstance buff) {
            var definition = buff.Definition;
            string overrideText = Override(definition);
            if (overrideText == null) {
                return BuffDescription.GetDescriptionWithDuration(buff);
            }
            // The game's list composer appends the running duration to an override the same way.
            if (definition.GetHasDuration()
                && definition.DurationType.m_DurationDisplayType != DurationDisplayType.None) {
                string duration = DurationDescription.GetDurationText(
                    definition.DurationType, buff.GetDurationAmount());
                if (!string.IsNullOrEmpty(duration)) {
                    return overrideText + " (" + duration + ")";
                }
            }
            return overrideText;
        }

        public static string Description(BuffDefinition definition)
            => Override(definition)
               ?? BuffDescription.GetDescription(definition, addLineOnActorDataEffect: false);

        private static string Override(BuffDefinition definition)
            => GameLoc.TryGet("buff_tooltip_" + definition.m_Id + "_override");
    }
}
