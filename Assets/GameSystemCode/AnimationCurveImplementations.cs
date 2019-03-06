using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatBlockSystem;

namespace BeatBlockSystem.AnimationCurveImplementations {
    public class DefaultLinearCurve : IAnimationCurve {
        // All this does is return the same time; it doesn't adjust the curve at all!
        public float MapTimeToAnimationPercentage(float time) {
            return time;
        }
    }
    public class DefaultQuadraticCurve : IAnimationCurve {
        public float MapTimeToAnimationPercentage(float time) {
            return time * time;
        }
    }
}
