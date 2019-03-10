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

namespace GameManagementScripts {
    public class GameManagerScript : MonoBehaviour {

        private const int BEAT_BLOCK_PRE_INIT_SIZE = 50;    // This will be the number of BeatBlocks we pre-initialise for each type, in the object pools
        private const int ANIMATION_OBJ_PRE_INIT_SIZE = 10; // This will be the number of animation gameobjects we pre-initialise for each type, in the object pools
        private const int HITBOX_OBJ_PRE_INIT_SIZE = 10;    // This will be the number of hitbox gameobjects we pre-initialise for each type, in the object pools

        // Publicly in-editor settable prefabs, which will allow us to link which Unity prefab elements relate to which AnimObj archetype id's
        public AnimationObject BigBlockAnimationObjectPrefab;
        public AnimationObject SmallBlockAnimationObjectPrefab;

        // This script will act as the highest level controller object, which links to the Unity engine, and calls upon the
        // object factories to configure the logic objects, set everything up, and initiate it!
        // It will also serve as the update-flow entry point for all the objects in the system (except the low-level animation
        // GameObjects, which are themselves monobehaviours and thus will update themselves).
        private TrackManager trackManager;
        
        public void Start() {
            // Set up a mapping for each AnimationObject type, to the linked prefabs. This allows our AnimationObjectController Factories to access the prefab type.
            Dictionary<int, AnimationObject> animMap = new Dictionary<int, AnimationObject> {
                { AnimObjArchetypes.SMALL_BLOCK, SmallBlockAnimationObjectPrefab },
                { AnimObjArchetypes.BIG_BLOCK, BigBlockAnimationObjectPrefab }
            };
            
            // For now, we will just setup a new trackmanager when the script starts. This could optionally be invoked via some game-triggered call back in future
            SetupTrackManagerInstance(animMap, ANIMATION_OBJ_PRE_INIT_SIZE);
        }

        private void SetupTrackManagerInstance(Dictionary<int, AnimationObject> prefabMap, int animpreinitNum) {
            /* --- Build the track manager by configuring all the concrete types and supplying the dependencies accordingly --- */
            var concreteTrackFactory = new LayoutTrackFactory();
            var beatBlockArchetypeFactory = new SimpleBeatBlockArchetypeFactory(prefabMap, animpreinitNum);

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

namespace ConfigurationFactories {

    // Associate names for the Archetypes IDs so configuration is easier --------------------------------------------------------
    static class BeatBlockArchetypes {
        public const int ONE_BEAT_SMALL_BLOCK = 0;
        public const int TWO_BEAT_SMALL_BLOCK = 1;
        public const int ONE_BEAT_BIG_BLOCK = 2;
        public const int TWO_BEAT_BIG_BLOCK = 3;

        public static int Length { get { return 4; } }
    }
    static class AnimCurveArchetypes {
        public const int LINEAR_DEFAULT = 0;
        public const int QUADRATIC_DEFAULT = 1;

        public static int Length { get { return 2; } }
    }
    static class AnimObjArchetypes {
        public const int SMALL_BLOCK = 0;
        public const int BIG_BLOCK = 1;

        public static int Length { get { return 2; } }
    }
    static class HitboxObjArchetypes {
        public const int SMALL_SQUARE = 0;
        public const int BIG_SQUARE = 1;

        public static int Length { get { return 2; } }
    }

    // Map named BeatBlock archetypes to AnimCurve, AnimObj, and HitboxObj archetypes -------------------------------------------
    static class ArchetypeMappings {
        public static int AnimCurve(int beatBlockType) { return animCurves[beatBlockType]; }
        public static Dictionary<int, int> animCurves = new Dictionary<int, int> {
            { BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK, AnimCurveArchetypes.LINEAR_DEFAULT },
            { BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK, AnimCurveArchetypes.LINEAR_DEFAULT },
            { BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK, AnimCurveArchetypes.QUADRATIC_DEFAULT },
            { BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK, AnimCurveArchetypes.QUADRATIC_DEFAULT }
        };

        public static int AnimObj(int beatBlockType) { return animObjs[beatBlockType]; }
        public static Dictionary<int, int> animObjs = new Dictionary<int, int> {
            { BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK, AnimObjArchetypes.SMALL_BLOCK },
            { BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK, AnimObjArchetypes.SMALL_BLOCK },
            { BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK, AnimObjArchetypes.BIG_BLOCK },
            { BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK, AnimObjArchetypes.BIG_BLOCK }
        };

        public static int HitboxObj(int beatBlockType) { return hbObjs[beatBlockType]; }
        public static Dictionary<int, int> hbObjs = new Dictionary<int, int> {
            { BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK, HitboxObjArchetypes.SMALL_SQUARE },
            { BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK, HitboxObjArchetypes.SMALL_SQUARE },
            { BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK, HitboxObjArchetypes.BIG_SQUARE },
            { BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK, HitboxObjArchetypes.BIG_SQUARE }
        };
    }


    // Builder Type for BeatBlocks ----------------------------------------------------------------------------------------------
    public class BeatBlockBuilder_Recyclable {
        private int typeId;                                                     // 0
        public BeatBlockBuilder_Recyclable TypeId(int id) {
            setchecks[0] = true;
            typeId = id;
            return this;
        }

        private float speed;                                                    // 1
        public BeatBlockBuilder_Recyclable Speed(float s) {
            setchecks[1] = true;
            speed = s;
            return this;
        }

        private float hitboxPlaybackSpeed;                                      // 2
        public BeatBlockBuilder_Recyclable HitboxPlaybackSpeed(float v) {
            setchecks[2] = true;
            hitboxPlaybackSpeed = v;
            return this;
        }

        private float intensity;                                                // 3
        public BeatBlockBuilder_Recyclable Intensity(float i) {
            setchecks[3] = true;
            intensity = i;
            return this;
        }

        private bool comboable;                                                 // 4
        public BeatBlockBuilder_Recyclable Comboable(bool c) {
            setchecks[4] = true;
            comboable = c;
            return this;
        }

        private int combofactor;                                                // 5
        public BeatBlockBuilder_Recyclable ComboFactor(int f) {
            setchecks[5] = true;
            combofactor = f;
            return this;
        }

        private IAnimationCurve animationCurve;                                 // 6
        public BeatBlockBuilder_Recyclable AnimationCurve(IAnimationCurve c) {
            setchecks[6] = true;
            animationCurve = c;
            return this;
        }

        private int layoutLayer;                                                // 7
        public BeatBlockBuilder_Recyclable LayoutLayer(int l) {
            setchecks[7] = true;
            layoutLayer = l;
            return this;
        }

        private float sizeScalingFactor;                                        // 8
        public BeatBlockBuilder_Recyclable SizeScalingFactor(float s) {
            setchecks[8] = true;
            sizeScalingFactor = s;
            return this;
        }

        private bool sizeScalable;                                              // 9
        public BeatBlockBuilder_Recyclable SizeScalable(bool s) {
            setchecks[9] = true;
            sizeScalable = s;
            return this;
        }

        private GameSpaceOccupationOverTimeTemplate hitBoxSpaceOccupation;      // 10
        public BeatBlockBuilder_Recyclable HitBoxSpaceOccupation(GameSpaceOccupationOverTimeTemplate t) {
            setchecks[10] = true;
            hitBoxSpaceOccupation = t;
            return this;
        }

        private GameSpaceOccupationOverTimeTemplate animationSpaceOccupation;   // 11
        public BeatBlockBuilder_Recyclable AnimationSpaceOccupation(GameSpaceOccupationOverTimeTemplate t) {
            setchecks[11] = true;
            animationSpaceOccupation = t;
            return this;
        }

        private IAnimationGameObjectController animationGameObjectController;   // 12
        public BeatBlockBuilder_Recyclable AnimationGameObjectController(IAnimationGameObjectController anim) {
            setchecks[12] = true;
            animationGameObjectController = anim;
            return this;
        }

        private IHitboxGameObjectController hitboxGameObjectController;         // 13
        public BeatBlockBuilder_Recyclable HitBoxGameObjectController(IHitboxGameObjectController t) {
            setchecks[13] = true;
            hitboxGameObjectController = t;
            return this;
        }

        private string[] names = {
            "BeatBlock archetype Id",
            "Speed",
            "Hitbox playback speed",
            "Intensity",
            "Combo-able flag",
            "Combo factor",
            "Animation Curve object",
            "Layout layer",
            "Size scaling factor",
            "Size scalable flag",
            "Hitbox space occupation object",
            "Animation space occupation object",
            "Animation gameObj Controller",
            "Hitbox gameObj Controller"
        };

        private bool[] setchecks;

        public BeatBlockBuilder_Recyclable() {
            setchecks = new bool[14];
            ResetBuilder();
        }

        public void ResetBuilder() {
            for (int i = 0; i < 14; i++) setchecks[i] = false;
        }

        public void ThrowIfNotAllParamtersAreSet() {
            bool allSet = true;
            string msg = "Error building a BeatBlock. The following parameters were not set: ";

            int i = 0;
            while (i < 14) {
                if (!setchecks[i]) {
                    allSet = false;
                    msg = msg + names[i];
                    i++;
                    break;
                }
                i++;
            }
            if (!allSet) {
                while (i < 14) {
                    if (!setchecks[i]) {
                        msg = msg + ", " + names[i];
                    }
                }
                throw new InvalidOperationException(msg);
            }
        }

        public BeatBlock Build() {
            ThrowIfNotAllParamtersAreSet();
            return new BeatBlock(typeId, speed, hitboxPlaybackSpeed, animationCurve, intensity, comboable, combofactor, layoutLayer, sizeScalingFactor, sizeScalable,
                hitBoxSpaceOccupation, animationSpaceOccupation, animationGameObjectController, hitboxGameObjectController);
        }
    }
    
    // Define Archetype Factories, which build and configure the archetypes which are named above -------------------------------
    public interface IArchetypeFactory<T> {
        int NumArchetypes { get; }
        T BuildArchetype(int typeId);
    }
    public interface IGameSpaceTemplateFactory<T> : IArchetypeFactory<T> {
        GameSpaceOccupationOverTimeTemplate GetTemplate(int typeId); 
    }

    public class AnimationCurveArchetypeFactory : IArchetypeFactory<IAnimationCurve> {
        
        public int NumArchetypes {
            get { return AnimCurveArchetypes.Length; }
        }

        private Func<IAnimationCurve>[] builderFuncs;

        public AnimationCurveArchetypeFactory() {
            builderFuncs = new Func<IAnimationCurve>[NumArchetypes];
            builderFuncs[AnimCurveArchetypes.LINEAR_DEFAULT] = () => new DefaultLinearCurve();
            builderFuncs[AnimCurveArchetypes.QUADRATIC_DEFAULT] = () => new DefaultQuadraticCurve();
        }

        public IAnimationCurve BuildArchetype(int typeId) {
            if (typeId >= NumArchetypes || typeId < 0) throw new ArgumentOutOfRangeException();
            return builderFuncs[typeId]();
        }
    }

    public class AnimationGameObjectArchetypeFactory : IGameSpaceTemplateFactory<IAnimationGameObjectController> {

        public int NumArchetypes { get { return AnimObjArchetypes.Length; } }

        private GameSpaceOccupationOverTimeTemplate[] templates;
        private ICategoricalObjectPool<AnimationObject> sharedPool;

        public AnimationGameObjectArchetypeFactory(Dictionary<int, AnimationObject> prefabMap, int preinitNum) {
            templates = new GameSpaceOccupationOverTimeTemplate[NumArchetypes];

            // Create the Categorical object pool which the AnimationObjectControllers will source their objects from.
            IObjectPool<AnimationObject>[] pools = new IObjectPool<AnimationObject>[AnimObjArchetypes.Length];
            for (int i=0; i < AnimObjArchetypes.Length; i++) {
                pools[i] = new QueueBasedObjectPool<AnimationObject>(
                    // We must instantiate a clone of the prefab instance as part of the contruction function, and make sure it starts off deactivated
                    () => {
                        var toRet = UnityEngine.Object.Instantiate(prefabMap[i]);
                        toRet.gameObject.SetActive(false);
                        return toRet;
                    }, preinitNum
                );
            }
            sharedPool = new SharableSubpoolCategoricalObjectPool<AnimationObject>(ArchetypeMappings.animObjs, pools);
        }

        public IAnimationGameObjectController BuildArchetype(int typeId) {
            if (typeId >= NumArchetypes || typeId < 0) throw new ArgumentOutOfRangeException();
            return new StraightBlockController(typeId, sharedPool, new Vector3(0, 0, 0), new Vector3(0, 0, 20));
        }

        public GameSpaceOccupationOverTimeTemplate GetTemplate(int typeId) {
            // TODO: Define templates for the object occupation objects
        }
    }

    public class HitboxGameObjectArchetypeFactory : IGameSpaceTemplateFactory<IHitboxGameObjectController> {
        // TODO
        public int NumArchetypes {
            get {
                throw new NotImplementedException();
            }
        }

        public IHitboxGameObjectController BuildArchetype(int typeId) {
            throw new NotImplementedException();
        }

        public GameSpaceOccupationOverTimeTemplate GetTemplate(int typeId) {
            throw new NotImplementedException();
        }
    }

    public class SimpleBeatBlockArchetypeFactory : IArchetypeFactory<BeatBlock> {

        public int NumArchetypes {
            get { return BeatBlockArchetypes.Length; }
        }

        private BeatBlockBuilder_Recyclable builder;
        private readonly AnimationCurveArchetypeFactory animCurveFactory;
        private readonly AnimationGameObjectArchetypeFactory animObjFactory;
        private readonly HitboxGameObjectArchetypeFactory hitboxObjFactory;

        public SimpleBeatBlockArchetypeFactory(Dictionary<int, AnimationObject> animPrefabMap, int animPreinitNum) {
            builder = new BeatBlockBuilder_Recyclable();
            animCurveFactory = new AnimationCurveArchetypeFactory();
            animObjFactory = new AnimationGameObjectArchetypeFactory(animPrefabMap, animPreinitNum);
            // TODO: Implement HitboxGameObjectArchetypeFactory
            hitboxObjFactory = new HitboxGameObjectArchetypeFactory();

            builderFuncs = new Func<BeatBlock>[NumArchetypes];
            builderFuncs[BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK] = () => {
                int blockType = BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK;

                // Make sure we don't have carried over properties
                builder.ResetBuilder();

                // Set properties for the beat block type
                builder.TypeId(blockType)
                    .Speed(1)
                    .HitboxPlaybackSpeed(1)
                    .Intensity(5)
                    .AnimationCurve(animCurveFactory.BuildArchetype(ArchetypeMappings.AnimCurve(blockType)))
                    .Comboable(false)
                    .ComboFactor(1)
                    .LayoutLayer(1)
                    .SizeScalable(false)
                    .SizeScalingFactor(1)
                    .HitBoxSpaceOccupation(hitboxObjFactory.GetTemplate(ArchetypeMappings.HitboxObj(blockType)))
                    .AnimationSpaceOccupation(animObjFactory.GetTemplate(ArchetypeMappings.AnimObj(blockType)))
                    .AnimationGameObjectController(animObjFactory.BuildArchetype(ArchetypeMappings.AnimObj(blockType)))
                    .HitBoxGameObjectController(hitboxObjFactory.BuildArchetype(ArchetypeMappings.HitboxObj(blockType)));

                return builder.Build();
            };
            builderFuncs[BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK] = () => {
                int blockType = BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK;

                // Make sure we don't have carried over properties
                builder.ResetBuilder();

                // Set properties for the beat block type
                builder.TypeId(blockType)
                    .Speed(2)
                    .HitboxPlaybackSpeed(1)
                    .Intensity(2)
                    .AnimationCurve(animCurveFactory.BuildArchetype(ArchetypeMappings.AnimCurve(blockType)))
                    .Comboable(false)
                    .ComboFactor(1)
                    .LayoutLayer(1)
                    .SizeScalable(false)
                    .SizeScalingFactor(1)
                    .HitBoxSpaceOccupation(hitboxObjFactory.GetTemplate(ArchetypeMappings.HitboxObj(blockType)))
                    .AnimationSpaceOccupation(animObjFactory.GetTemplate(ArchetypeMappings.AnimObj(blockType)))
                    .AnimationGameObjectController(animObjFactory.BuildArchetype(ArchetypeMappings.AnimObj(blockType)))
                    .HitBoxGameObjectController(hitboxObjFactory.BuildArchetype(ArchetypeMappings.HitboxObj(blockType)));

                return builder.Build();
            };
            builderFuncs[BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK] = () => {
                int blockType = BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK;

                // Make sure we don't have carried over properties
                builder.ResetBuilder();

                // Set properties for the beat block type
                builder.TypeId(blockType)
                    .Speed(1)
                    .HitboxPlaybackSpeed(1)
                    .Intensity(7)
                    .AnimationCurve(animCurveFactory.BuildArchetype(ArchetypeMappings.AnimCurve(blockType)))
                    .Comboable(false)
                    .ComboFactor(1)
                    .LayoutLayer(1)
                    .SizeScalable(false)
                    .SizeScalingFactor(1)
                    .HitBoxSpaceOccupation(hitboxObjFactory.GetTemplate(ArchetypeMappings.HitboxObj(blockType)))
                    .AnimationSpaceOccupation(animObjFactory.GetTemplate(ArchetypeMappings.AnimObj(blockType)))
                    .AnimationGameObjectController(animObjFactory.BuildArchetype(ArchetypeMappings.AnimObj(blockType)))
                    .HitBoxGameObjectController(hitboxObjFactory.BuildArchetype(ArchetypeMappings.HitboxObj(blockType)));

                return builder.Build();
            };
            builderFuncs[BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK] = () => {
                int blockType = BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK;

                // Make sure we don't have carried over properties
                builder.ResetBuilder();

                // Set properties for the beat block type
                builder.TypeId(blockType)
                    .Speed(2)
                    .HitboxPlaybackSpeed(1)
                    .Intensity(3)
                    .AnimationCurve(animCurveFactory.BuildArchetype(ArchetypeMappings.AnimCurve(blockType)))
                    .Comboable(false)
                    .ComboFactor(1)
                    .LayoutLayer(1)
                    .SizeScalable(false)
                    .SizeScalingFactor(1)
                    .HitBoxSpaceOccupation(hitboxObjFactory.GetTemplate(ArchetypeMappings.HitboxObj(blockType)))
                    .AnimationSpaceOccupation(animObjFactory.GetTemplate(ArchetypeMappings.AnimObj(blockType)))
                    .AnimationGameObjectController(animObjFactory.BuildArchetype(ArchetypeMappings.AnimObj(blockType)))
                    .HitBoxGameObjectController(hitboxObjFactory.BuildArchetype(ArchetypeMappings.HitboxObj(blockType)));

                return builder.Build();
            };
        }

        private Func<BeatBlock>[] builderFuncs;

        public BeatBlock BuildArchetype(int typeId) {
            if (typeId >= NumArchetypes || typeId < 0) throw new ArgumentOutOfRangeException();
            return builderFuncs[typeId]();
        }
    }

    public class LayoutTrackFactory : ITrackFactory {

        // TODO: A version of an ITrackFactory which: when a new layout track is created automatically creates associated intensity amd rhythm tracks,
        // and links them up with an (state stored) Level generator. The idea here, is that the level generator object will have the capability to do a
        // search algorithm on a background thread, which populates the same layer track which the game-thread is actively reading from. To do this, the level
        // generator will be reading data from the associated intensity and rhythm tracks - which themselves might be being generating in real-time also!

        public ILayoutTrack GetNewLayoutTrack(float beatsPerMinute, int trackLength) {
            return new ListLayoutTrack(trackLength);
        }
    }
}
