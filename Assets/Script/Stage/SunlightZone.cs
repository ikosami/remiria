using System.Collections.Generic;
using UnityEngine;

namespace Pulling
{
    public struct LightBeam
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    public class SunlightZone : MonoBehaviour
    {
        const int DirRight = 0;
        const int DirUp = 1;
        const int DirLeft = 2;
        const int DirDown = 3;
        const byte AxisVertical = 1;
        const byte AxisHorizontal = 2;

        static readonly int[] StepX = { 1, 0, -1, 0 };
        static readonly int[] StepY = { 0, 1, 0, -1 };

        static int _cachedHash;
        static int _cachedWidth;
        static int _cachedHeight;
        static bool _cacheValid;
        static byte[] _flags = System.Array.Empty<byte>();
        static bool[] _seen = System.Array.Empty<bool>();
        static readonly List<LightBeam> Beams = new List<LightBeam>();
        static readonly List<LightBeam> Runs = new List<LightBeam>();

        public static bool IsInSkyLight(GridMapBuilder builder, TileDatabase database, Vector3 world)
        {
            if (builder == null || builder.CellSize <= 0f)
            {
                return false;
            }

            int x = Mathf.FloorToInt(world.x / builder.CellSize);
            int y = Mathf.FloorToInt(world.y / builder.CellSize);
            if (x < 0 || x >= builder.Width)
            {
                return false;
            }

            if (y >= builder.Height)
            {
                return ColumnOpenToSky(builder, x);
            }

            return IsInSkyLight(builder, database, x, y);
        }

        public static bool IsInSkyLight(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            if (builder == null || !builder.InBounds(x, y) || !Ensure(builder, database))
            {
                return false;
            }

            return _flags[y * builder.Width + x] != 0;
        }

        public static bool IsExposedToSun(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            return IsInSkyLight(builder, database, x, y);
        }

        public static void CopyBeams(GridMapBuilder builder, TileDatabase database, List<LightBeam> destination)
        {
            destination.Clear();
            if (!Ensure(builder, database))
            {
                return;
            }

            for (int i = 0; i < Beams.Count; i++)
            {
                destination.Add(Beams[i]);
            }
        }

        public static bool ColumnOpenToSky(GridMapBuilder builder, int x)
        {
            int id = builder.GetCell(x, builder.Height - 1);
            return id == TileIds.Empty || id == TileIds.OneWay;
        }

        public static bool BlocksLight(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            if (builder.GetCell(x, y) == TileIds.OneWay)
            {
                return false;
            }

            return IsSolid(builder, database, x, y);
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

        static bool Ensure(GridMapBuilder builder, TileDatabase database)
        {
            if (builder == null || builder.Grid == null || builder.Width <= 0 || builder.Height <= 0)
            {
                _cacheValid = false;
                return false;
            }

            int hash = Hash(builder);
            if (_cacheValid && hash == _cachedHash && _cachedWidth == builder.Width && _cachedHeight == builder.Height)
            {
                return true;
            }

            Rebuild(builder, database, hash);
            return true;
        }

        static void Rebuild(GridMapBuilder builder, TileDatabase database, int hash)
        {
            int width = builder.Width;
            int height = builder.Height;
            int cells = width * height;
            if (_flags.Length != cells)
            {
                _flags = new byte[cells];
            }
            else
            {
                System.Array.Clear(_flags, 0, cells);
            }

            int seenLength = cells * 4;
            if (_seen.Length != seenLength)
            {
                _seen = new bool[seenLength];
            }
            else
            {
                System.Array.Clear(_seen, 0, seenLength);
            }

            for (int x = 0; x < width; x++)
            {
                int topId = builder.GetCell(x, height - 1);
                if (ColumnOpenToSky(builder, x) || topId == TileIds.MirrorNE || topId == TileIds.MirrorNW
                    || topId == TileIds.SwitchLight)
                {
                    Cast(builder, database, x, height - 1, DirDown);
                }
            }

            int[] grid = builder.Grid;
            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i] == TileIds.Sunlight)
                {
                    Cast(builder, database, i % width, i / width, DirDown);
                }
            }

            BuildBeams(width, height);
            _cachedHash = hash;
            _cachedWidth = width;
            _cachedHeight = height;
            _cacheValid = true;
        }

        static void Cast(GridMapBuilder builder, TileDatabase database, int x, int y, int dir)
        {
            int width = builder.Width;
            int height = builder.Height;
            int guard = width * height * 4;
            while (guard-- > 0)
            {
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                {
                    return;
                }

                int cell = y * width + x;
                int seenIndex = (cell << 2) | dir;
                if (_seen[seenIndex])
                {
                    return;
                }

                _seen[seenIndex] = true;
                int id = builder.GetCell(x, y);
                if (TryReflect(id, dir, out int outDir))
                {
                    _flags[cell] |= Axis(dir);
                    _flags[cell] |= Axis(outDir);
                    x += StepX[outDir];
                    y += StepY[outDir];
                    dir = outDir;
                    continue;
                }

                if (id == TileIds.SwitchLight)
                {
                    _flags[cell] |= Axis(dir);
                    return;
                }

                if (BlocksLight(builder, database, x, y))
                {
                    return;
                }

                _flags[cell] |= Axis(dir);
                x += StepX[dir];
                y += StepY[dir];
            }
        }

        static bool TryReflect(int id, int dir, out int outDir)
        {
            switch (id)
            {
                case TileIds.MirrorNE:
                    if (dir == DirDown) { outDir = DirLeft; return true; }
                    if (dir == DirRight) { outDir = DirUp; return true; }
                    break;
                case TileIds.MirrorNW:
                    if (dir == DirDown) { outDir = DirRight; return true; }
                    if (dir == DirLeft) { outDir = DirUp; return true; }
                    break;
                case TileIds.MirrorSE:
                    if (dir == DirUp) { outDir = DirLeft; return true; }
                    if (dir == DirRight) { outDir = DirDown; return true; }
                    break;
                case TileIds.MirrorSW:
                    if (dir == DirUp) { outDir = DirRight; return true; }
                    if (dir == DirLeft) { outDir = DirDown; return true; }
                    break;
            }

            outDir = dir;
            return false;
        }

        static byte Axis(int dir)
        {
            return dir == DirUp || dir == DirDown ? AxisVertical : AxisHorizontal;
        }

        static void BuildBeams(int width, int height)
        {
            Beams.Clear();
            CollectRuns(width, height, AxisVertical, true);
            SortRuns(true);
            MergeRuns(true);
            CollectRuns(width, height, AxisHorizontal, false);
            SortRuns(false);
            MergeRuns(false);
        }

        static void CollectRuns(int width, int height, byte axis, bool vertical)
        {
            Runs.Clear();
            if (vertical)
            {
                for (int x = 0; x < width; x++)
                {
                    int y = 0;
                    while (y < height)
                    {
                        if ((_flags[y * width + x] & axis) == 0)
                        {
                            y++;
                            continue;
                        }

                        int start = y;
                        y++;
                        while (y < height && (_flags[y * width + x] & axis) != 0)
                        {
                            y++;
                        }

                        Runs.Add(new LightBeam { x = x, y = start, width = 1, height = y - start });
                    }
                }

                return;
            }

            for (int y = 0; y < height; y++)
            {
                int x = 0;
                while (x < width)
                {
                    if ((_flags[y * width + x] & axis) == 0)
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    x++;
                    while (x < width && (_flags[y * width + x] & axis) != 0)
                    {
                        x++;
                    }

                    Runs.Add(new LightBeam { x = start, y = y, width = x - start, height = 1 });
                }
            }
        }

        static void SortRuns(bool vertical)
        {
            for (int i = 1; i < Runs.Count; i++)
            {
                LightBeam key = Runs[i];
                int j = i - 1;
                while (j >= 0 && CompareRun(Runs[j], key, vertical) > 0)
                {
                    Runs[j + 1] = Runs[j];
                    j--;
                }

                Runs[j + 1] = key;
            }
        }

        static int CompareRun(LightBeam a, LightBeam b, bool vertical)
        {
            if (vertical)
            {
                if (a.y != b.y) return a.y - b.y;
                if (a.height != b.height) return a.height - b.height;
                return a.x - b.x;
            }

            if (a.x != b.x) return a.x - b.x;
            if (a.width != b.width) return a.width - b.width;
            return a.y - b.y;
        }

        static void MergeRuns(bool vertical)
        {
            for (int i = 0; i < Runs.Count;)
            {
                LightBeam beam = Runs[i];
                int j = i + 1;
                if (vertical)
                {
                    while (j < Runs.Count
                           && Runs[j].y == beam.y
                           && Runs[j].height == beam.height
                           && Runs[j].x == beam.x + beam.width)
                    {
                        beam.width += Runs[j].width;
                        j++;
                    }
                }
                else
                {
                    while (j < Runs.Count
                           && Runs[j].x == beam.x
                           && Runs[j].width == beam.width
                           && Runs[j].y == beam.y + beam.height)
                    {
                        beam.height += Runs[j].height;
                        j++;
                    }
                }

                Beams.Add(beam);
                i = j;
            }
        }

        static int Hash(GridMapBuilder builder)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + builder.Width;
                hash = hash * 31 + builder.Height;
                int[] grid = builder.Grid;
                for (int i = 0; i < grid.Length; i++)
                {
                    hash = hash * 31 + grid[i];
                }

                return hash;
            }
        }
    }
}
