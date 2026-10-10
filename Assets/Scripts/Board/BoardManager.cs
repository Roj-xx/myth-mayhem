using UnityEngine;

namespace MythMayhem.Board
{
    public class BoardManager : MonoBehaviour
    {
        public const int LaneCount = 3;

        [SerializeField] private Transform[] playerSpawns = new Transform[LaneCount];
        [SerializeField] private Transform[] opponentSpawns = new Transform[LaneCount];

        public Transform[] PlayerSpawns => playerSpawns;
        public Transform[] OpponentSpawns => opponentSpawns;

        public Transform GetPlayerSpawn(int laneIndex)
        {
            return playerSpawns[laneIndex];
        }

        public Transform GetOpponentSpawn(int laneIndex)
        {
            return opponentSpawns[laneIndex];
        }
    }
}
