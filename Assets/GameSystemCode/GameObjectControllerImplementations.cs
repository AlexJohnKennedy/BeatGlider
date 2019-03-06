using BeatBlockSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameObjectControllerImplementations {
    public class AnimationGameObjectController : IAnimationGameObjectController {
        // TODO - DESIGN AND IMPLEMENT THIS!
        public int AnimationTypeId => throw new NotImplementedException();

        public bool StartAnimation(GridPosition offset, float scalingFactor, float speed, int comboFactor) {
            throw new NotImplementedException();
        }

        public bool Update(float timeIndex) {
            throw new NotImplementedException();
        }
    }

    public class HitboxGameObjectController : IHitboxGameObjectController {
        // TODO - DESIGN AND IMPLEMENT THIS!

        // should this be a monobehaviour? Or wrap a monobehaviour?
        public int HitboxTypeId => throw new NotImplementedException();

        public float HitDelayOffset => throw new NotImplementedException();

        public float HitboxDuration => throw new NotImplementedException();

        public bool StartAnimation(GridPosition offset, float sizeScalingFactor, float playbackSpeedScalingFactor) {
            throw new NotImplementedException();
        }

        public bool Update(float timeIndex) {
            throw new NotImplementedException();
        }
    }
}
