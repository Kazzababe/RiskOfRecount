using RiskOfRecount.Ui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfRecount.Tracking {
    public class BaseTrackingStat : MonoBehaviour {
        public string type;
        protected SortedDictionary<long, float> events = new SortedDictionary<long, float>();

        public void Start() {
            Debug.Log("Fuck yo");
        }

        public virtual float GetValue() {
            return events.Values.Sum();
        }

        public float GetTotalValue() {
            return RecountBar.BarInstances.Values.Sum((bar) => {
                return bar.trackers.Values.SelectMany((x) => x).ToList().Where((tracker) => tracker.GetType() == GetType()).Sum((stat) => stat.GetValue());
            });
        }

        public virtual void LogEvent(float value) {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (events.ContainsKey(now)) {
                events[now] = events[now] + value;
            } else {
                events.Add(now, value);
            }
            RoRecount.ui.UpdateBars();
        }
    }
}
