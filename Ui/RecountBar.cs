using LeTai.Asset.TranslucentImage;
using RiskOfRecount.Tracking;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount.Ui {
    public class RecountBar : MonoBehaviour {
        public static Dictionary<PlayerCharacterMasterController, RecountBar> BarInstances = new Dictionary<PlayerCharacterMasterController, RecountBar>();
        private static List<Color32> colors = new List<Color32>() {
            new Color32(98, 78, 51, 255),
            new Color32(71, 115, 149, 255),
            new Color32(155, 150, 58, 255),
            new Color32(0, 161, 90, 255),
            new Color32(151, 155, 156, 255),
            new Color32(104, 130, 68, 255),
            new Color32(3, 62, 133, 255),
            new Color32(154, 76, 0, 255)
        };

        public GameObject container;
        private GameObject bar;
        private GameObject nameTextObject;
        private GameObject valueTextObject;

        private Coroutine animation;

        private string nameText;
        public string NameText {
            get { return nameText; }
            set {
                nameText = value;
                if (nameTextObject) {
                    nameTextObject.GetComponent<HGTextMeshProUGUI>().text = nameText;
                }
            }
        }

        private string valueText = "0 (0.0%)";
        public string ValueText {
            get { return valueText; }
            set {
                valueText = value;
                if (valueTextObject) {
                    valueTextObject.GetComponent<HGTextMeshProUGUI>().text = valueText;
                }
            }
        }

        public float Percentage {
            get { return bar.GetComponent<LayoutElement>().preferredWidth / 300.0f; }
        }

        public BaseTrackingStat CurrentTrackingStat {
            get { return (BaseTrackingStat)container.GetComponent(RoRecount.ui.CurrentTrackingDef.type); }
        }

        public PlayerCharacterMasterController player;
        public Dictionary<string, List<BaseTrackingStat>> trackers = new Dictionary<string, List<BaseTrackingStat>>();

        public void Start() {
            BarInstances.Add(player, this);

            container = new GameObject("container");
            container.transform.SetParent(gameObject.transform);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(300, 22);
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0, 0);
            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.preferredWidth = 300;
            containerLayout.preferredHeight = 22;
            VerticalLayoutGroup containerGroup = container.AddComponent<VerticalLayoutGroup>();
            containerGroup.childControlWidth = true;
            containerGroup.childForceExpandWidth = false;
            containerGroup.childControlHeight = true;
            containerGroup.childForceExpandHeight = false;

            bar = new GameObject("bar");
            bar.transform.SetParent(container.transform);
            RectTransform barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0, 0);
            barRect.sizeDelta = new Vector2(300, 22);
            Image barImage = bar.AddComponent<Image>();
            barImage.sprite = Resources.Load<GameObject>("Prefabs/UI/Tooltip").GetComponentInChildren<TranslucentImage>(true).sprite;
            barImage.color = colors[new System.Random().Next(colors.Count)];
            barImage.type = Image.Type.Sliced;
            barImage.fillCenter = true;
            LayoutElement barLayout = bar.AddComponent<LayoutElement>();
            barLayout.preferredWidth = 300;
            barLayout.preferredHeight = 22;

            nameTextObject = new GameObject("name");
            nameTextObject.transform.SetParent(container.transform);
            RectTransform nameRect = nameTextObject.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            HGTextMeshProUGUI nameMesh = nameTextObject.AddComponent<HGTextMeshProUGUI>();
            nameMesh.fontSize = 16;
            nameMesh.color = LocalUserManager.GetFirstLocalUser().cachedMasterController.GetDisplayName().Equals(NameText) ? Color.yellow : Color.white;
            nameMesh.outlineWidth = 0.5f;
            nameMesh.text = player.GetDisplayName();
            nameMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            nameMesh.margin = new Vector4(3, 0, 0, 0);
            nameTextObject.AddComponent<LayoutElement>().ignoreLayout = true;

            valueTextObject = new GameObject("value");
            valueTextObject.transform.SetParent(container.transform);
            RectTransform valueRect = valueTextObject.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 0.5f);
            HGTextMeshProUGUI valueMesh = valueTextObject.AddComponent<HGTextMeshProUGUI>();
            valueMesh.fontSize = 16;
            valueMesh.color = Color.white;
            valueMesh.text = ValueText;
            valueMesh.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            valueMesh.margin = new Vector4(0, 0, 3, 0);
            valueTextObject.AddComponent<LayoutElement>().ignoreLayout = true;

            foreach (TrackingDef def in RoRecount.TrackingDefs) {
                BaseTrackingStat stat = container.AddComponent(def.type) as BaseTrackingStat;
                if (!trackers.ContainsKey(stat.type)) {
                    trackers.Add(stat.type, new List<BaseTrackingStat>());
                }
                trackers[stat.type].Add(stat);
            }
        }

        public void LogEvent(string type, float amount) {
            if (trackers.ContainsKey(type)) {
                trackers[type].ForEach((stat) => stat.LogEvent(amount));
            }
        }

        public void UpdateValue() {
            if (animation != null) {
                StopCoroutine(animation);
            }
            float total = CurrentTrackingStat.GetTotalValue();
            float value = CurrentTrackingStat.GetValue();
            float percent = value / total * 100.0f;

            String display = Math.Round(value).ToString(CultureInfo.InvariantCulture);
            if (value >= 1000000000) {
                display = value.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            } else if (value >= 1000000) {
                display = value.ToString("0,,.##M", CultureInfo.InvariantCulture);
            } else if (value >= 1000) {
                display = value.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            LayoutElement layoutElement = bar.GetComponent<LayoutElement>();
            animation = StartCoroutine(AnimateBar(layoutElement.preferredWidth, value / total * 300));
            ValueText = $"{display} ({(float.IsNaN(percent) ? 0 : percent):N1}%)";
        }
        private IEnumerator AnimateBar(float start, float end) {
            LayoutElement layoutElement = bar.GetComponent<LayoutElement>();
            float time = 0;
            while (time < 0.5f) {
                layoutElement.preferredWidth = EaseInOut(start, end, time, 0.5f);

                yield return null;
                time += Time.deltaTime;
            }
            layoutElement.preferredWidth = end;
        }
        private float EaseInOut(float initial, float final, float time, float duration) {
            float change = final - initial;
            time /= duration / 2;
            if (time < 1f) return change / 2 * time * time + initial;
            time--;
            return -change / 2 * (time * (time - 2) - 1) + initial;
        }
    }
}
