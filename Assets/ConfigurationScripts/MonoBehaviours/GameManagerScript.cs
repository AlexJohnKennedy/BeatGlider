using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackSystem;
using ConfigurationFactories;
using ObjectPoolingImplementations;
using BeatBlockSystem;
using BeatBlockSystem.AnimationCurveImplementations;
using GameObjectControllerImplementations;

namespace ConfigurationScripts {

    public class DUMMYFACTORY<T> : IArchetypeFactory<T> {
        public int NumArchetypes { get { return 9999; } }

        public T BuildArchetype(int typeId) {
            return default(T);
        }
    }

    public class AnimationControllerFactory : IArchetypeFactory<IAnimationGameObjectController> {
        private Dictionary<int, int> beatBlockToAnimationTypeMapping;
        private Dictionary<int, IAnimControllerConfigurationScript> animationArcheTypeToControllerFactoryFunctionMapping;
        private ICategoricalObjectPool<AnimationObject> pool;

        public AnimationControllerFactory(Dictionary<int, int> beatBlockToAnimationTypeMapping, Dictionary<int, IAnimControllerConfigurationScript> animationArcheTypeToControllerFactoryFunctionMapping, ICategoricalObjectPool<AnimationObject> pool) {
            this.beatBlockToAnimationTypeMapping = beatBlockToAnimationTypeMapping;
            this.animationArcheTypeToControllerFactoryFunctionMapping = animationArcheTypeToControllerFactoryFunctionMapping;
            this.pool = pool;
        }

        public int NumArchetypes {
            get { return animationArcheTypeToControllerFactoryFunctionMapping.Keys.Count; }
        }

        public IAnimationGameObjectController BuildArchetype(int beatBlockArchetypeId) {
            var configObject = animationArcheTypeToControllerFactoryFunctionMapping[beatBlockToAnimationTypeMapping[beatBlockArchetypeId]];
            return configObject.CreateController(beatBlockToAnimationTypeMapping[beatBlockArchetypeId], pool);
        }
    }

    public class HitboxControllerFactory : IArchetypeFactory<IHitboxGameObjectController> {
        private Dictionary<int, int> beatBlockToHitboxTypeMapping;
        private Dictionary<int, IHitboxControllerConfigurationScript> hbArcheTypeToControllerFactoryFunctionMapping;
        private ICategoricalObjectPool<HitboxObject> pool;

        public HitboxControllerFactory(Dictionary<int, int> beatBlockToAnimationTypeMapping, Dictionary<int, IHitboxControllerConfigurationScript> hbArcheTypeToControllerFactoryFunctionMapping, ICategoricalObjectPool<HitboxObject> pool) {
            this.beatBlockToHitboxTypeMapping = beatBlockToAnimationTypeMapping;
            this.hbArcheTypeToControllerFactoryFunctionMapping = hbArcheTypeToControllerFactoryFunctionMapping;
            this.pool = pool;
        }

        public int NumArchetypes {
            get { return hbArcheTypeToControllerFactoryFunctionMapping.Keys.Count; }
        }

        public IHitboxGameObjectController BuildArchetype(int beatBlockArchetypeId) {
            var configObject = hbArcheTypeToControllerFactoryFunctionMapping[beatBlockToHitboxTypeMapping[beatBlockArchetypeId]];
            return configObject.CreateController(beatBlockToHitboxTypeMapping[beatBlockArchetypeId], pool);
        }
    }

    public class GameManagerScript : MonoBehaviour {

        private const int BEAT_BLOCK_PRE_INIT_SIZE = 50;    // This will be the number of BeatBlocks we pre-initialise for each type, in the object pools
        private const int ANIMATION_OBJ_PRE_INIT_SIZE = 10; // This will be the number of animation gameobjects we pre-initialise for each type, in the object pools
        private const int HITBOX_OBJ_PRE_INIT_SIZE = 10;    // This will be the number of hitbox gameobjects we pre-initialise for each type, in the object pools

        // This script will act as the highest level controller object, which links to the Unity engine, and calls upon the
        // object factories to configure the logic objects, set everything up, and initiate it!
        // It will also serve as the update-flow entry point for all the objects in the system (except the low-level animation
        // GameObjects, which are themselves monobehaviours and thus will update themselves).
        private TrackManager trackManager;

        private IArchetypeFactory<IAnimationGameObjectController> animControllerFactory;
        private IArchetypeFactory<IHitboxGameObjectController> hbControllerFactory;
        private IArchetypeFactory<GameSpaceOccupationOverTimeTemplate> animOccupationFactory = new DUMMYFACTORY<GameSpaceOccupationOverTimeTemplate>();
        private IArchetypeFactory<GameSpaceOccupationOverTimeTemplate> hbOccupationFactory = new DUMMYFACTORY<GameSpaceOccupationOverTimeTemplate>();

        private IObjectPool<AnimationObject> MakeNewPool(AnimationObject prefab) {
            return new QueueBasedObjectPool<AnimationObject>(
                // We must instantiate a clone of the prefab instance as part of the contruction function, and make sure it starts off deactivated
                () => {
                    var toRet = Instantiate(prefab);
                    toRet.gameObject.SetActive(false);
                    return toRet;
                }, ANIMATION_OBJ_PRE_INIT_SIZE
            );
        }
        private IObjectPool<HitboxObject> MakeNewPool(HitboxObject prefab) {
            return new QueueBasedObjectPool<HitboxObject>(
                // We must instantiate a clone of the prefab instance as part of the contruction function, and make sure it starts off deactivated
                () => {
                    var toRet = Instantiate(prefab);
                    toRet.gameObject.SetActive(false);
                    return toRet;
                }, ANIMATION_OBJ_PRE_INIT_SIZE
            );
        }

        public void Start() {
            // Construct all of the factories we will need
            SetupAnimControllerFactory();
            SetupHitboxControllerFactory();
            // SetupAnimOccupationFactory();
            // SetupHitboxOccupationFactory();

            SetupTrackManagerInstance();
        }

        private void SetupHitboxControllerFactory() {
            // Each underlying prefab type defines an underlying 'animation archetype', which beatblock archetypes will map to.
            // We expect some amount of IAnimControllerConfigurationScripts to be attached to this config object, which will define the animation prefabs,
            // and thus define the animation archetypes. Each prefab will also contain data detailing which BeatBlock types use them!
            int currHbArchetypeId = 0;
            Dictionary<int, int> BeatBlockToHitboxTypeMapping = new Dictionary<int, int>();
            Dictionary<int, IHitboxControllerConfigurationScript> HitboxArcheTypeToControllerFactoryFunctionMapping = new Dictionary<int, IHitboxControllerConfigurationScript>();
            List<IObjectPool<HitboxObject>> prefabPools = new List<IObjectPool<HitboxObject>>();

            foreach (IHitboxControllerConfigurationScript config in GetComponents<IHitboxControllerConfigurationScript>()) {
                foreach (HitboxObject prefab in config.GetPrefabs()) {
                    // Each prefab type is pooled individually.
                    var newPool = MakeNewPool(prefab);

                    // Store this new subpool in the global list (which will later be passed into the categorical pool when that is constructed)
                    prefabPools.Insert(currHbArchetypeId, newPool);

                    // Add a mapping refence for all the BeatBlock types which will end up using this.
                    foreach (int bbType in prefab.BeatBlockTypesWhichUseThis) {
                        BeatBlockToHitboxTypeMapping.Add(bbType, currHbArchetypeId);
                    }

                    // Add a mapping reference to the config object capable of creating the correct Controller object for this animation archetype
                    HitboxArcheTypeToControllerFactoryFunctionMapping.Add(currHbArchetypeId, config);

                    // Increment!
                    currHbArchetypeId++;
                }
            }

            // Create the shared pool of animation prefab objects
            ICategoricalObjectPool<HitboxObject> hbPrefabPool = new SimpleCategoricalObjectPool<HitboxObject>(currHbArchetypeId, prefabPools.ToArray());

            // Create the controller factory!
            hbControllerFactory = new HitboxControllerFactory(BeatBlockToHitboxTypeMapping, HitboxArcheTypeToControllerFactoryFunctionMapping, hbPrefabPool);
        }

        private void SetupAnimControllerFactory() {
            // Each underlying prefab type defines an underlying 'animation archetype', which beatblock archetypes will map to.
            // We expect some amount of IAnimControllerConfigurationScripts to be attached to this config object, which will define the animation prefabs,
            // and thus define the animation archetypes. Each prefab will also contain data detailing which BeatBlock types use them!
            int currAnimationArchetypeId = 0;
            Dictionary<int, int> BeatBlockToAnimationTypeMapping = new Dictionary<int, int>();
            Dictionary<int, IAnimControllerConfigurationScript> AnimationArcheTypeToControllerFactoryFunctionMapping = new Dictionary<int, IAnimControllerConfigurationScript>();
            List<IObjectPool<AnimationObject>> prefabPools = new List<IObjectPool<AnimationObject>>();

            foreach (IAnimControllerConfigurationScript config in GetComponents<IAnimControllerConfigurationScript>()) {
                foreach (AnimationObject prefab in config.GetPrefabs()) {
                    // Each prefab type is pooled individually.
                    var newPool = MakeNewPool(prefab);

                    // Store this new subpool in the global list (which will later be passed into the categorical pool when that is constructed)
                    prefabPools.Insert(currAnimationArchetypeId, newPool);

                    // Add a mapping refence for all the BeatBlock types which will end up using this.
                    foreach (int bbType in prefab.BeatBlockTypesWhichUseThis) {
                        BeatBlockToAnimationTypeMapping.Add(bbType, currAnimationArchetypeId);
                    }

                    // Add a mapping reference to the config object capable of creating the correct Controller object for this animation archetype
                    AnimationArcheTypeToControllerFactoryFunctionMapping.Add(currAnimationArchetypeId, config);

                    // Increment!
                    currAnimationArchetypeId++;
                }
            }

            // Create the shared pool of animation prefab objects
            ICategoricalObjectPool<AnimationObject> animPrefabPool = new SimpleCategoricalObjectPool<AnimationObject>(currAnimationArchetypeId, prefabPools.ToArray());

            // Create the Animation controller factory!
            animControllerFactory = new AnimationControllerFactory(BeatBlockToAnimationTypeMapping, AnimationArcheTypeToControllerFactoryFunctionMapping, animPrefabPool);
        }

        private void SetupTrackManagerInstance() {
            /* --- Build the track manager by configuring all the concrete types and supplying the dependencies accordingly --- */
            var concreteTrackFactory = new LayoutTrackFactory();
            var beatBlockArchetypeFactory = new SimpleBeatBlockArchetypeFactory(animControllerFactory, hbControllerFactory, animOccupationFactory, hbOccupationFactory);

            // Build the beatblock type-sepcific object pools
            var concreteSubPools = new QueueBasedObjectPool<BeatBlock>[beatBlockArchetypeFactory.NumArchetypes];
            for (int i = 0; i < beatBlockArchetypeFactory.NumArchetypes; i++) {
                concreteSubPools[i] = new QueueBasedObjectPool<BeatBlock>(() => beatBlockArchetypeFactory.BuildArchetype(i), BEAT_BLOCK_PRE_INIT_SIZE);
            }
            var concreteBeatBlockPool = new SimpleCategoricalObjectPool<BeatBlock>(beatBlockArchetypeFactory.NumArchetypes, concreteSubPools);

            // Instantiate the track manager!
            trackManager = new TrackManager(concreteTrackFactory, concreteBeatBlockPool);
        }

    }
}
