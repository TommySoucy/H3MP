using UnityEngine;

namespace H3MP.Networking
{
    public class Player
    {
        public int ID;
        public string username;

        // State vars
        public int IFF;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 torsoPos;
        public Quaternion torsoRot;
        public Vector3 leftHandPos;
        public Quaternion leftHandRot;
        public Vector3 rightHandPos;
        public Quaternion rightHandRot;
        public float health;
        public int maxHealth;

        public string scene;
        public int instance;
        public bool firstInSceneInstance;
        public int colorIndex;
        public string playerPrefabID;

        public Player(int ID, string username, Vector3 spawnPos, int IFF, int colorIndex)
        {
            this.ID = ID;
            this.username = username;
            this.position = spawnPos;
            this.rotation = Quaternion.identity;
            this.IFF = IFF;
            this.colorIndex = colorIndex;
        }
    }
}
