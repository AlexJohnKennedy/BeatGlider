using BeatBlockSystem;
using TrackSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameObjectControllerImplementations {

    // Basic type to represent any object which can be controlled by an AnimationGameObjectController.
    public interface IAnimationObject {

    }

    public class ScriptedAnimationObject : MonoBehaviour, IAnimationObject {

    }

    /// <summary>
    /// This class is a simple implementation of an AnimationGameObjectController, which is responsbile for controlling the animation of a
    /// given beat block! It will acquire an AnimationObject from a pool, when needed, and control its animation until the BeatBlock has hit!
    /// </summary>
    public class StraightBlockController : IAnimationGameObjectController {
        
        public int AnimationTypeId { get; }

        private readonly ICategoricalObjectPool<IAnimationObject> pool;
        private Vector3 playerPlaneCentrePoint;
        private Vector3 backPlaneCentrePoint;
        private Vector3 animationDirection;
        private bool isActive;
        private IAnimationObject currObject;

        public StraightBlockController(int typeId, ICategoricalObjectPool<IAnimationObject> pool, Vector3 playerPlaneCentrePoint, Vector3 backPlaneCentrePoint) {
            this.AnimationTypeId = typeId;
            this.pool = pool;
            this.playerPlaneCentrePoint = playerPlaneCentrePoint;
            this.backPlaneCentrePoint = backPlaneCentrePoint;
            animationDirection = playerPlaneCentrePoint - backPlaneCentrePoint;
            currObject = null;
            isActive = false;
        }

        // When this is called, we start playing our animation. Thus, we acquire a GameObject to control, place it in the correct position, configure its direction,
        // and then activate it!
        public bool StartAnimation(GridPosition offset, float scalingFactor, float speed, int comboFactor) {
            this.isActive = true;
            this.currObject = pool.GetObject(this.AnimationTypeId);
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
