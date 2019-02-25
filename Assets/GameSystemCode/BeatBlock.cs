using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * This file will define the generic and composable 'beat block' class, which will form the basic logic components for all
 * types of 'obstacle block' which the appears on the play-grid, and the player has to dodge. This object defines the BeatBlock
 * class itself, as well as the interfaces for all of the composable elements which will define the behaviours of the beat block.
 */

namespace BeatBlockSystem {
    public class BeatBlock {

        /* NOTE: All 'times' in this code are in units of BEATS, which the game manager can scale and process relative to the current in-game song */
        /* E.g. if the 'HitTime' is 12.5, that means the block will 'hit' the player-plane on the 'and' of 1, in the 4th bar. (the 12.5th beat of the song) */

        /// <summary>
        /// The 'HitTime' stores the time relative to the start of the current song, in beats, that this block will 'hit' the player-plane.
        /// This will be set whenever this beat block is placed into the layout track, and never at any time. It should be illegal to attempt to change the
        /// hit time once it is already placed on the layout track; hence, the setter is private and (TODO) is only used by the method which places it onto the
        /// layout track. 
        /// </summary>
        public float HitTime { get; private set; }


    }
}
