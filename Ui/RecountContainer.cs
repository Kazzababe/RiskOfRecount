using RiskOfRecount.Tracking;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount.Ui {
    public class RecountContainer : MonoBehaviour {
        public static float AnimationDuration = 0.2f;

        private GameObject canvas;
        public GameObject container;
        private GameObject header;
        private GameObject headerText;
        private GameObject barsContainer;

        private Coroutine updateCoroutine;
        private int trackingDefPosition = 0;
        public TrackingDef CurrentTrackingDef {
            get { return RoRecount.TrackingDefs[trackingDefPosition]; }
        }

        public void Awake() {
            canvas = new GameObject("Recount Canvas") {
                layer = 5
            };
            canvas.transform.SetParent(gameObject.transform);
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = -1;
            canvas.AddComponent<GraphicRaycaster>();

            Debug.Log("Create ui");
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
            headerMesh.text = CurrentTrackingDef.display;
            headerMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            headerMesh.margin = new Vector4(3, 0, 0, 0);
            LayoutElement headerLayout = headerText.AddComponent<LayoutElement>();
            headerLayout.flexibleWidth = 1.0f;

            GameObject buttonContainer = new GameObject("Buttons container");
            buttonContainer.transform.SetParent(header.transform);
            HorizontalLayoutGroup buttonContainerGroup = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainerGroup.childControlWidth = true;
            buttonContainerGroup.childForceExpandWidth = false;
            buttonContainerGroup.padding = new RectOffset(0, 0, 5, 0);
            buttonContainer.AddComponent<LayoutElement>().flexibleWidth = 0;
            buttonContainer.AddComponent<ContentSizeFitter>();

            GameObject previousButtonObject = new GameObject("Next button");
            previousButtonObject.transform.SetParent(buttonContainer.transform);
            Button previousButton = previousButtonObject.AddComponent<Button>();
            previousButton.onClick.AddListener(() => {
                PreviousCycle();
            });
            HGTextMeshProUGUI previousText = previousButton.gameObject.AddComponent<HGTextMeshProUGUI>();
            previousText.fontSize = 20;
            previousText.color = Color.white;
            previousText.text = "<-";
            previousText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            previousText.margin = new Vector4(3, 0, 3, 0);
            previousButtonObject.AddComponent<LayoutElement>().flexibleWidth = 0;
            previousButtonObject.AddComponent<ContentSizeFitter>();

            GameObject nextButtonObject = new GameObject("Next button");
            nextButtonObject.transform.SetParent(buttonContainer.transform);
            Button nextButton = nextButtonObject.AddComponent<Button>();
            nextButton.onClick.AddListener(() => {
                NextCycle();
            });
            HGTextMeshProUGUI nextText = nextButton.gameObject.AddComponent<HGTextMeshProUGUI>();
            nextText.fontSize = 20;
            nextText.color = Color.white;
            nextText.text = "->";
            nextText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            nextText.margin = new Vector4(3, 0, 3, 0);
            nextButtonObject.AddComponent<LayoutElement>().flexibleWidth = 0;
            nextButtonObject.AddComponent<ContentSizeFitter>();

            GameObject info = new GameObject("Recount info");
            info.transform.SetParent(container.transform);
            RectTransform infoRect = info.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(0, 0.5f);
            HGTextMeshProUGUI infoMesh = info.AddComponent<HGTextMeshProUGUI>();
            infoMesh.fontSize = 12;
            infoMesh.color = Color.white;
            infoMesh.text = "(Press F2 & F3 to cycle display stat)";
            infoMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            infoMesh.margin = new Vector4(3, 0, 0, 0);

            barsContainer = new GameObject("Bars container");
            barsContainer.transform.SetParent(container.transform);
            RectTransform barsContainerRect = barsContainer.AddComponent<RectTransform>();
            barsContainerRect.sizeDelta = new Vector2(300, 22);
            barsContainerRect.anchorMin = new Vector2(0, 0);
            barsContainerRect.anchorMax = new Vector2(1, 1);
            barsContainerRect.pivot = new Vector2(0, 0);
            VerticalLayoutGroup barsContainerGroup = barsContainer.AddComponent<VerticalLayoutGroup>();
            barsContainerGroup.childControlWidth = true;
            barsContainerGroup.childForceExpandWidth = false;
            barsContainerGroup.childControlHeight = true;
            barsContainerGroup.childForceExpandHeight = false;
            barsContainerGroup.spacing = 4;
            barsContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Update() {
            if (Input.GetKeyDown(KeyCode.F2)) {
                PreviousCycle();
            } else if (Input.GetKeyDown(KeyCode.F3)) {
                NextCycle();
            }
        }

        private void NextCycle() {
            trackingDefPosition = trackingDefPosition == RoRecount.TrackingDefs.Count - 1 ? 0 : trackingDefPosition + 1;
            headerText.GetComponent<HGTextMeshProUGUI>().text = CurrentTrackingDef.display;

            UpdateBars();

            if (CurrentTrackingDef.doUpdate) {
                updateCoroutine = StartCoroutine(UpdateDisplay());
            } else if (updateCoroutine != null) {
                StopCoroutine(updateCoroutine);
            } 
        }

        private void PreviousCycle() {
            trackingDefPosition = trackingDefPosition == 0 ? RoRecount.TrackingDefs.Count - 1 : trackingDefPosition - 1;
            headerText.GetComponent<HGTextMeshProUGUI>().text = CurrentTrackingDef.display;

            UpdateBars();

            if (CurrentTrackingDef.doUpdate) {
                updateCoroutine = StartCoroutine(UpdateDisplay());
            } else if (updateCoroutine != null) {
                StopCoroutine(updateCoroutine);
            }
        }

        public void OnDestroy() {
            RecountBar.BarInstances.Clear();
        }

        public void AddBar(PlayerCharacterMasterController player) {
            if (RecountBar.BarInstances.ContainsKey(player)) {
                return;
            }
            barsContainer.AddComponent<RecountBar>().player = player;
        }

        public void RemoveBar(PlayerCharacterMasterController player) {
            RecountBar.BarInstances.Remove(player);
            foreach (RecountBar bar in barsContainer.GetComponents<RecountBar>().Reverse()) {
                if (bar.player.Equals(player)) {
                    Destroy(bar);
                }
            }
        }

        public void LogEvent(PlayerCharacterMasterController player, string type, float amount) {
            RecountBar.BarInstances[player].LogEvent(type, amount);
        }

        public void UpdateBars() {
            barsContainer.GetComponents<RecountBar>().ToList().ForEach((bar) => bar.UpdateValue());
            List<RecountBar> bars = barsContainer.GetComponents<RecountBar>().ToList();
            bars.Sort((bar1, bar2) => {
                return bar2.Percentage.CompareTo(bar1.Percentage);
            });
            for (int i = 0; i < bars.Count; i++) {
                bars[i].container.transform.SetSiblingIndex(i);
            }
        }
        private IEnumerator UpdateDisplay() {
            while (true) {
                RecountBar.BarInstances.Values.ToList().ForEach((bar) => bar.UpdateValue());
                yield return new WaitForSeconds(0.4f);
            }
        }
    }
}