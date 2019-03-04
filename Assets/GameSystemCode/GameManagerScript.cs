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
        private const int NUM_BEAT_BLOCK_ARCHETYPES = 20;   // Depends on how many we have made!
        public int NumBeatBlockArchetypes {
            get { return NUM_BEAT_BLOCK_ARCHETYPES; }
        }

        public BeatBlock BuildBeatBlockArchetype(int typeId) {
            // TODO: 9 - IMPLEMENT LOGIC TO CONFIGURE BEAT BLOCKS
            throw new NotImplementedException();
        }
    }

    public class LayoutTrackFactory : ITrackFactory {

        // TODO: This class, when a new layout track is created, should automatically generate

        public ILayoutTrack GetNewLayoutTrack(float beatsPerMinute, int trackLength) {
            return new ListLayoutTrack(trackLength);
        }
    }
}
