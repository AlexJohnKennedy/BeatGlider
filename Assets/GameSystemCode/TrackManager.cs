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

        public TrackManager(ITrackFactory factoryObj) {
            trackFactory = factoryObj;
            isCurrentlyPlayingTrack = false;
        }

        private float CalculateBeatsIntoTrack(float time) {
            return beatsPerMinute / ((time - currentTrackStartTime_GameTime) * 60);
        }

        private ILayoutTrack layoutTrack;
        private ITrackFactory trackFactory;

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
    }
}
