using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MythMayhem.Board;
using MythMayhem.UI;

namespace MythMayhem.Gameplay
{
    public class SummonController : MonoBehaviour
    {
        private const string InstructionMessage = "Select a card, then tap one of your lanes";

        [SerializeField] private BoardManager board;
        [SerializeField] private CardHandController hand;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CreatureFactory creatureFactory;
        [SerializeField] private float tapRadius = 200f;
        [SerializeField] private TurnManager turnManager;

        private readonly GameObject[] laneOccupants = new GameObject[BoardManager.LaneCount];
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private PointerEventData pointerData;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            SetStatus(InstructionMessage);
        }

        private void Update()
        {
            if (!TryGetTap(out Vector2 screenPosition))
                return;

            if (IsOverUI(screenPosition))
                return;

            int lane = FindPlayerLane(screenPosition);
            if (lane >= 0)
            {
                TrySummon(lane);
                return;
            }

            if (hand != null && hand.HasSelection && IsNearOpponentSpawn(screenPosition))
                SetStatus("You can only summon into your own lanes");
        }

        private bool TryGetTap(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool IsOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            if (pointerData == null)
                pointerData = new PointerEventData(EventSystem.current);

            pointerData.position = screenPosition;
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }

        private int FindPlayerLane(Vector2 screenPosition)
        {
            if (mainCamera == null || board == null)
                return -1;

            Transform[] spawns = board.PlayerSpawns;
            int bestLane = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < spawns.Length; i++)
            {
                float distance = ScreenDistance(screenPosition, spawns[i]);
                if (distance <= tapRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLane = i;
                }
            }

            return bestLane;
        }

        private bool IsNearOpponentSpawn(Vector2 screenPosition)
        {
            if (mainCamera == null || board == null)
                return false;

            Transform[] spawns = board.OpponentSpawns;
            for (int i = 0; i < spawns.Length; i++)
            {
                if (ScreenDistance(screenPosition, spawns[i]) <= tapRadius)
                    return true;
            }

            return false;
        }

        private float ScreenDistance(Vector2 screenPosition, Transform spawnPoint)
        {
            if (spawnPoint == null)
                return float.MaxValue;

            Vector3 projected = mainCamera.WorldToScreenPoint(spawnPoint.position);
            if (projected.z <= 0f)
                return float.MaxValue;

            return Vector2.Distance(screenPosition, new Vector2(projected.x, projected.y));
        }

        private void TrySummon(int lane)
        {
            if (turnManager != null && !turnManager.IsActive(CreatureSide.Player))
            {
                SetStatus("Wait for your turn");
                return;
            }

            if (hand == null || !hand.HasSelection)
            {
                SetStatus("Select a card first");
                return;
            }

            if (laneOccupants[lane] != null)
            {
                SetStatus("Lane " + (lane + 1) + " is already occupied");
                return;
            }

            Transform spawn = board.GetPlayerSpawn(lane);
            if (spawn == null || creatureFactory == null)
            {
                SetStatus("Cannot summon right now");
                return;
            }

            CreatureDefinition definition = hand.SelectedDefinition;
            int cost = definition != null ? definition.Cost : 0;

            if (turnManager != null && !turnManager.CanAfford(CreatureSide.Player, cost))
            {
                SetStatus("Not enough summon points");
                return;
            }

            GameObject summoned = creatureFactory.Create(spawn, lane, definition, CreatureSide.Player);
            if (summoned == null)
            {
                SetStatus("Cannot summon right now");
                return;
            }

            laneOccupants[lane] = summoned;
            hand.ConsumeSelected();

            if (turnManager != null)
                turnManager.Spend(CreatureSide.Player, cost);

            string creatureName = definition != null ? definition.DisplayName : "ASWANG";
            SetStatus("Summoned " + creatureName + " to lane " + (lane + 1));
        }

        public CreatureInstance GetLaneOccupant(int laneIndex)
        {
            GameObject occupant = laneOccupants[laneIndex];
            return occupant != null ? occupant.GetComponent<CreatureInstance>() : null;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
