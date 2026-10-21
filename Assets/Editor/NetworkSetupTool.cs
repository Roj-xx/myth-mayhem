using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using MythMayhem.Board;
using MythMayhem.Gameplay;
using MythMayhem.Net;

namespace MythMayhem.EditorTools
{
    // Adds the networking components to the battle scene's GameDirector and wires
    // their references. Safe to run more than once; existing components are reused.
    public static class NetworkSetupTool
    {
        private const string BattleScenePath = "Assets/Scenes/PlayWithAI.unity";

        public static void SetupBattleScene()
        {
            Scene scene = MythMayhemUi.OpenForScopedEditing(BattleScenePath, BattleScenePath);
            try
            {
                GameObject director = FindInScene(scene, "GameDirector");
                if (director == null)
                    throw new Exception("GameDirector was not found in " + BattleScenePath);

                if (director.GetComponent<NetworkObject>() == null)
                    director.AddComponent<NetworkObject>();

                BoardView boardView = director.GetComponent<BoardView>();
                if (boardView == null)
                    boardView = director.AddComponent<BoardView>();

                MatchSession session = director.GetComponent<MatchSession>();
                if (session == null)
                    session = director.AddComponent<MatchSession>();

                BoardManager board = FindComponentInScene<BoardManager>(scene);
                DeckController playerDeck = FindInScene(scene, "PlayerDeck")?.GetComponent<DeckController>();
                DeckController opponentDeck = FindInScene(scene, "OpponentDeck")?.GetComponent<DeckController>();
                Camera camera = FindComponentInScene<Camera>(scene);

                WireSession(session, director, board, boardView, playerDeck, opponentDeck, camera);
                WireBoardView(boardView, director);

                EditorUtility.SetDirty(director);
                EditorUtility.SetDirty(session);
                EditorUtility.SetDirty(boardView);
                EditorSceneManager.MarkSceneDirty(scene);
                MythMayhemUi.FinishScopedScene(scene, BattleScenePath);

                Debug.Log("[Myth Mayhem] Networked battle scene wired. Rebuild a player and test Create/Join.");
            }
            catch (Exception)
            {
                MythMayhemUi.DiscardScopedScene(scene);
                throw;
            }
        }

        private static void WireSession(MatchSession session, GameObject director, BoardManager board, BoardView boardView, DeckController playerDeck, DeckController opponentDeck, Camera camera)
        {
            SerializedObject serialized = new SerializedObject(session);
            serialized.FindProperty("turnManager").objectReferenceValue = director.GetComponent<TurnManager>();
            serialized.FindProperty("matchController").objectReferenceValue = director.GetComponent<MatchController>();
            serialized.FindProperty("cardPlay").objectReferenceValue = director.GetComponent<CardPlayController>();
            serialized.FindProperty("combat").objectReferenceValue = director.GetComponent<CombatController>();
            serialized.FindProperty("board").objectReferenceValue = board;
            serialized.FindProperty("mainCamera").objectReferenceValue = camera;
            serialized.FindProperty("playerDeck").objectReferenceValue = playerDeck;
            serialized.FindProperty("opponentDeck").objectReferenceValue = opponentDeck;
            serialized.FindProperty("boardView").objectReferenceValue = boardView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireBoardView(BoardView boardView, GameObject director)
        {
            SerializedObject serialized = new SerializedObject(boardView);
            serialized.FindProperty("cardPlay").objectReferenceValue = director.GetComponent<CardPlayController>();
            serialized.FindProperty("laneCount").intValue = BoardManager.LaneCount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;

                Transform found = FindChildRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;

                Transform nested = FindChildRecursive(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
