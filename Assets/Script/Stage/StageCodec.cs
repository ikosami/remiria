using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Pulling
{
    public static class StageCodec
    {
        public static int[] Decode(StageAsset asset)
        {
            if (asset == null)
            {
                return Array.Empty<int>();
            }

            int width = Mathf.Max(1, asset.width);
            int height = Mathf.Max(1, asset.height);
            var grid = new int[width * height];

            if (asset.rows == null)
            {
                return grid;
            }

            int rowsToRead = Mathf.Min(height, asset.rows.Length);
            for (int y = 0; y < rowsToRead; y++)
            {
                DecodeRow(asset.rows[y], grid, y * width, width);
            }

            return grid;
        }

        public static string[] Encode(int[] grid, int width, int height)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var rows = new string[height];
            for (int y = 0; y < height; y++)
            {
                rows[y] = EncodeRow(grid, y * width, width);
            }

            return rows;
        }

        public static string EncodeRow(int[] grid, int start, int width)
        {
            if (width <= 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            int current = grid[start];
            int count = 1;

            for (int x = 1; x < width; x++)
            {
                int id = grid[start + x];
                if (id == current)
                {
                    count++;
                    continue;
                }

                AppendToken(sb, current, count);
                current = id;
                count = 1;
            }

            AppendToken(sb, current, count);
            return sb.ToString();
        }

        public static void DecodeRow(string row, int[] grid, int start, int width)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                return;
            }

            int x = 0;
            var tokens = row.Split(',');
            for (int i = 0; i < tokens.Length && x < width; i++)
            {
                string token = tokens[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                ParseToken(token, out int id, out int count);
                count = Mathf.Max(1, count);
                for (int c = 0; c < count && x < width; c++)
                {
                    grid[start + x] = id;
                    x++;
                }
            }
        }

        static void AppendToken(StringBuilder sb, int id, int count)
        {
            if (sb.Length > 0)
            {
                sb.Append(',');
            }

            sb.Append(id.ToString(CultureInfo.InvariantCulture));
            if (count > 1)
            {
                sb.Append(':');
                sb.Append(count.ToString(CultureInfo.InvariantCulture));
            }
        }

        static void ParseToken(string token, out int id, out int count)
        {
            int colon = token.IndexOf(':');
            if (colon < 0)
            {
                id = int.Parse(token, CultureInfo.InvariantCulture);
                count = 1;
                return;
            }

            id = int.Parse(token.Substring(0, colon), CultureInfo.InvariantCulture);
            count = int.Parse(token.Substring(colon + 1), CultureInfo.InvariantCulture);
        }
    }
}
