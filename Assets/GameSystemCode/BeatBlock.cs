using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
 * This file will define the generic and composable 'beat block' class, which will form the basic logic components for all
 * types of 'obstacle block' which the appears on the play-grid, and the player has to dodge. This object defines the BeatBlock
 * class itself, as well as the interfaces for all of the composable elements which will define the behaviours of the beat block.
 */

namespace BeatBlockSystem {

    /// <summary>
    /// This simply represents an animation playback curve, which is essentially some speed ramping for the animation. We model this as
    /// mapping a 'time' (between 0 and 1, which represents the BeatBlock lifetime as an interval) to some other value between 0 and 1,
    /// representing the how much of the animation has been completed up to that time. I.e. returning 0.75 means that the animation is 75
    /// percent complete by the passed 'time' time. The underlying logic performing this mapping is unknown, but should follow the following
    /// guidelines if possible, to make animating things for sensible and reduce the chance for crazy behaviours:
    /// 
    /// f(0) = 0
    /// f(1) = 1
    /// f(x) > f(y) for all x > y (POSSIBLY NOT, IN STRANGE CASES WHERE WE WANT THE ANIMATION TO OSCILLATE)
    /// f is continuous and conforms to the properties of a mathematical function
    /// 
    /// </summary>
    public interface IAnimationCurve {
        float MapTimeToAnimationPercentage(float time);
    }

    /// <summary>
    /// Interface for accessing a 2D grid coordinate. Represents a single point on the game grid.
    /// Most likely will end up just being a wrapper for some simple data structure,
    /// but put here as an interface to make it a logically abstract component which only relates to the BeatBlock and Player-grid logic system.
    /// </summary>
    public interface IGridPosition {
        float XPos { get; }
        float YPos { get; }

        /// <summary>
        /// X Radius: tells us half the width of the grid, in game units. The Player-plane grid will always be centred around (0,0,0), so we know that the min
        /// and max X grid values are (-XRadius, +XRadius)
        /// </summary>
        float XRadius { get; }

        /// <summary>
        /// Y Radius: tells us half the height of the grid, in game units. The Player-plane grid will always be centred around (0,0,0), so we know that the min
        /// and max Y grid values are (-YRadius, +YRadius)
        /// </summary>
        float YRadius { get; }
    }

    /// <summary>
    /// Simple class which represents a 'box' on the 2D player-plane, using the BeatBox local construct of a gridposition.
    /// Stored simply as a top-left and bottom-right point on the grid, such that the rest can be derived.
    /// </summary>
    public class GridBox {
        public IGridPosition TopLeftPoint { get; private set; }
        public IGridPosition BottomRightPoint { get; private set; }
        public GridBox(IGridPosition topleft, IGridPosition bottomright) {
            TopLeftPoint = topleft;
            BottomRightPoint = bottomright;
        }
    }

    /// <summary>
    /// This class is designed to represent a BeatBlock 'occupying space' in game space. Note, that we will want BeatBlocks to be able to
    /// sit 'in front' of each other in 3D space as the animation of the Visual GameObject may 'come towards' the player-plane from the back of the game space.
    /// for this reason.
    /// 
    /// To represent this in a simple manner, we will divide the Z axis aspect (the depth) of the game-space simply into sections, such that the entire 3D game-space
    /// is modelled as 'n' 2D planes, where the front-most one is the player-plane where all the actual collisions and interactions happen.
    /// 
    /// Plane n: The plane where back-most beatblocks will originate
    /// Plane 0: The front-most plane, i.e. the 'player-plane' where beatblocks will 'hit'
    /// I.e. the higher the 'plane index', the further back the plane in Z-axis space.
    /// 
    /// This class therefore stores a collection of GridBox's for each plane in the grid space.
    /// </summary>
    public class GameSpaceOccupation {
        public int NumPlanes { get; private set; }
        private IEnumerable<GridBox>[] gridSpaceData;
        public IEnumerable<GridBox>[] GridSpaceData {
            get { return gridSpaceData; }
            set {
                NumPlanes = value.Length;
                gridSpaceData = value;
            }
        }
        public IEnumerable<GridBox> GetPlaneData(int index) {
            if (index < 0 || index >= NumPlanes) {
                throw new System.ArgumentOutOfRangeException("Tried to get a plane on a grid space occupation object that was illegal");
            }
            else {
                return GridSpaceData[index];
            }
        }
    }

    /// <summary>
    /// This class models a BeatBlock's game space occupation over the entire time-interval that it is in the game; i.e. the space that it is occupying can change over
    /// time!
    /// 
    /// This class will be asked to provide a game-space-occupation for a GIVEN TIME, passing a time value between zero and one; where zero represents the BeatBlock's
    /// origin time, and one represents the beatblock's hit-time! 
    /// 
    /// </summary>
    public class GameSpaceOccupationOverTime {

        // To model 'occupying space over time' we will just do a simple representation with two arrays. This could become more sophisicated/performant later, but for now:
        // Store a series of GameSpaceOccupations in an array. Each object in the array represents the space occupied for some time window, which will be a subset of 0-1. 
        // Then, a second array contains the time windows for the corresponding GameSpaceOccupation objects.
        // So, for example, spaceOccupied[3] will represent the space occupied for all time values between [ timeWindow[2], timeWindow[3] )
        // spaceOccupied[0] will rep. the space for all time values between [ 0, timeWindow[0] ], since there is no need to store 'zero' at the start of the timeWindow array.
        private GameSpaceOccupation[] spaceOccupied;
        private float[] timeWindow;

        public GameSpaceOccupation GetSpaceOccupiedAtTime(float time) {
            if (time < 0 || time > 1) { throw new System.ArgumentOutOfRangeException("Time intervals must be between 0 and 1. They represent the time inverval between a BeatBlock spawning, and hitting the player-plane"); }
            
            // Since we expect the number of time windows to be very small (around 3 or 4), there's no real point doing a binary search or anything. we can just directly scan
            // through until we find the correct value.
            for (int i=0; i < timeWindow.Length; i++) {
                if (time < timeWindow[i]) {
                    return spaceOccupied[i];
                }
            }
            return spaceOccupied[spaceOccupied.Length - 1];
        }

        public GameSpaceOccupationOverTime(GameSpaceOccupation[] spaceOccupied, float[] timeWindows) {
            if (timeWindows.Length != spaceOccupied.Length) { throw new System.ArgumentException("The data arrays must be the same size!"); }
            if (timeWindows[timeWindows.Length - 1] < 1) { throw new System.ArgumentException("The last time window entry must be 1 or greater, to make the internal algorithm work (TODO, fix)"); }

            this.spaceOccupied = spaceOccupied;
            this.timeWindow = timeWindows;
        }
    }

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

        /// <summary>
        /// The Speed determines how many beats it takes for the beatblock to hit the player-plane after starting its animation. E.g., with a speed of two, the 
        /// beat block will appear 2 beats before hitting the player-plane
        /// </summary>
        public float Speed { get; private set; }

        /// <summary>
        /// This object will control the speed ramping of the animation playback, converting a linearly interpolation 'time percentage' from start time to hit time
        /// into a customised time percentage to apply to the animation. This is not settable, and should only be configured when the BeatBlock logioc object is
        /// constructed.
        /// </summary>
        public IAnimationCurve AnimationCurve { get; }

        /// <summary>
        /// Intensity is used to map against the intensity track, to gauge how intense/difficult a given part of the track layout is, by summing the intensities of
        /// nearby beat blocks.
        /// </summary>
        public float Intensity { get; private set; }

        /// <summary>
        /// Tells the client object if this type of Beat Block can be 'comboed'. If not, this should never be set or read.
        /// </summary>
        public bool Comboable { get; }

        /// <summary>
        /// This property tells us how many 'comboes' this beat block encapsulates. Only applies if this BeatBlock is a composite beat block, where the animation
        /// and gameObject manager logic is actually a chain of sub-beat blocks
        /// </summary>
        public int ComboFactor {
            get {
                if (!Comboable) return 1;
                return comboFactor;
            }
            private set {
                if (!Comboable) throw new System.ArgumentException("Tried to set the Combo value on a non-comboable beat block");
                else { comboFactor = value; }
            }
        }
        private int comboFactor;

        /// <summary>
        /// This determines which 'layer' this beat block sits on, in the layout track. This can be used to allow beatblocks to overlap in terms of grid occupation if desired,
        /// or if we want to calculate intensity sums per layer. For example, we might want to generate a layout such that slow, sleeping 'laser' beat blocks are on one layer,
        /// and fast moving, short lasting small blocks which fly at the player are on a separate layer. That way, the layout generators which create both of these could have
        /// the option of assessing the intensities and grid occupation of these separately. Should only ever be set when the beat block object is placed onto the layout track.
        /// </summary>
        public int LayoutLayer { get; private set; }

        /// <summary>
        /// Only applicable if this BeatBlock is determined to be 'scalable'. This can be used by the layout track generator or level designer to make adjust the size of an
        /// incoming obstacle, if applicable. (Some blocks might break or act strnagely if scaled, for example if it is animated with the intention of zig sagging across the
        /// entire player-plane width)
        /// </summary>
        public float SizeScalingFactor {
            get {
                if (!SizeScalable) return 1f;
                else return sizeScalingFactor;
            }
            set {
                if (!SizeScalable) { throw new System.ArgumentException("Tried to set the Size scaling factor on a non-scalable beat block"); }
                else { sizeScalingFactor = value; }
            }
        }
        private float sizeScalingFactor;
        public bool SizeScalable { get; }

        /// <summary>
        /// Represents the 2D positional offset, from (0,0), that this beat block is sitting on. This should never be set while the BeatBlock exists on the layout track.
        /// </summary>
        public IGridPosition GridPosition { get; private set; }

        // TODO: Define methods for accessing the space occupations for both hitbox, and non-hitbox stuff, and add the ability to inherently apply the beatblock's 'offset position' to that occupation data which is returned.
        private GameSpaceOccupationOverTime hitBoxSpaceOccupation;
        private GameSpaceOccupationOverTime animationSpaceOccupation;

        // TODO: Define interfaces/controller objects which are delgated to handle interaction and management of the GameObject components: VisualGameObjController and HitboxObjectController!
        // (These are complex objects which will ahve to themselves be designed, since most of the game-observable behaviours will be defined through the GameObjects which they manage!)

        // TODO: DEFINE A 'PLACE ON LAYOUT TRACK' METHOD, WHICH IS WHAT IS ALLOWED TO SET ALL THE PRIVATE SETTERS FOR THESE PROPERTIES!
    }
}
