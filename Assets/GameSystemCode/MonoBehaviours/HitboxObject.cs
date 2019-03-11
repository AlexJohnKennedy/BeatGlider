using BeatBlockSystem;
using TrackSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameObjectControllerImplementations {
    // Basic type to represent any object which can be controlled by an HitboxGameObjectController.
    public abstract class HitboxObject : MonoBehaviour {
        [Header("List of BeatBlock archetype IDs, informing which BeatBlock types will use this animation prefab")]
        public List<int> BeatBlockTypesWhichUseThis;

        public abstract void PlaceAtWorldSpace(Vector2 spawnPosition);
        public abstract void SetAnimationDirection(Vector2 animationDirection);
        public abstract void ActivateGameObject();
        public abstract void DeactivateGameObject();

        public abstract void UpdateObj(float time);
    }
}
