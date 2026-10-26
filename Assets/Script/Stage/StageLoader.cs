namespace Pulling
{
    public static class StageLoader
    {
        public static int[] LoadGrid(StageAsset asset)
        {
            return StageCodec.Decode(asset);
        }
    }
}
