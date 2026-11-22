namespace Pulling
{
    public static class TileBounceTable
    {
        public static float GetBounciness(int tileId)
        {
            switch (tileId)
            {
                case TileIds.Ground:
                case TileIds.Door:
                    return PlayerTuning.LowBounciness;
                case TileIds.Switch:
                case TileIds.SwitchToggle:
                    return 0f;
                case TileIds.HighBounce:
                case TileIds.Breakable:
                case TileIds.BreakableStrong:
                case TileIds.Bomb:
                    return PlayerTuning.HighBounciness;
                case TileIds.BlastWall:
                    return PlayerTuning.LowBounciness;
                case TileIds.OneWay:
                case TileIds.EnemyWeak:
                case TileIds.EnemyMid:
                case TileIds.EnemyHeavy:
                case TileIds.EnemyBoss:
                    return PlayerTuning.LowBounciness;
                case TileIds.SlopeNE:
                case TileIds.SlopeNW:
                case TileIds.SlopeSE:
                case TileIds.SlopeSW:
                case TileIds.MirrorNE:
                case TileIds.MirrorNW:
                case TileIds.MirrorSE:
                case TileIds.MirrorSW:
                    return PlayerTuning.HighBounciness;
                default:
                    return PlayerTuning.Bounciness;
            }
        }
    }
}

