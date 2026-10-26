using UnityEngine;

namespace Pulling
{
    public class SunlightZone : MonoBehaviour
    {
        public static bool IsInSkyLight(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            if (builder == null || !builder.InBounds(x, y))
            {
                return false;
            }

            int top = builder.Height - 1;
            if (builder.GetCell(x, top) != TileIds.Empty)
            {
                return false;
            }

            for (int checkY = y; checkY <= top; checkY++)
            {
                if (IsSolid(builder, database, x, checkY))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsExposedToSun(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            if (builder == null)
            {
                return false;
            }

            for (int sunY = y; sunY < builder.Height; sunY++)
            {
                if (builder.GetCell(x, sunY) != TileIds.Sunlight)
                {
                    continue;
                }

                bool clear = true;
                for (int checkY = y; checkY < sunY; checkY++)
                {
                    if (IsSolid(builder, database, x, checkY))
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSolid(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            int id = builder.GetCell(x, y);
            if (id == TileIds.Empty || id == TileIds.Sunlight)
            {
                return false;
            }

            var def = database != null ? database.Get(id) : null;
            if (def != null && def.isTrigger)
            {
                return false;
            }

            return true;
        }
    }
}
