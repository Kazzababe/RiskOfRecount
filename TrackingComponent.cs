using RiskOfRecount.Ui;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RiskOfRecount {
    public class TrackingComponent : MonoBehaviour {
        public long dpsStart;
        public float peakDps;
        public SortedDictionary<long, float> damageEvents = new SortedDictionary<long, float>();
        public SortedDictionary<long, float> healEvents = new SortedDictionary<long, float>();

        public float DPS {
            get {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (damageEvents.Count == 0 || (now - damageEvents.Keys.Last()) >= RoRecount.dpsTimeout) {
                    return 0;
                }
                float total = damageEvents.Where((item) => item.Key >= dpsStart).Sum((pair) => pair.Value);
                return total / (now - dpsStart) * 1000.0f;
            }
        }

        public float TotalDamage {
            get {
                return damageEvents.Sum((pair) => pair.Value);
            }
        }

        public float TotalHealing {
            get {
                return healEvents.Sum((pair) => pair.Value);
            }
        }

        public void Start() {
            Debug.Log("Created damage component...");
        }

        public void LogDamageEvent(float damage) {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((damageEvents.Count > 0 && (now - damageEvents.Keys.Last()) >= RoRecount.dpsTimeout) || damageEvents.Count == 0) {
                dpsStart = now;
            }
            if (damageEvents.ContainsKey(now)) {
                damageEvents[now] = damageEvents[now] + damage;
            } else {
                damageEvents.Add(now, damage);
            }
            // Check for peak dps and wait for at least a second before recording 
            if (DPS > peakDps && now - dpsStart >= 1000) {
                peakDps = DPS;
            }
            if (RoRecount.display <= 2) {
                UpdateBars();
            }
        }

        public void LogHealEvent(float amount) {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (healEvents.ContainsKey(now)) {
                healEvents[now] = healEvents[now] + amount;
            } else {
                healEvents.Add(now, amount);
            }
            if (RoRecount.display == 3) {
                UpdateBars();
            }
        }

        public void UpdateBars() {
            foreach ((PlayerCharacterMasterController player, RecountBar bar) in RoRecount.bars) {
                bar.UpdateValue(player);
            }
            List<RecountBar> bars = RoRecount.bars.Values.ToList();
            bars.Sort((RecountBar bar1, RecountBar bar2) => {
                return bar1.bar.GetComponent<LayoutElement>().preferredWidth.CompareTo(bar2.GetComponent<LayoutElement>().preferredWidth);
            });
            for (int i = 0; i < bars.Count; i++) {
                if (bars[i].container.transform.GetSiblingIndex() != i + 2) {
                    // I don't actually know if this affects performance or not
                    bars[i].container.transform.SetSiblingIndex(i + 2);
                }
            }
        }
    }
}