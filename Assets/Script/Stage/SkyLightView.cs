using System.Collections.Generic;
using UnityEngine;

namespace Pulling
{
    public class SkyLightView : MonoBehaviour
    {
        public Sprite beamSprite;

        GridMapBuilder _builder;
        TileDatabase _database;
        readonly List<SpriteRenderer> _beams = new List<SpriteRenderer>();
        bool _missingSpriteWarned;
        bool _dirty;
        bool _hasApplied;
        int _appliedSignature;

        public void Bind(GridMapBuilder builder, TileDatabase database)
        {
            _builder = builder;
            _database = database;
            builder?.BindSkyLight(this);
            if (builder != null && builder.Grid != null)
            {
                Rebuild();
            }
        }

        public void Rebuild()
        {
            if (Time.inFixedTimeStep)
            {
                _dirty = true;
                return;
            }

            Apply();
        }

        void LateUpdate()
        {
            if (!_dirty && !LayoutChanged())
            {
                return;
            }

            _dirty = false;
            Apply();
        }

        void Apply()
        {
            int used = 0;
            if (_builder != null && _builder.Grid != null && beamSprite != null && _builder.Height > 0)
            {
                int top = _builder.Height - 1;
                int x = 0;
                while (x < _builder.Width)
                {
                    int depth = ColumnDepth(x, top);
                    if (depth <= 0)
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    x++;
                    while (x < _builder.Width && ColumnDepth(x, top) == depth)
                    {
                        x++;
                    }

                    Place(used, start, x - start, depth);
                    used++;
                }
            }
            else if (beamSprite == null && !_missingSpriteWarned)
            {
                _missingSpriteWarned = true;
                Debug.LogWarning("光の画像が未設定です。Pulling/Build Game Scene を実行してください。");
            }

            for (int i = used; i < _beams.Count; i++)
            {
                if (_beams[i] != null)
                {
                    _beams[i].gameObject.SetActive(false);
                }
            }

            _hasApplied = TrySignature(out _appliedSignature);
        }

        bool LayoutChanged()
        {
            if (!TrySignature(out int signature))
            {
                return _hasApplied;
            }

            return !_hasApplied || signature != _appliedSignature;
        }

        bool TrySignature(out int signature)
        {
            signature = 0;
            if (_builder == null || _builder.Grid == null)
            {
                return false;
            }

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _builder.Width;
                hash = hash * 31 + _builder.Height;
                hash = hash * 31 + _builder.CellSize.GetHashCode();
                hash = hash * 31 + (beamSprite != null ? beamSprite.GetInstanceID() : 0);
                int[] grid = _builder.Grid;
                for (int i = 0; i < grid.Length; i++)
                {
                    hash = hash * 31 + grid[i];
                }

                signature = hash;
            }

            return true;
        }

        int ColumnDepth(int x, int top)
        {
            if (_builder.GetCell(x, top) != TileIds.Empty)
            {
                return 0;
            }

            int depth = 0;
            for (int y = top; y >= 0; y--)
            {
                if (SunlightZone.IsSolid(_builder, _database, x, y))
                {
                    break;
                }

                depth++;
            }

            return depth;
        }

        void Place(int index, int startX, int span, int depth)
        {
            var renderer = GetBeam(index);
            float cell = _builder.CellSize;
            float width = span * cell;
            float height = depth * cell;
            var rect = beamSprite.rect;
            float ppu = beamSprite.pixelsPerUnit > 0f ? beamSprite.pixelsPerUnit : 100f;
            float localWidth = rect.width / ppu;
            float localHeight = rect.height / ppu;
            float scaleX = localWidth > 0f ? width / localWidth : 1f;
            float scaleY = localHeight > 0f ? height / localHeight : 1f;
            renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            Vector2 pivot = beamSprite.pivot;
            float left = -pivot.x / ppu;
            float topOffset = (rect.height - pivot.y) / ppu;
            renderer.transform.position = new Vector3(
                startX * cell - left * scaleX,
                _builder.Height * cell - topOffset * scaleY,
                0f);
        }

        SpriteRenderer GetBeam(int index)
        {
            while (_beams.Count <= index)
            {
                var go = new GameObject("Beam");
                go.transform.SetParent(transform, false);
                var created = go.AddComponent<SpriteRenderer>();
                created.sortingOrder = -1;
                _beams.Add(created);
            }

            var renderer = _beams[index];
            renderer.gameObject.SetActive(true);
            renderer.sprite = beamSprite;
            renderer.color = Color.white;
            return renderer;
        }
    }
}
