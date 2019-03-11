using BeatBlockSystem;
using GameObjectControllerImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackSystem;
using UnityEngine;

namespace ConfigurationScripts {
    public class StraightBlockControllerConfiguration : MonoBehaviour, IAnimControllerConfigurationScript {
        [Header("List of animation object prefabs which are to be controlled by a StaightBlockController")]
        public List<AnimationObject> PrefabList;

        public IAnimationGameObjectController CreateController(int animTypeId, ICategoricalObjectPool<AnimationObject> pool) {
            return new StraightBlockController(animTypeId, pool, new Vector3(0, 0, 0), new Vector3(0, 0, 20));
        }

        public IEnumerable<AnimationObject> GetPrefabs() {
            return PrefabList;
        }
    }
}
