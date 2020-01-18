using System;
using System.Linq;

namespace RiskOfRecount.Tracking.Stats {
    public class TrackingStatCurrentDPS : BaseTrackingStat {
        private static long DPSTimeout = 10000;

        protected long dpsStart;

        public void Awake() {
            type = "damage";
        }

        public override float GetValue() {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (events.Count == 0 || (now - events.Keys.Last()) >= DPSTimeout) {
                return 0;
            }
            float total = events.Where((item) => item.Key >= dpsStart).Sum((pair) => pair.Value);
            return total / (now - dpsStart) * 1000.0f;
        }

        public override void LogEvent(float value) {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((events.Count > 0 && (now - events.Keys.Last()) >= DPSTimeout) || events.Count == 0) {
                dpsStart = now;
            }
            base.LogEvent(value);
        }
    }
}
