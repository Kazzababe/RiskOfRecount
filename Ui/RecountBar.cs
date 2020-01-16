using LeTai.Asset.TranslucentImage;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount.Ui {
    public class RecountBar {
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

        private GameObject container;
        private GameObject bar;
        private GameObject nameText;
        private GameObject valueText;

        public RecountBar(GameObject parent, String playerName) {
            container = new GameObject("container");
            container.transform.SetParent(parent.transform);

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

            nameText = new GameObject("name");
            nameText.transform.SetParent(container.transform);
            RectTransform nameRect = nameText.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            HGTextMeshProUGUI nameMesh = nameText.AddComponent<HGTextMeshProUGUI>();
            nameMesh.fontSize = 16;
            nameMesh.color = LocalUserManager.GetFirstLocalUser().cachedMasterController.GetDisplayName().Equals(playerName) ? Color.red : Color.white;
            nameMesh.text = playerName;
            nameMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            nameMesh.margin = new Vector4(3, 0, 0, 0);
            nameText.AddComponent<LayoutElement>().ignoreLayout = true;

            valueText = new GameObject("value");
            valueText.transform.SetParent(container.transform);
            RectTransform valueRect = valueText.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 0.5f);
            HGTextMeshProUGUI valueMesh = valueText.AddComponent<HGTextMeshProUGUI>();
            valueMesh.fontSize = 16;
            valueMesh.color = Color.white;
            valueMesh.text = "0.00";
            valueMesh.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            valueMesh.margin = new Vector4(0, 0, 3, 0);
            valueText.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        public void UpdateValue(PlayerCharacterMasterController player) {
            TrackingComponent trackingComponent = player.GetComponent<TrackingComponent>();
            float total = RoRecount.display == 0 ? RoRecount.GetTotalDamage() : RoRecount.display == 1 ?
                RoRecount.GetTotalDPS() : RoRecount.GetTotalHealing();
            float value = RoRecount.display == 0 ? trackingComponent.TotalDamage : RoRecount.display == 1 ?
                trackingComponent.DPS : trackingComponent.TotalHealing;

            String display = Math.Round(value).ToString(CultureInfo.InvariantCulture);
            if (value >= 1000000000) {
                display = value.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            } else if (value >= 1000000) {
                display = value.ToString("0,,.##M", CultureInfo.InvariantCulture);
            } else if (value >= 1000) {
                display = value.ToString("0,.#K", CultureInfo.InvariantCulture);
            }

            bar.GetComponent<LayoutElement>().preferredWidth = value / total * 300;
            valueText.GetComponent<HGTextMeshProUGUI>().text = display;
        }
    }
}
