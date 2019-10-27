﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
#if ProjectCore
using Sirenix.OdinInspector;
#endif

namespace BS
{
    public class LevelDefinition : MonoBehaviour
    {
        public static LevelDefinition current;
        public Transform playerStart;
        public List<CustomReference> customReferences;
        public bool loadDefaultCharIfNeeded = true;
        [NonSerialized]
        public bool initialized;

        [Serializable]
        public class CustomReference
        {
            public string name;
            public List<Transform> transforms;
        }

#if ProjectCore

        [NonSerialized, ShowInInspector, ReadOnly]
        public LevelData data;
        [NonSerialized, ShowInInspector, ReadOnly]
        public LevelData.ModeRank modeRank;

        protected virtual void Awake()
        {
            if (gameObject.scene.name.ToLower() != "master")
            {
                current = this;
            }

            if (loadDefaultCharIfNeeded && GameManager.playerData == null)
            {
                Debug.Log("No player data loaded, get first character slot");
                List<CharacterData> characterDataList = DataManager.GetCharacters();
                if (GameManager.local.defaultCharacterIndex < characterDataList.Count && GameManager.local.defaultCharacterIndex >= 0)
                {
                    GameManager.playerData = characterDataList[GameManager.local.defaultCharacterIndex];
                }
                else if (characterDataList.Count > 0)
                {
                    GameManager.playerData = characterDataList[0];
                }
                else
                {
                    Debug.LogError("No character found in saves");
                    GameManager.playerData = new CharacterData("Temp", "Temp");
                }
                GameManager.playerData.UpdateVersion();
            }
        }

        protected virtual void Start()
        {
            StartCoroutine(OnLevelStartedCoroutine());
        }

        protected virtual void Update()
        {
            if (initialized)
            {
                foreach (LevelModule levelModeModule in modeRank.mode.modules)
                {
                    levelModeModule.Update(this);
                }
            }
        }

        protected virtual IEnumerator OnLevelStartedCoroutine()
        {
            // Wait game to initalize
            while (!GameManager.initialized) yield return new WaitForEndOfFrame();

            if (this.gameObject.scene.name.ToLower() == "master")
            {
                data = Catalog.GetData<LevelData>("Master");
            }
            else
            {
                while (SceneManager.GetActiveScene() != this.gameObject.scene) yield return new WaitForEndOfFrame();
            }

            try
            {
                if (modeRank == null) modeRank = data.GetFirstModRank();
                // Load level mode module
                foreach (LevelModule levelModeModule in modeRank.mode.modules)
                {
                    levelModeModule.OnLevelLoaded(this);
                }
            }
            catch (Exception e)
            {
                LoadingCamera.SetState(LoadingCamera.State.Error);
                throw;
            }

            // Wait modules to load
            foreach (LevelModule levelModeModule in modeRank.mode.modules)
            {
                while (!levelModeModule.initialized) yield return new WaitForEndOfFrame();
            }

            if (data.id.ToLower() != "master")
            {
                Player player = null;
                try
                {
                    // Spawn player
                    if (data.spawnPlayer)
                    {
                        player = Instantiate(Resources.Load("Player", typeof(Player)) as Player, playerStart.position, playerStart.rotation);
                        player.morphology = GameManager.playerData.morphology;
                        GameManager.local.FirePlayerSpawnedEvent(player);
                    }

                    if (player && Application.platform != RuntimePlatform.Android)
                    {
                        GameManager.liv.HMDCamera = player.head.cam;
                        GameManager.liv.TrackedSpaceOrigin = player.transform;
                        GameManager.liv.enabled = true;
                    }

                }
                catch (Exception e)
                {
                    LoadingCamera.SetState(LoadingCamera.State.Error);
                    throw;
                }

                // Spawn player body
                if (data.spawnBody && player)
                {
                    Creature playerCreature = null;
                    try
                    {
                        playerCreature = Catalog.GetData<CreatureData>(Catalog.gameData.defaultPlayerCreatureID).Instantiate(playerStart.position, playerStart.rotation);
                        playerCreature.container.containerID = null;
                        playerCreature.loadUmaPreset = false;
                        playerCreature.container.content = GameManager.playerData.inventory;
                    }
                    catch (Exception e)
                    {
                        LoadingCamera.SetState(LoadingCamera.State.Error);
                        throw;
                    }

                    if (playerCreature.umaCharacter)
                    {
                        try
                        {
                            playerCreature.umaCharacter.LoadUmaPreset(GameManager.playerData.umaPreset, null);
                        }
                        catch (Exception e)
                        {
                            LoadingCamera.SetState(LoadingCamera.State.Error);
                            throw;
                        }
                        while (playerCreature.umaCharacter.characterDataLoading) yield return new WaitForEndOfFrame();
                    }
                    while (!playerCreature.initialized) yield return new WaitForEndOfFrame();
                    try
                    {
                        player.SetBody(playerCreature.body);
                    }
                    catch (Exception e)
                    {
                        LoadingCamera.SetState(LoadingCamera.State.Error);
                        throw;
                    }
                }
                try
                {
                    if (data.fadeOutTime > 0) PostProcessManager.local.DoTimedEffect(Color.black, PostProcessManager.TimedEffect.FadeOut, data.fadeOutTime);
                }
                catch (Exception e)
                {
                    LoadingCamera.SetState(LoadingCamera.State.Error);
                    throw;
                }
            }
            LoadingCamera.SetState(LoadingCamera.State.Disabled);
            initialized = true;
        }

        public virtual void OnLevelUnload()
        {
            foreach (LevelModule levelModeModule in modeRank.mode.modules)
            {
                levelModeModule.OnLevelUnloaded(this);
            }
        }
#endif
    }
}