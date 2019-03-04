using BeatBlockSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TrackSystem {

    public interface ITrackFactory {
        ILayoutTrack GetNewLayoutTrack();
    }

    /// <summary>
    /// A generic Object pooling interface, which allows pooling of int-identifiable 'archetypes' of some class.
    /// This allows us to pool different configurations of the same Class-type, and retrieve them accordingly. I.e., this pooling interface
    /// essentially accesses multiple sub-pools in its implementation.
    /// 
    /// This interface can be asked for a pooled object, or pool an object, of any archetype identified by an int which is between 0 and MaxKey.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICategoricalObjectPool<T> {
        T GetObject(int typeId);
        bool PoolObject(T objectToDeactivate, int typeId);
        int MaxKey { get; }
    }

    /// <summary>
    /// This is the controller class which is called by the game-engine. It manages a 'level' which takes the form of a music track.
    /// It will be responsible for managing the beat-blocks which are currently active, and activating beat-blocks from the layout-track
    /// at the appropriate time.
    /// </summary>
    public class TrackManager : MonoBehaviour, IMasterGameTimer {
        private bool isCurrentlyPlayingTrack;
        private float currentTrackStartTime_GameTime;   // For a track which is currently playing, this is the time in GAME TIME units, which the zero'th beat lands on.

        private float beatsPerMinute;   // The beats per minute value for the currently playing track
        private int trackLengthInBeats;

        // Event to tell active BeatBlocks about the current tracktime, in beats.
        public event Action<float> UpdateTime;

        public TrackManager(ITrackFactory factoryObj, ICategoricalObjectPool<BeatBlock> beatBlockPool) {
            trackFactory = factoryObj;
            this.beatBlockPool = beatBlockPool;
            isCurrentlyPlayingTrack = false;
        }

        private float CalculateBeatsIntoTrack(float time) {
            return beatsPerMinute / ((time - currentTrackStartTime_GameTime) * 60);
        }

        private ILayoutTrack layoutTrack;
        private ITrackFactory trackFactory;
        private ICategoricalObjectPool<BeatBlock> beatBlockPool;    // Only used to retire used beatblocks back to the pool!

        public void StartNewTrack(float beatsPerMinute, int trackLength, float gameTimeStartDelay) {
            layoutTrack = trackFactory.GetNewLayoutTrack();
            isCurrentlyPlayingTrack = true;
            this.beatsPerMinute = beatsPerMinute;
            this.trackLengthInBeats = trackLength;

            // When this is called, we are initiating a new track. That means our zero'th beat time will begin at (now + gameTimeStartDelay)
            currentTrackStartTime_GameTime = Time.time + gameTimeStartDelay;
        }

        public void Update() {
            float trackTime_beats = CalculateBeatsIntoTrack(Time.time);

            // Check with the Layout manager which blocks should spawn, and become activated. These blocks will be registered to listen for timer updates.
            foreach (BeatBlock blockToActivate in layoutTrack.GetBeatBlocksToSpawn(trackTime_beats)) {
                blockToActivate.ActivateBeatBlock(this);    // The Beat block will register with us, since we are implementing a Master Timer!
            }

            // Update the active BeatBlocks.
            UpdateTime?.Invoke(trackTime_beats);
        }

        // Callback to signal when an active beatblock has completed its in-game actions and can be returned to a pool for re-use!
        public void SignalCompletion(BeatBlock blockWhichFinished) {
            beatBlockPool.PoolObject(blockWhichFinished, blockWhichFinished.BeatBlockTypeId);
        }
    }
}
