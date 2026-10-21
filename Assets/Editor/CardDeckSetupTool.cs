using System.Collections.Generic;
using MythMayhem.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.EditorTools
{
    // The creature/building setup tools create the prefabs and definitions, but the
    // scene's deck lists are hand-authored, so a freshly added card never reaches the
    // player's hand. Bakunawa and Sagrado ended up only in the AI deck, which is why
    // the player could never draw them.
    //
    // This tops the PlayerDeck up with every card definition found under Assets/Data/Cards
    // (creatures, supports and buildings). Safe to re-run: existing entries are kept and
    // only missing ones are appended, so it stays idempotent.
    public static class CardDeckSetupTool
    {
        private const string BattleScenePath = "Assets/Scenes/PlayWithAI.unity";
        private const string CardsRoot = "Assets/Data/Cards";
        private const string PlayerDeckName = "PlayerDeck";

        public static void SetupCardDecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Decks] Exit play mode before topping up the deck.");
                return;
            }

            Scene scene = MythMayhemUi.OpenForScopedEditing(BattleScenePath, BattleScenePath);
            try
            {
                GameObject deckObject = MythMayhemUi.FindRootInScene(scene, PlayerDeckName);
                if (deckObject == null)
                    throw new System.Exception(PlayerDeckName + " was not found in " + BattleScenePath);

                DeckController deck = deckObject.GetComponent<DeckController>();
                if (deck == null)
                    throw new System.Exception(PlayerDeckName + " has no DeckController component.");

                SerializedObject serialized = new SerializedObject(deck);
                SerializedProperty pool = serialized.FindProperty("cardPool");
                if (pool == null || !pool.isArray)
                    throw new System.Exception("DeckController.cardPool was not found.");

                int added = 0;
                added += AppendMissing<CreatureDefinition>(pool, CardType.Creature, "creatureDefinition");
                added += AppendMissing<SupportCardDefinition>(pool, CardType.Support, "supportDefinition");
                added += AppendMissing<BuildingDefinition>(pool, CardType.Building, "buildingDefinition");

                if (added > 0)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(deck);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                MythMayhemUi.FinishScopedScene(scene, BattleScenePath);

                Debug.Log("[Decks] Player deck now has " + pool.arraySize + " card(s); " +
                          added + " added.");
            }
            catch (System.Exception)
            {
                MythMayhemUi.DiscardScopedScene(scene);
                throw;
            }
        }

        // Appends any definition of the given type that is not already in the pool.
        private static int AppendMissing<T>(SerializedProperty pool, CardType type, string field)
            where T : Object
        {
            int added = 0;

            foreach (T definition in LoadAll<T>())
            {
                if (definition == null || Contains(pool, field, definition))
                    continue;

                Append(pool, type, field, definition);
                added++;
            }

            return added;
        }

        private static bool Contains(SerializedProperty pool, string field, Object definition)
        {
            for (int i = 0; i < pool.arraySize; i++)
            {
                SerializedProperty element = pool.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative(field).objectReferenceValue == definition)
                    return true;
            }

            return false;
        }

        private static void Append(SerializedProperty pool, CardType type, string field, Object definition)
        {
            int index = pool.arraySize;
            pool.InsertArrayElementAtIndex(index);

            SerializedProperty element = pool.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("type").enumValueIndex = (int)type;
            element.FindPropertyRelative("creatureDefinition").objectReferenceValue = null;
            element.FindPropertyRelative("supportDefinition").objectReferenceValue = null;
            element.FindPropertyRelative("buildingDefinition").objectReferenceValue = null;
            element.FindPropertyRelative(field).objectReferenceValue = definition;
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            List<T> results = new List<T>();

            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { CardsRoot });
            foreach (string guid in guids)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    results.Add(asset);
            }

            return results;
        }
    }
}
