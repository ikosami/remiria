using UnityEngine;

namespace Pulling
{
    public static class BounceResolver
    {
        public static float GetBounciness(Collider2D col, Vector2 worldPoint, GridMapBuilder builder)
        {
            if (col != null)
            {
                var surface = col.GetComponent<BounceSurface>();
                if (surface == null)
                {
                    surface = col.GetComponentInParent<BounceSurface>();
                }

                if (surface != null)
                {
                    return surface.bounciness;
                }
            }

            if (builder != null && builder.TryWorldToCell(worldPoint, out int x, out int y))
            {
                int id = builder.GetCell(x, y);
                if (id != TileIds.Empty)
                {
                    return TileBounceTable.GetBounciness(id);
                }
            }

            return PlayerTuning.Bounciness;
        }
    }
}

