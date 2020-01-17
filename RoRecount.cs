using BepInEx;
using RiskOfRecount.Ui;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount {
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.ravioligravioli.RoRecount", "Risk of Recount", "1.1.0")]
    public class RoRecount : BaseUnityPlugin {
        public static long dpsTimeout = 10000;
        public static Dictionary<PlayerCharacterMasterController, RecountBar> bars = new Dictionary<PlayerCharacterMasterController, RecountBar>();
        public static int display = 0;
        public static string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static RoRecount instance { get; set; }

        private GameObject canvas;
        private GameObject container;
        private GameObject header;
        private GameObject headerText;
        private Dictionary<int, string> displays = new Dictionary<int, string>() {
            { 0, "Total Damage" },
            { 1, "Current Damage Per Second (DPS)" },
            { 2, "Peak Damage Per Second (DPS)" },
            { 3, "Total Healing" }
        };

        private Coroutine dpsCoroutine = null;

        public void Awake() {
            instance = this;
        }

        public void onMouseDown() {

        }

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
                display = display == 3 ? 0 : display + 1;

                if (display == 1) {
                    // Switch to DPS display, start coroutine
                    dpsCoroutine = StartCoroutine(UpdateDPS());
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

        // These next three methods are hack af
        public Coroutine CreateBarAnimationCoroutine(LayoutElement element, float start, float end) {
            return StartCoroutine(AnimateBar(element, start, end));
        }

        public void DestroyBarAnimationCoroutine(Coroutine coroutine) {
            StopCoroutine(coroutine);
        }

        private IEnumerator AnimateBar(LayoutElement element, float start, float end) {
            float time = 0;
            while (time < RecountBar.AnimationDuration) {
                element.preferredWidth = EaseInOut(start, end, time, RecountBar.AnimationDuration);

                yield return null;
                time += Time.deltaTime;
            }
            element.preferredWidth = end;
        }

        // Stole this from the internet, ain't gonna lie
        private float EaseInOut(float initial, float final, float time, float duration) {
            float change = final - initial;
            time /= duration / 2;
            if (time < 1f) return change / 2 * time * time + initial;
            time--;
            return -change / 2 * (time * (time - 2) - 1) + initial;
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
            HorizontalLayoutGroup headerGroup = header.AddComponent<HorizontalLayoutGroup>();
            headerGroup.childForceExpandWidth = false;
            headerGroup.padding = new RectOffset(3, 3, 3, 3);

            headerText = new GameObject("Recount header text");
            headerText.transform.SetParent(header.transform);
            HGTextMeshProUGUI headerMesh = headerText.AddComponent<HGTextMeshProUGUI>();
            headerMesh.fontSize = 20;
            headerMesh.color = Color.white;
            headerMesh.text = displays[display];
            headerMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            headerMesh.margin = new Vector4(3, 0, 0, 0);
            LayoutElement headerLayout = headerText.AddComponent<LayoutElement>();
            headerLayout.flexibleWidth = 1.0f;

            GameObject test = new GameObject("test");
            test.transform.SetParent(header.transform);
            Button testButton = test.AddComponent<Button>();
            testButton.onClick.AddListener(() => Debug.Log("This does nothing for now");
            HGTextMeshProUGUI testText = testButton.gameObject.AddComponent<HGTextMeshProUGUI>();
            testText.fontSize = 20;
            testText.color = Color.white;
            testText.text = "-";
            testText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            testText.margin = new Vector4(0, 0, 3, 0);
            LayoutElement testLayout = test.AddComponent<LayoutElement>();
            //testLayout.preferredWidth = 16;
            testLayout.flexibleWidth = 0;
            test.AddComponent<ContentSizeFitter>();


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

        public static float GetTotalPeakDPS() {
            return PlayerCharacterMasterController.instances.Sum((p) => {
                if (p && p.GetComponent<TrackingComponent>()) {
                    return p.GetComponent<TrackingComponent>().peakDps;
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