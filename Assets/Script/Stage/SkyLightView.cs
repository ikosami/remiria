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
        readonly List<LightBeam> _spans = new List<LightBeam>();
        Material _material;
        Sprite _quad;
        bool _missingShaderWarned;
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
            if (_builder != null && _builder.Grid != null && _builder.Height > 0 && EnsureMaterial())
            {
                _material.SetFloat("_HeightMin", 0f);
                _material.SetFloat("_HeightMax", _builder.Height * _builder.CellSize);
                _material.SetFloat("_BottomAlpha", 0.18f);
                SunlightZone.CopyBeams(_builder, _database, _spans);
                for (int i = 0; i < _spans.Count; i++)
                {
                    Place(used, _spans[i]);
                    used++;
                }
            }
            else if (_builder != null && _builder.Grid != null && _builder.Height > 0 && !_missingShaderWarned)
            {
                _missingShaderWarned = true;
                Debug.LogWarning("太陽光シェーダーが見つかりません。");
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
                int[] grid = _builder.Grid;
                for (int i = 0; i < grid.Length; i++)
                {
                    hash = hash * 31 + grid[i];
                }

                signature = hash;
            }

            return true;
        }

        void Place(int index, LightBeam beam)
        {
            var renderer = GetBeam(index);
            float cell = _builder.CellSize;
            renderer.transform.localScale = new Vector3(beam.width * cell, beam.height * cell, 1f);
            renderer.transform.position = new Vector3(beam.x * cell, (beam.y + beam.height) * cell, 0f);
        }

        bool EnsureMaterial()
        {
            if (_material != null)
            {
                return true;
            }

            Shader shader = Resources.Load<Shader>("SkyLightGradient");
            if (shader == null)
            {
                shader = Shader.Find("Pulling/SkyLightGradient");
            }

            if (shader == null)
            {
                return false;
            }

            _material = new Material(shader)
            {
                name = "SkyLightGradient (runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }

        Sprite QuadSprite()
        {
            if (_quad != null)
            {
                return _quad;
            }

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "SkyLightQuad",
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _quad = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 1f), 4f);
            _quad.name = "SkyLightQuad";
            _quad.hideFlags = HideFlags.HideAndDontSave;
            return _quad;
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
            renderer.sharedMaterial = _material;
            renderer.sprite = QuadSprite();
            renderer.color = Color.white;
            return renderer;
        }
    }
}
