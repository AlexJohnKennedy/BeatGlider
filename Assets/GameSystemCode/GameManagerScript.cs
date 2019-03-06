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

        // This script will act as the highest level controller object, which links to the Unity engine, and calls upon the
        // object factories to configure the logic objects, set everything up, and initiate it!
        // It will also serve as the update-flow entry point for all the objects in the system (except the low-level animation
        // GameObjects, which are themselves monobehaviours and thus will update themselves).
        private TrackManager trackManager;
        
        public void Start() {
            // For now, we will just setup a new trackmanager when the script starts. This could optionally be done via some game-triggered call back or
            // something.
            SetupTrackManagerInstance();
        }

        private void SetupTrackManagerInstance() {
            /* --- Build the track manager by configuring all the concrete types and supplying the dependencies accordingly --- */
            var concreteTrackFactory = new LayoutTrackFactory();
            var beatBlockArchetypeFactory = new SimpleBeatBlockArchetypeFactory();

            // Build the beatblock type-sepcific object pools
            var concreteSubPools = new QueueBasedObjectPool<BeatBlock>[beatBlockArchetypeFactory.NumBeatBlockArchetypes];
            for (int i = 0; i < beatBlockArchetypeFactory.NumBeatBlockArchetypes; i++) {
                concreteSubPools[i] = new QueueBasedObjectPool<BeatBlock>(() => beatBlockArchetypeFactory.BuildBeatBlockArchetype(i), BEAT_BLOCK_PRE_INIT_SIZE);
            }
            var concreteBeatBlockPool = new SimpleCategoricalObjectPool<BeatBlock>(beatBlockArchetypeFactory.NumBeatBlockArchetypes, concreteSubPools);

            // Instantiate the track manager!
            trackManager = new TrackManager(concreteTrackFactory, concreteBeatBlockPool);
        }

    }
}

namespace ConfigurationFactories {

    public interface IBeatBlockArchetypeFactory {
        int NumBeatBlockArchetypes { get; }
        BeatBlock BuildBeatBlockArchetype(int typeId);
    }

    public class SimpleBeatBlockArchetypeFactory : IBeatBlockArchetypeFactory {

        public int NumBeatBlockArchetypes {
            get { return Enum.GetNames(typeof(BeatBlockArchetypeValues)).Length; }
        }

        private enum BeatBlockArchetypeValues {
            ONE_BEAT_SMALL_BLOCK = 0,
            TWO_BEAT_SMALL_BLOCK = 1,
            ONE_BEAT_BIG_BLOCK   = 2,
            TWO_BEAT_BIG_BLOCK   = 3
        }
        private enum AnimationArchetypeValues {
            SMALL_BLOCK = 0,
            BIG_BLOCK   = 1
        }

        public SimpleBeatBlockArchetypeFactory() {

            // TODO - finish these configs and make an additional factory pattern which creates an Animation pool, and associated gamespace occupation objects.
            // Pairing them together because a give pool will contain the same animObj prefabs, each of which will contain the same gamespace occupation (as defined by
            // their animations!)

            // TODO - Later, we'll want a better way to configure these objects and build them. We could implement a builder pattern for setting the parameters on
            // a BeatBlock.

            // TODO - Way later, we'll want to develop configuration tools, which allow us to automatically generate the data-objects based on easier means, to facilitate
            // easy and flexible game design.

            builderFuncs = new Func<BeatBlock>[NumBeatBlockArchetypes];
            builderFuncs[(int)BeatBlockArchetypeValues.ONE_BEAT_SMALL_BLOCK] = () => {
                return new BeatBlock((int)BeatBlockArchetypeValues.ONE_BEAT_BIG_BLOCK,
                    1, 1, new DefaultLinearCurve(), 5, false, 1, 1, 1f, false, hitBoxSpaceOccupation, animationSpaceOccupation,
                    new StraightBlockController((int)AnimationArchetypeValues.SMALL_BLOCK, animationObjPool, new Vector3(0, 0, 0), new Vector3(0, 0, 20)),
                    new HitboxGameObjectController());
            };
        }

        private Func<BeatBlock>[] builderFuncs;

        public BeatBlock BuildBeatBlockArchetype(int typeId) {
            if (typeId >= NumBeatBlockArchetypes || typeId < 0) throw new ArgumentOutOfRangeException();
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
