namespace Game
{
    using UnityEngine;

    // Defines measurements for the hex grid
    public static class HexMetrics
    {
        // Distance from center to hex corner
        public static float outerRadius = 4f;
        // Distance from center to flat edge
        public static float innerRadius = outerRadius * Mathf.Sqrt(3f) / 2f;
    }
}