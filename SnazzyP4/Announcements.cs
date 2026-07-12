using System;
using System.Collections.Generic;

namespace SnazzyP4
{
    /// <summary>
    /// One announcement within an ordered-mode leaf, such as "Announce Gaze".
    /// It carries its enabled state, its position in the list and, optionally, one or more custom messages.
    /// </summary>
    public class AnnouncementSlot
    {
        /// <summary>The slot id, such as "gaze" or "inferno".</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Whether this announcement is sent when its trigger fires.</summary>
        public bool Enabled { get; set; }

        /// <summary>Whether the custom messages are used instead of the default message.</summary>
        public bool UseCustomMessage { get; set; }

        /// <summary>The custom messages, sent in order; empty entries are ignored.</summary>
        public List<string> Messages { get; set; } = new();

        /// <summary>
        /// Whether this is a user-added custom slot (as opposed to a built-in mechanic or the title).
        /// Custom slots have no default message, are never removed by <see cref="AnnouncementData.EnsureSlots"/>,
        /// and always fire regardless of which mechanic was pressed.
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// Personal Mode per-announcement channel override. Empty means "use the selected channel".
        /// Only consulted when per-channel announcements are enabled.
        /// </summary>
        public string Channel { get; set; } = string.Empty;
    }

    /// <summary>
    /// The announcement configuration for one (set, real/fake) combination.
    /// Ordered mode uses the slot list; simple mode uses the multi-line text.
    /// </summary>
    public class AnnouncementLeaf
    {
        /// <summary>The ordered, reorderable announcement slots.</summary>
        public List<AnnouncementSlot> Slots { get; set; } = new();

        /// <summary>The simple-mode text, one chat message per non-empty line.</summary>
        public string SimpleText { get; set; } = string.Empty;
    }

    /// <summary>
    /// The announcement configuration for one category (Exdeath or Chaos), split into first/second set and real/fake.
    /// </summary>
    public class AnnouncementCategory
    {
        /// <summary>Whether the category uses ordered-list mode; otherwise simple text-box mode.</summary>
        public bool Ordered { get; set; } = true;

        /// <summary>The first set, real branch.</summary>
        public AnnouncementLeaf FirstReal { get; set; } = new();

        /// <summary>The first set, fake branch.</summary>
        public AnnouncementLeaf FirstFake { get; set; } = new();

        /// <summary>The second set, real branch.</summary>
        public AnnouncementLeaf SecondReal { get; set; } = new();

        /// <summary>The second set, fake branch.</summary>
        public AnnouncementLeaf SecondFake { get; set; } = new();

        /// <summary>
        /// Returns the leaf for a set and real/fake combination.
        /// </summary>
        public AnnouncementLeaf GetLeaf(bool isFirst, bool isReal)
        {
            if (isFirst)
            {
                return isReal ? FirstReal : FirstFake;
            }

            return isReal ? SecondReal : SecondFake;
        }
    }

    /// <summary>
    /// The complete announcement configuration for one chat channel.
    /// </summary>
    public class ChannelAnnouncements
    {
        /// <summary>The Exdeath announcements.</summary>
        public AnnouncementCategory Exdeath { get; set; } = new();

        /// <summary>The Chaos announcements.</summary>
        public AnnouncementCategory Chaos { get; set; } = new();
    }

    /// <summary>
    /// Shared data for the announcement system: the slot ids per category, their labels and the generated default messages.
    /// </summary>
    public static class AnnouncementData
    {
        /// <summary>The ordered Exdeath announcement slot ids (the built-in title plus the four mechanics).</summary>
        public static readonly string[] ExdeathSlots = { "title", "gaze", "spread", "drop", "accel" };

        /// <summary>The ordered Chaos announcement slot ids for the first set, which always resolves Inferno.</summary>
        public static readonly string[] ChaosFirstSlots = { "title", "inferno" };

        /// <summary>The ordered Chaos announcement slot ids for the second set, which always resolves Tsunami.</summary>
        public static readonly string[] ChaosSecondSlots = { "title", "tsunami" };

        /// <summary>
        /// Returns the ordered slot ids for a category and set. Chaos is static, so the first set is Inferno and the second is Tsunami.
        /// </summary>
        public static string[] SlotIdsFor(string categoryId, bool isFirst)
        {
            if (categoryId == "chaos")
            {
                return isFirst ? ChaosFirstSlots : ChaosSecondSlots;
            }

            return ExdeathSlots;
        }

        /// <summary>
        /// Whether a slot is safe to broadcast to party chat: the gaze and the two chaos callouts.
        /// Party Mode sends only these; Personal Mode blocks everything else from party chat unless overridden.
        /// </summary>
        public static bool IsPartySafe(string slotId)
        {
            return slotId is "gaze" or "inferno" or "tsunami";
        }

        /// <summary>
        /// Returns the display label for a slot id.
        /// </summary>
        public static string SlotLabel(string id)
        {
            return id switch
            {
                "title" => "Title",
                "gaze" => "Gaze",
                "spread" => "Spread",
                "drop" => "Water Drop",
                "accel" => "Acceleration",
                "inferno" => "Inferno",
                "tsunami" => "Tsunami",
                _ => "Custom message",
            };
        }

        /// <summary>
        /// Creates a new, empty user-added custom slot with a unique id.
        /// </summary>
        public static AnnouncementSlot NewCustomSlot()
        {
            return new()
            {
                Id = "custom_" + Guid.NewGuid().ToString("N"),
                IsCustom = true,
                UseCustomMessage = true,
                Messages = new List<string> { string.Empty },
            };
        }

        /// <summary>
        /// Returns the generated default message for a slot in a given set and real/fake branch.
        /// When <paramref name="includeSetNumber"/> is false, the "[1st]"/"[2nd]" prefix is dropped from Exdeath debuff messages.
        /// </summary>
        public static string DefaultMessage(string categoryId, string slotId, bool isFirst, bool isReal, bool includeSetNumber = true)
        {
            var set = isFirst ? "1st" : "2nd";
            if (slotId == "title")
            {
                // Exdeath set titles include the real/fake state; chaos sets are static (Inferno first, Tsunami second) so the title names the mechanic.
                return categoryId == "exdeath"
                    ? $"---------- {set} Set : {(isReal ? "REAL" : "FAKE")} ----------"
                    : (isFirst ? "---------- Inferno ----------" : "---------- Tsunami ----------");
            }

            // Format: "[set] Debuff - Resolvement", for example "[1st] Lightning - Spread". The set prefix is optional.
            if (categoryId == "exdeath")
            {
                var prefix = includeSetNumber ? $"[{set}] " : string.Empty;
                return slotId switch
                {
                    "gaze" => isReal ? $"{prefix}Gaze - Look Away" : $"{prefix}Gaze - Look",
                    "spread" => isReal ? $"{prefix}Lightning - Spread" : $"{prefix}Lightning - Stack",
                    "drop" => isReal ? $"{prefix}Water Drop - Stack" : $"{prefix}Water Drop - Spread",
                    "accel" => isReal ? $"{prefix}Acceleration - Stand Still" : $"{prefix}Acceleration - Move",
                    _ => string.Empty,
                };
            }

            return slotId switch
            {
                "inferno" => isReal ? "Inferno - Twister (MOVE)" : "Inferno - Donut (STAY)",
                "tsunami" => isReal ? "Tsunami - Donut (STAY)" : "Tsunami - Twister (MOVE)",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Ensures a leaf contains exactly the slots for its category, adding any that are missing and removing any that no longer apply, while preserving the existing order and state.
        /// </summary>
        public static void EnsureSlots(AnnouncementLeaf leaf, string[] slotIds)
        {
            for (var canonicalIndex = 0; canonicalIndex < slotIds.Length; canonicalIndex++)
            {
                var id = slotIds[canonicalIndex];
                var exists = false;
                foreach (var slot in leaf.Slots)
                {
                    if (slot.Id == id)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    continue;
                }

                // Insert a missing built-in slot ahead of the first existing built-in with a higher
                // canonical index, so newly added slots (such as the title) land in their natural order
                // without disturbing custom slots or the user's own reordering.
                var insertAt = leaf.Slots.Count;
                for (var slotIndex = 0; slotIndex < leaf.Slots.Count; slotIndex++)
                {
                    var existingCanonicalIndex = Array.IndexOf(slotIds, leaf.Slots[slotIndex].Id);
                    if (existingCanonicalIndex > canonicalIndex)
                    {
                        insertAt = slotIndex;
                        break;
                    }
                }

                // New built-in slots start enabled, except the title, so a fresh setup announces everything but the titles.
                leaf.Slots.Insert(insertAt, new AnnouncementSlot { Id = id, Enabled = id != "title" });
            }

            // Remove only stale built-in slots; user-added custom slots are always kept.
            leaf.Slots.RemoveAll(slot => !slot.IsCustom && Array.IndexOf(slotIds, slot.Id) < 0);
        }
    }
}
