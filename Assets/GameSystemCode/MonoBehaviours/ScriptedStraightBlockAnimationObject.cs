using BeatBlockSystem;
using TrackSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameObjectControllerImplementations {
    public class ScriptedStraightBlockAnimationObject : AnimationObject {

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
}
