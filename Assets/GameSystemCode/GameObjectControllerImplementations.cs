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
    public abstract class AnimationObject : MonoBehaviour {
        public abstract void PlaceAtWorldSpace(Vector3 spawnPosition);
        public abstract void SetAnimationDirection(Vector3 animationDirection);
        public abstract void ActivateGameObject();
        public abstract void DeactivateGameObject();

        public abstract void UpdateObj(float time);
    }

    public class ScriptedAnimationObject : AnimationObject {

        private float? zAxisStartValue;

        public override void ActivateGameObject() {
            if (!zAxisStartValue.HasValue) {
                throw new InvalidOperationException("Tried at activate a this gameobject before the spawn position was set. This is not allowed");
            }

            // Set the gameObject to which this script is attached to 'active'
            this.gameObject.SetActive(true);
        }

        public override void DeactivateGameObject() {
            zAxisStartValue = null;

            // Set the gameObject to which this script is attached to 'inactive'
            this.gameObject.SetActive(false);
        }

        public override void PlaceAtWorldSpace(Vector3 spawnPosition) {
            this.transform.position = spawnPosition;
            zAxisStartValue = spawnPosition.z;
        }

        public override void SetAnimationDirection(Vector3 animationDirection) {
            this.transform.forward = animationDirection;
        }

        public override void UpdateObj(float time) {
            // Linearly fly towards 0 on the Z axis.
            // At time zero, the z axis of this game object's position will be (spawnPosition.z)
            // At time one, the z axis of this game object's position will be 0.

            // We expect  (0 <= t <= 1)
            float newZpos = zAxisStartValue.Value - zAxisStartValue.Value * time;
            this.transform.position = new Vector3(transform.position.x, transform.position.y, newZpos);
        }
    }

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

        public int HitboxTypeId {
            get {
                throw new NotImplementedException();
            }
        }

        public float HitDelayOffset {
            get {
                throw new NotImplementedException();
            }
        }

        public float HitboxDuration {
            get {
                throw new NotImplementedException();
            }
        }

        public bool StartAnimation(GridPosition offset, float sizeScalingFactor, float playbackSpeedScalingFactor) {
            throw new NotImplementedException();
        }

        public bool Update(float timeIndex) {
            throw new NotImplementedException();
        }
    }
}
