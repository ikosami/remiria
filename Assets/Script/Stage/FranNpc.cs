using UnityEngine;

namespace Pulling
{
    public class FranNpc : MonoBehaviour
    {
        GridMapBuilder _builder;
        TileDatabase _database;
        int _x;
        int _y;
        bool _destroyed;

        public void Bind(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            _builder = builder;
            _database = database;
            _x = x;
            _y = y;
        }

        void Update()
        {
            if (_destroyed || _builder == null)
            {
                return;
            }

            if (!SunlightZone.IsExposedToSun(_builder, _database, _x, _y))
            {
                return;
            }

            _destroyed = true;
            _builder.SetCell(_x, _y, TileIds.Empty);
        }
    }
}

