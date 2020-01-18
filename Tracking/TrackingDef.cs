using System;

namespace RiskOfRecount.Tracking {
    public struct TrackingDef {
        public string display;
        public Type type;
        public bool doUpdate;

        public TrackingDef(string display, Type type, bool doUpdate = false) {
            if (type.IsAssignableFrom(typeof(BaseTrackingStat))) {
                throw new InvalidCastException("type must be of type Component");
            }
            this.display = display;
            this.type = type;
            this.doUpdate = doUpdate;
        }
    }
}
