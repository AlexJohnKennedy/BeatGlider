using BeatBlockSystem;
using TrackSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameObjectControllerImplementations {

    /// <summary>
    /// This class is a simple implementation of an AnimationGameObjectController, which is responsbile for controlling the animation of a
    /// given beat block! It will acquire an AnimationObject from a pool, when needed, and control its animation until the BeatBlock has hit!
    /// </summary>
    public class StraightBlockController : IAnimationGameObjectController {
        
        public int AnimationTypeId { get; }

        private readonly ICategoricalObjectPool<AnimationObject> pool;
        private Vector3 playerPlaneCentrePoint;
        private Vector3 backPlaneCentrePoint;
        private Vector3 animationDirection;
        private bool isActive;
        private AnimationObject currObject;

        public StraightBlockController(int typeId, ICategoricalObjectPool<AnimationObject> pool, Vector3 playerPlaneCentrePoint, Vector3 backPlaneCentrePoint) {
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

            // Calculate the position to spawn the animation object at. This will be the (backPlaneCentrePoint + offset).
            // In this implementation, we assume that we are aligned to the gameworld global axes!
            Vector3 spawnPosition = new Vector3(backPlaneCentrePoint.x + offset.XPos, backPlaneCentrePoint.y + offset.YPos, backPlaneCentrePoint.z);

            currObject.PlaceAtWorldSpace(spawnPosition);
            currObject.SetAnimationDirection(animationDirection);
            currObject.ActivateGameObject();

            return true;
        }

        public bool Update(float timeIndex) {
            currObject.UpdateObj(timeIndex);
            if (timeIndex >= 1f) {
                // We are done! We should deactivate this object and return it the pool.
                currObject.DeactivateGameObject();
                pool.PoolObject(currObject, AnimationTypeId);
                isActive = false;
                currObject = null;

                return true;
            }
            return false;
        }
    }

    public class HitboxGameObjectController : IHitboxGameObjectController {
        // TODO - DESIGN AND IMPLEMENT THIS!

        private readonly ICategoricalObjectPool<HitboxObject> pool;
        private Vector2 playerPlaneCentrePoint;
        private bool isActive;
        private HitboxObject currObject;

        public HitboxGameObjectController(int typeId, ICategoricalObjectPool<HitboxObject> pool, Vector2 playerPlaneCentrePoint, float hitDelay, float hitboxDuration) {
            this.HitboxTypeId = typeId;
            this.HitDelayOffset = hitDelay;
            this.pool = pool;
            this.playerPlaneCentrePoint = playerPlaneCentrePoint;
            currObject = null;
            isActive = false;
            this.HitboxDuration = hitboxDuration;
        }

        public int HitboxTypeId { get; }

        public float HitDelayOffset { get; }

        public float HitboxDuration { get; }

        public bool StartAnimation(GridPosition offset, float sizeScalingFactor, float playbackSpeedScalingFactor) {
            throw new NotImplementedException();
        }

        public bool Update(float timeIndex) {
            throw new NotImplementedException();
        }
    }
}
