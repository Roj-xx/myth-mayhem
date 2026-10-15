using UnityEngine;

namespace MythMayhem.Board
{
    public class BoardManager : MonoBehaviour
    {
        public const int LaneCount = 3;

        [SerializeField] private Transform[] playerSpawns = new Transform[LaneCount];
        [SerializeField] private Transform[] opponentSpawns = new Transform[LaneCount];
        [SerializeField] private Transform[] playerBuildingSpawns = new Transform[LaneCount];
        [SerializeField] private Transform[] opponentBuildingSpawns = new Transform[LaneCount];

        public Transform[] PlayerSpawns => playerSpawns;
        public Transform[] OpponentSpawns => opponentSpawns;
        public Transform[] PlayerBuildingSpawns => playerBuildingSpawns;
        public Transform[] OpponentBuildingSpawns => opponentBuildingSpawns;

        public Transform GetPlayerSpawn(int laneIndex)
        {
            return playerSpawns[laneIndex];
        }

        public Transform GetOpponentSpawn(int laneIndex)
        {
            return opponentSpawns[laneIndex];
        }

        public Transform GetPlayerBuildingSpawn(int laneIndex)
        {
            return AtIndex(playerBuildingSpawns, laneIndex);
        }

        public Transform GetOpponentBuildingSpawn(int laneIndex)
        {
            return AtIndex(opponentBuildingSpawns, laneIndex);
        }

        private static Transform AtIndex(Transform[] array, int laneIndex)
        {
            if (array == null || laneIndex < 0 || laneIndex >= array.Length)
                return null;

            return array[laneIndex];
        }
    }
}
