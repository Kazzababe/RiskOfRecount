using BepInEx;
using RiskOfRecount.Ui;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount {
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.ravioligravioli.RoRecount", "Risk of Recount", "1.1.0")]
    public class RoRecount : BaseUnityPlugin {
        public static long dpsTimeout = 10000;
        public static Dictionary<PlayerCharacterMasterController, RecountBar> bars = new Dictionary<PlayerCharacterMasterController, RecountBar>();
        public static int display = 0;

        private GameObject canvas;
        private GameObject container;
        private GameObject header;
        private Dictionary<int, string> displays = new Dictionary<int, string>() {
            { 0, "Total Damage" },
            { 1, "Damage Per Second (DPS)" },
            { 2, "Total Healing" }
        };


        private Coroutine dpsCoroutine = null;

        public void OnEnable() {
            On.RoR2.HealthComponent.Heal += (orig, self, amount, procChainMask, nonRegen) => {
                if (self.body && self.body.master && self.body.master.playerCharacterMasterController) {
                    PlayerCharacterMasterController player = self.body.master.playerCharacterMasterController;
                    if (player) {
                        if (Math.Round(self.health + amount) <= self.body.maxHealth) {
                            // Player has actually healed
                            if (player.GetComponent<TrackingComponent>() == null) {
                                player.gameObject.AddComponent<TrackingComponent>();
                            }
                            TrackingComponent trackingComponent = player.GetComponent<TrackingComponent>();
                            trackingComponent.LogHealEvent(amount);
                        }
                    }
                }

                return orig(self, amount, procChainMask, nonRegen);
            };
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, info, victim) => {
                orig(self, info, victim);

                if (info.attacker) {
                    CharacterBody body = info.attacker.GetComponent<CharacterBody>();
                    if (body && body.master) {
                        PlayerCharacterMasterController player = null;
                        if (body && body.master) {
                            if (body.master.playerCharacterMasterController) {
                                player = body.master.playerCharacterMasterController;
                            }
                            if (body.master.minionOwnership && body.master.minionOwnership.ownerMaster && body.master.minionOwnership.ownerMaster.playerCharacterMasterController) {
                                player = body.master.minionOwnership.ownerMaster.playerCharacterMasterController;
                            }
                        }
                        if (player) {
                            if (player.GetComponent<TrackingComponent>() == null) {
                                player.gameObject.AddComponent<TrackingComponent>();
                            }
                            TrackingComponent trackingComponent = player.GetComponent<TrackingComponent>();
                            trackingComponent.LogDamageEvent(info.damage);
                        }
                    }
                }
            };

            // Showing the UI at BeginState guarantees players are joined in
            On.RoR2.Run.BeginStage += (orig, self) => {
                orig(self);

                if (canvas == null) {
                    ShowUI();
                }
            };

            // Destroy the canvas at the end of a run
            On.RoR2.Run.BeginGameOver += (orig, self, result) => {
                orig(self, result);

                Destroy(canvas);
            };

            // If a new player joins mid-game, add a bar for them to the UI
            On.RoR2.Run.OnUserAdded += (orig, self, user) => {
                orig(self, user);

                if (user && user.masterController) {
                    PlayerCharacterMasterController player = user.masterController;
                    if (player.GetComponent<TrackingComponent>() == null) {
                        player.gameObject.AddComponent<TrackingComponent>();
                    }
                    if (canvas != null) {
                        AddBar(player);
                    }
                }
            };

            // If a player leaves mid-game, remove their respective bar from the UI
            On.RoR2.Run.OnUserRemoved += (orig, self, user) => {
                orig(self, user);

                if (user && user.masterController) {
                    bars.Remove(user.masterController);
                }
            };
        }

        public void Update() {
            if (RoR2.Run.instance == null) {
                return;
            }
            if (Input.GetKeyDown(KeyCode.F2)) {
                display = display == 2 ? 0 : display + 1;

                if (display == 1) {
                    // Switch to DPS display, start coroutine
                    StartCoroutine(UpdateDPS());
                } else if (dpsCoroutine != null) {
                    // Switched away from DPS display, stop coroutine
                    StopCoroutine(dpsCoroutine);
                }

                if (header) {
                    header.GetComponent<HGTextMeshProUGUI>().text = displays[display];
                    PlayerCharacterMasterController player = PlayerCharacterMasterController.instances[0];
                    if (player) {
                        player.GetComponent<TrackingComponent>().UpdateBars();
                    }
                }
            }
        }

        public IEnumerator UpdateDPS() {
            while (true) {
                if (display == 1 && canvas) {
                    PlayerCharacterMasterController player = PlayerCharacterMasterController.instances[0];
                    if (player) {
                        player.GetComponent<TrackingComponent>().UpdateBars();
                    }
                }
                yield return new WaitForSeconds(0.4f);
            }
        }

        public void ShowUI() {
            bars.Clear();

            canvas = new GameObject("Recount Canvas") {
                layer = 5
            };
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = -1;
            canvas.AddComponent<GraphicRaycaster>();

            container = new GameObject("Recount container");
            container.transform.SetParent(canvas.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 0.5f);
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.pivot = new Vector2(0, 0);
            containerRect.anchoredPosition = new Vector2(-400, 0);
            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            VerticalLayoutGroup containerGroup = container.AddComponent<VerticalLayoutGroup>();
            containerGroup.childControlWidth = true;
            containerGroup.childForceExpandWidth = false;
            containerGroup.childControlHeight = true;
            containerGroup.childForceExpandHeight = false;
            containerGroup.padding = new RectOffset(5, 5, 5, 5);
            containerGroup.spacing = 4;
            Image containerImage = container.AddComponent<Image>();
            containerImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where((sprite) => sprite.name.Equals("texUIPopupRect")).First();
            containerImage.type = Image.Type.Sliced;
            containerImage.color = new Color32(41, 43, 45, 241);
            containerImage.fillCenter = true;
            container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            header = new GameObject("Recount header");
            header.transform.SetParent(container.transform);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0, 0.5f);
            HGTextMeshProUGUI headerMesh = header.AddComponent<HGTextMeshProUGUI>();
            headerMesh.fontSize = 20;
            headerMesh.color = Color.white;
            headerMesh.text = displays[display];
            headerMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            headerMesh.margin = new Vector4(3, 0, 0, 0);

            GameObject info = new GameObject("Recount info");
            info.transform.SetParent(container.transform);
            RectTransform infoRect = info.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(0, 0.5f);
            HGTextMeshProUGUI infoMesh = info.AddComponent<HGTextMeshProUGUI>();
            infoMesh.fontSize = 12;
            infoMesh.color = Color.white;
            infoMesh.text = "(Press F2 to cycle display stat)";
            infoMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            infoMesh.margin = new Vector4(3, 0, 0, 0);

            for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++) {
                PlayerCharacterMasterController player = PlayerCharacterMasterController.instances[i];
                if (player) {
                    AddBar(player);
                }
            }
        }

        private void AddBar(PlayerCharacterMasterController player) {
            if (bars.ContainsKey(player)) {
                return;
            }
            bars.Add(player, new RecountBar(container, player.GetDisplayName()));
        }

        public static float GetTotalDamage() {
            return PlayerCharacterMasterController.instances.Sum((p) => {
                if (p && p.GetComponent<TrackingComponent>()) {
                    return p.GetComponent<TrackingComponent>().TotalDamage;
                }
                return 0;
            });
        }

        public static float GetTotalDPS() {
            return PlayerCharacterMasterController.instances.Sum((p) => {
                if (p && p.GetComponent<TrackingComponent>()) {
                    return p.GetComponent<TrackingComponent>().DPS;
                }
                return 0;
            });
        }

        public static float GetTotalHealing() {
            return PlayerCharacterMasterController.instances.Sum((p) => {
                if (p && p.GetComponent<TrackingComponent>()) {
                    return p.GetComponent<TrackingComponent>().TotalHealing;
                }
                return 0;
            });
        }
    }
}
