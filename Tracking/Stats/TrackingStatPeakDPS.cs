using System;

namespace RiskOfRecount.Tracking.Stats {
    public class TrackingStatPeakDPS : TrackingStatCurrentDPS {
        private float peakDps = 0;

        public override float GetValue() {
            return peakDps;
        }

        public override void LogEvent(float value) {
            base.LogEvent(value);

            float dps = base.GetValue();
            if (dps > peakDps && DateTimeOffset.Now.ToUnixTimeMilliseconds() - dpsStart >= 1000) {
                peakDps = dps;
            }
        }
    }
}
