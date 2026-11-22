using UnityEngine;

namespace Pulling
{
    public static class FranRecords
    {
        const string Prefix = "pulling.fran.";

        public static bool IsCollected(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                return false;
            }

            return PlayerPrefs.GetInt(Prefix + stageId, 0) != 0;
        }

        public static void MarkCollected(string stageId)
        {
            if (string.IsNullOrEmpty(stageId) || IsCollected(stageId))
            {
                return;
            }

            PlayerPrefs.SetInt(Prefix + stageId, 1);
            PlayerPrefs.Save();
        }

        public static void Move(string fromId, string toId)
        {
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId) || fromId == toId || !IsCollected(fromId))
            {
                return;
            }

            PlayerPrefs.DeleteKey(Prefix + fromId);
            PlayerPrefs.SetInt(Prefix + toId, 1);
            PlayerPrefs.Save();
        }
    }
}
