using CitizenFX.Core;
using System;

namespace DensitySuppression.Client
{
    internal static class Vector3Extensions
    {
        internal static float DistanceToPlayer(this Vector3 pos1, Vector3 pos2)
        {
            return (float)Math.Sqrt((double)pos1.DistanceToSquared(pos2));
        }
    }
}
