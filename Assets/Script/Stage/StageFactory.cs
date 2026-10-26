using UnityEngine;

namespace Pulling
{
    public static class StageFactory
    {
        public static StageAsset CreateDefault()
        {
            var asset = ScriptableObject.CreateInstance<StageAsset>();
            asset.name = "Stage_1";
            asset.version = 1;
            asset.id = "Stage_1";
            asset.width = 48;
            asset.height = 12;
            asset.cellSize = 1f;
            asset.cameraVisibleRows = 12;
            asset.spawnX = 2;
            asset.spawnY = 1;

            var grid = new int[asset.width * asset.height];
            for (int x = 0; x < asset.width; x++)
            {
                bool pit = x >= 16 && x <= 20;
                grid[x] = pit ? TileIds.Hazard : TileIds.Ground;
            }

            for (int y = 1; y <= 2; y++)
            {
                grid[y * asset.width + 10] = TileIds.Breakable;
            }

            for (int x = 14; x <= 24; x++)
            {
                grid[4 * asset.width + x] = TileIds.Ground;
            }

            for (int x = 30; x <= 34; x++)
            {
                grid[2 * asset.width + x] = TileIds.Ground;
            }

            grid[asset.width + (asset.width - 3)] = TileIds.Goal;
            asset.rows = StageCodec.Encode(grid, asset.width, asset.height);
            return asset;
        }

        public static StageAsset CreateBlank(string id)
        {
            string stageId = string.IsNullOrWhiteSpace(id) ? "Stage" : id.Trim();
            var asset = ScriptableObject.CreateInstance<StageAsset>();
            asset.version = 1;
            asset.name = stageId;
            asset.id = stageId;
            asset.width = 48;
            asset.height = 12;
            asset.cellSize = 1f;
            asset.cameraVisibleRows = 12;
            asset.spawnX = 2;
            asset.spawnY = 1;

            var grid = new int[asset.width * asset.height];
            for (int x = 0; x < asset.width; x++)
            {
                grid[x] = TileIds.Ground;
            }

            grid[asset.width + (asset.width - 3)] = TileIds.Goal;
            asset.rows = StageCodec.Encode(grid, asset.width, asset.height);
            return asset;
        }
    }
}
