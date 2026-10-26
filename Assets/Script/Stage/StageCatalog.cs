using System.Collections.Generic;
using UnityEngine;

namespace Pulling
{
    [CreateAssetMenu(fileName = "StageCatalog", menuName = "Pulling/Stage Catalog")]
    public class StageCatalog : ScriptableObject
    {
        public List<StageAsset> stages = new List<StageAsset>();

        public int Count => stages != null ? stages.Count : 0;

        public StageAsset Get(int index)
        {
            if (stages == null || stages.Count == 0)
            {
                return null;
            }

            int wrapped = ((index % stages.Count) + stages.Count) % stages.Count;
            return stages[wrapped];
        }

        public int IndexOf(StageAsset stage)
        {
            if (stages == null || stage == null)
            {
                return -1;
            }

            return stages.IndexOf(stage);
        }

        public int IndexOfId(string id)
        {
            if (stages == null || string.IsNullOrEmpty(id))
            {
                return -1;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && stages[i].id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
