using BeatBlockSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackSystem {

    public interface ILayoutTrack {
        bool PlaceBlockOnTrack(BeatBlock block, float hitTime, GridPosition offset, float speed, int layer);
        IEnumerable<BeatBlock> GetBeatBlocksToSpawn(float trackTime);
    }

    /// <summary>
    /// This is the class which represents which BeatBlocks and Obstacles to spawn, at what time relative to the song. Thus, it essentially defines
    /// a 'level'.
    /// The Game manager will look at this each update to determine what BeatBlocks (and other potential level elements) should be spawned at the current time!
    /// </summary>
    public class ListLayoutTrack : ILayoutTrack {
        private const int MAX_POSSIBLE_BLOCKS_PER_TRACK = 1000;
        
        // We will represent the layout track simply as a list of BeatBlocks, ordered by spawn time relative to the song.
        // Thus, the sorting key will be (BeatBlock.HitTime - BeatBlock.Speed) = Origin time.
        private List<BeatBlock> trackData;
        private int frontOfList;

        public ListLayoutTrack() {
            trackData = new List<BeatBlock>(MAX_POSSIBLE_BLOCKS_PER_TRACK);
            frontOfList = 0;
        }

        public bool PlaceBlockOnTrack(BeatBlock block, float hitTime, GridPosition offset, float speed, int layer) {
            float key = hitTime - speed;

            // The way this class is designed, the key should never be less than the item at the front of the list, because Blocks with
            // those keys will have already been popped and placed into the game-world.
            // The front of the list will be indicated by the 'front' pointer, to avoid data-reshuffles.
            int i = frontOfList;
            if (frontOfList >= MAX_POSSIBLE_BLOCKS_PER_TRACK) {
                throw new ArgumentOutOfRangeException("Added too many BeatBlocks to the layout track! Either increase the hardcoded MAX_BLOCKS or add fewer blocks to this track");
            }
            while (i < trackData.Count && key > trackData[i].SpawnTime) {
                /* Do nothing, we are just searching for the correct spot */
                i++;
            }
            trackData.Insert(i, block);
            return block.PlacedOnLayoutTrack(hitTime, offset, speed, layer);
        }

        public IEnumerable<BeatBlock> GetBeatBlocksToSpawn(float trackTime) {
            int i = frontOfList;
            while (i < trackData.Count && trackTime >= trackData[i].SpawnTime) {
                yield return trackData[i];
                frontOfList++;
                i++;
            }
            // The same BeatBlock should never be yielded twice, unless it has been re-placed (i.e. recycled) on the layout track.
        }
    }
}
