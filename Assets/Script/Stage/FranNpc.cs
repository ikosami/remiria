using UnityEngine;

namespace Pulling
{
    public class FranNpc : MonoBehaviour
    {
        GridMapBuilder _builder;
        TileDatabase _database;
        string _stageId;
        int _x;
        int _y;
        bool _gone;

        public void Bind(GridMapBuilder builder, TileDatabase database, int x, int y)
        {
            _builder = builder;
            _database = database;
            _stageId = builder != null ? builder.StageId : null;
            _x = x;
            _y = y;

            if (AlreadyCollected())
            {
                Hide();
            }
        }

        void Update()
        {
            if (_gone || _builder == null || _builder.IsEditing)
            {
                return;
            }

            if (!SunlightZone.IsInSkyLight(_builder, _database, _x, _y)
                && !SunlightZone.IsExposedToSun(_builder, _database, _x, _y))
            {
                return;
            }

            Hide();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_gone || _builder == null || _builder.IsEditing)
            {
                return;
            }

            if (other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            FranRecords.MarkCollected(_stageId);
            Hide();
        }

        bool AlreadyCollected()
        {
            return _builder != null && !_builder.IsEditing && FranRecords.IsCollected(_stageId);
        }

        void Hide()
        {
            if (_gone)
            {
                return;
            }

            _gone = true;
            var colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}
