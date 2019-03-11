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
    /// <summary>
    /// Base script type for making an editor-configurable mapping of IAnimationGameObjectController type to AnimationObject prefabs
    /// </summary>
    public interface IAnimControllerConfigurationScript {
        // Should be editor settable.
        IEnumerable<AnimationObject> GetPrefabs();
        IAnimationGameObjectController CreateController(int animTypeId, ICategoricalObjectPool<AnimationObject> pool);
    }
}
