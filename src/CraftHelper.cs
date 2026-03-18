using Il2Cpp;
using UnityEngine;

namespace TimeShift
{
    // Finds the currently active craft in the scene
    public static class CraftHelper
    {
        public static Craft GetActiveCraft()
        {
            // Try the static property first
            try
            {
                var active = Craft.active;
                if (active != null) return active;
            }
            catch { }

            // Fallback to searching the scene
            var crafts = Object.FindObjectsOfType<Craft>();
            return crafts != null && crafts.Length > 0 ? crafts[0] : null;
        }
    }
}
