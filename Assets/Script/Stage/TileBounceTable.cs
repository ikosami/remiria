namespace Pulling
{
    public static class TileBounceTable
    {
        public static float GetBounciness(int tileId)
        {
            switch (tileId)
            {
                case TileIds.Ground:
                    return PlayerTuning.LowBounciness;
                case TileIds.HighBounce:
                case TileIds.Breakable:
                case TileIds.BreakableStrong:
                case TileIds.Mirror:
                    return PlayerTuning.HighBounciness;
                case TileIds.OneWay:
                case TileIds.EnemyWeak:
                case TileIds.EnemyMid:
                case TileIds.EnemyBoss:
                    return PlayerTuning.LowBounciness;
                case TileIds.SlopeNE:
                case TileIds.SlopeNW:
                case TileIds.SlopeSE:
                case TileIds.SlopeSW:
                    return PlayerTuning.HighBounciness;
                default:
                    return PlayerTuning.Bounciness;
            }
        }
    }
}

