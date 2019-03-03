using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine;
using System.Linq;

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
    /// 2D grid coordinate. Represents a single point on the game grid.
    /// </summary>
    public struct GridPosition {
        public float XPos { get; }
        public float YPos { get; }

        public static GridPosition operator +(GridPosition a, GridPosition b) {
            return new GridPosition(a.XPos + b.XPos, a.YPos + b.YPos);
        }
        public static GridPosition operator -(GridPosition a, GridPosition b) {
            return new GridPosition(a.XPos - b.XPos, a.YPos - b.YPos);
        }

        public GridPosition(float xPos, float yPos) {
            XPos = xPos;
            YPos = yPos;
        }

        /// <summary>
        /// X Radius: tells us half the width of the grid, in game units. The Player-plane grid will always be centred around (0,0,0), so we know that the min
        /// and max X grid values are (-XRadius, +XRadius)
        /// </summary>
        public static float XRadius = 12f;  // TODO: Make this not some silly hardcoded value!

        /// <summary>
        /// Y Radius: tells us half the height of the grid, in game units. The Player-plane grid will always be centred around (0,0,0), so we know that the min
        /// and max Y grid values are (-YRadius, +YRadius)
        /// </summary>
        public static float YRadius = 12f;  // TODO: Make this not some silly hardcoded value!
    }

    /// <summary>
    /// Simple class which represents a 'box' on the 2D player-plane, using the BeatBox local construct of a gridposition.
    /// Stored simply as a top-left and bottom-right point on the grid, such that the rest can be derived.
    /// </summary>
    public class GridBox {
        public GridPosition TopLeftPoint { get; private set; }
        public GridPosition BottomRightPoint { get; private set; }
        public GridBox(GridPosition topleft, GridPosition bottomright) {
            TopLeftPoint = topleft;
            BottomRightPoint = bottomright;
        }
    }

    /// <summary>
    /// This class is designed to represent an object 'occupying space' in game space, such that it can be defined as a template for beatblocks.
    /// Note, that we will want BeatBlocks to be able to sit 'in front' of each other in 3D space as the animation of the Visual GameObject may 
    /// 'come towards' the player-plane from the back of the game space.
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
    public class GameSpaceOccupationTemplate {
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
                throw new ArgumentOutOfRangeException("Tried to get a plane on a grid space occupation object that was illegal");
            }
            return GridSpaceData[index];
        }
        public IEnumerable<GridBox> GetPlaneData(int index, GridPosition offset) {
            if (index < 0 || index >= NumPlanes) {
                throw new ArgumentOutOfRangeException("Tried to get a plane on a grid space occupation object that was illegal");
            }
            else {
                foreach (GridBox b in gridSpaceData) {
                    yield return new GridBox(b.TopLeftPoint + offset, b.BottomRightPoint + offset);
                }
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
    public class GameSpaceOccupationOverTimeTemplate {

        // To model 'occupying space over time' we will just do a simple representation with two arrays. This could become more sophisicated/performant later, but for now:
        // Store a series of GameSpaceOccupations in an array. Each object in the array represents the space occupied for some time window, which will be a subset of 0-1. 
        // Then, a second array contains the time windows for the corresponding GameSpaceOccupation objects.
        // So, for example, spaceOccupied[3] will represent the space occupied for all time values between [ timeWindow[2], timeWindow[3] )
        // spaceOccupied[0] will rep. the space for all time values between [ 0, timeWindow[0] ], since there is no need to store 'zero' at the start of the timeWindow array.
        private GameSpaceOccupationTemplate[] spaceOccupied;
        private float[] timeWindow;

        public GameSpaceOccupationTemplate GetSpaceOccupiedAtTime(float time) {
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

        public GameSpaceOccupationOverTimeTemplate(GameSpaceOccupationTemplate[] spaceOccupied, float[] timeWindow) {
            if (timeWindow.Length != spaceOccupied.Length) { throw new System.ArgumentException("The data arrays must be the same size!"); }
            if (timeWindow[timeWindow.Length - 1] < 1) { throw new System.ArgumentException("The last time window entry must be 1 or greater, to make the internal algorithm work (TODO, fix)"); }

            this.spaceOccupied = spaceOccupied;
            this.timeWindow = timeWindow;
        }
    }

    /// <summary>
    /// This class will act as a public interface object for a GIVEN BeatBlock, and will apply any Beat-block specifc offsets and distortions to a given beat block's
    /// GameSpaceOccupationTemplate object. The template objects will represent unmodified data which can be re-used between beat blocks, whereas, the
    /// BeatBlockGameSpaceOccupation represents the gameSpace occupation of a given beatblock at a given time. This is the object client objects will use.
    /// </summary>
    public class BeatBlockGameSpaceOccupation {
        private Func<GameSpaceOccupationOverTimeTemplate> HitboxTemplateGetter;
        private Func<GameSpaceOccupationOverTimeTemplate> AnimationTemplateGetter;
        private Func<GridPosition> OffsetGetter;

        public BeatBlockGameSpaceOccupation(Func<GameSpaceOccupationOverTimeTemplate> hitboxTemplateGetter, Func<GameSpaceOccupationOverTimeTemplate> animationTemplateGetter, Func<GridPosition> offsetGetter) {
            HitboxTemplateGetter = hitboxTemplateGetter;
            AnimationTemplateGetter = animationTemplateGetter;
            OffsetGetter = offsetGetter;
        }

        public IEnumerable<GridBox> GetHitboxArea(float time, int planeIndex) {
            return HitboxTemplateGetter().GetSpaceOccupiedAtTime(time).GetPlaneData(planeIndex, OffsetGetter());
        }
        public IEnumerable<GridBox> GetAnimationArea(float time, int planeIndex) {
            return AnimationTemplateGetter().GetSpaceOccupiedAtTime(time).GetPlaneData(planeIndex, OffsetGetter());
        }
        public IEnumerable<GridBox> GetTotalArea(float time, int planeIndex) {
            return GetHitboxArea(time, planeIndex).Concat(GetAnimationArea(time, planeIndex));
        }
    }

    public class BeatBlock {

        /* NOTE: All 'times' in this code are in units of BEATS, which the game manager can scale and process relative to the current in-game song */
        /* E.g. if the 'HitTime' is 12.5, that means the block will 'hit' the player-plane on the 'and' of 1, in the 4th bar. (the 12.5th beat of the song) */
        public int BeatBlockTypeId { get; }
        public bool OnLayoutTrack { get; private set; }

        /// <summary>
        /// The 'HitTime' stores the time relative to the start of the current song, in beats, that this block will 'hit' the player-plane.
        /// This will be set whenever this beat block is placed into the layout track, and never at any time. It should be illegal to attempt to change the
        /// hit time once it is already placed on the layout track; hence, the setter is private and (TODO) is only used by the method which places it onto the
        /// layout track. 
        /// 
        /// Settable when placed onto the layout track
        /// </summary>
        public float HitTime { get; private set; }

        /// <summary>
        /// The Speed determines how many beats it takes for the beatblock to hit the player-plane after starting its animation. E.g., with a speed of two, the 
        /// beat block will appear 2 beats before hitting the player-plane
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public float Speed { get; private set; }

        /// <summary>
        /// This is the time when the BeatBlock will appear in the game world. Calculated via the HitTime and Speed.
        /// </summary>
        public float SpawnTime {
            get { return HitTime - Speed; }
        }

        /// <summary>
        /// This controls how quickly the hitbox gameobject animation will playback, as a scale-factor of the objects normal animation speed. This should typically
        /// only be set to one.
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public float HitboxPlaybackSpeedScale { get; }

        /// <summary>
        /// This object will control the speed ramping of the animation playback, converting a linearly interpolation 'time percentage' from start time to hit time
        /// into a customised time percentage to apply to the animation. This is not settable, and should only be configured when the BeatBlock logioc object is
        /// constructed.
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public IAnimationCurve AnimationCurve { get; }

        /// <summary>
        /// Intensity is used to map against the intensity track, to gauge how intense/difficult a given part of the track layout is, by summing the intensities of
        /// nearby beat blocks.
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public float Intensity { get; }

        /// <summary>
        /// Tells the client object if this type of Beat Block can be 'comboed'. If not, this should never be set or read.
        /// </summary>
        public bool Comboable { get; }

        /// <summary>
        /// This property tells us how many 'comboes' this beat block encapsulates. Only applies if this BeatBlock is a composite beat block, where the animation
        /// and gameObject manager logic is actually a chain of sub-beat blocks
        /// 
        /// Settable when placed onto the layout track
        /// </summary>
        public int ComboFactor {
            get {
                if (!Comboable) return 1;
                return comboFactor;
            }
            private set {
                if (!Comboable) throw new ArgumentException("Tried to set the Combo value on a non-comboable beat block");
                else { comboFactor = value; }
            }
        }
        private int comboFactor;

        /// <summary>
        /// This determines which 'layer' this beat block sits on, in the layout track. This can be used to allow beatblocks to overlap in terms of grid occupation if desired,
        /// or if we want to calculate intensity sums per layer. For example, we might want to generate a layout such that slow, sleeping 'laser' beat blocks are on one layer,
        /// and fast moving, short lasting small blocks which fly at the player are on a separate layer. That way, the layout generators which create both of these could have
        /// the option of assessing the intensities and grid occupation of these separately. Should only ever be set when the beat block object is placed onto the layout track.
        /// 
        /// Settable when placed onto the layout track
        /// </summary>
        public int LayoutLayer { get; private set; }

        /// <summary>
        /// Only applicable if this BeatBlock is determined to be 'scalable'. This can be used by the layout track generator or level designer to make adjust the size of an
        /// incoming obstacle, if applicable. (Some blocks might break or act strnagely if scaled, for example if it is animated with the intention of zig sagging across the
        /// entire player-plane width)
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public float SizeScalingFactor {
            get {
                if (!SizeScalable) return 1f;
                else return sizeScalingFactor;
            }
            set {
                if (!SizeScalable) { throw new ArgumentException("Tried to set the Size scaling factor on a non-scalable beat block"); }
                else { sizeScalingFactor = value; }
            }
        }
        private float sizeScalingFactor;
        public bool SizeScalable { get; }

        /// <summary>
        /// Represents the 2D positional offset, from (0,0), that this beat block is sitting on. This should never be set while the BeatBlock exists on the layout track.
        /// 
        /// Settable when placed onto the layout track
        /// </summary>
        public GridPosition GridPosition { get; private set; }

        /// <summary>
        /// This property will return an interface object which allows clients to determine the what sections of the gamespace planes this particular beatblock occupies,
        /// at a given time between 0 and 1, where 0 is the BeatBlock's origin time, and 1 is the BeatBlock's hit time.
        /// 
        /// Defined by BeatBlock Archetype - Thus, only set upon construction
        /// </summary>
        public BeatBlockGameSpaceOccupation GameSpaceAreaOccupation { get; }
        private GameSpaceOccupationOverTimeTemplate hitBoxSpaceOccupation;
        private GameSpaceOccupationOverTimeTemplate animationSpaceOccupation;

        public IAnimationGameObjectController AnimationGameObjectController { get; }

        public IHitboxGameObjectController HitboxGameObjectController { get; }

        /// <summary>
        /// Should be invoked by whichever object is creating the Layout track, whenever this BeatBlock is placed onto a position on the track.
        /// </summary>
        /// <param name="hitTime"></param>
        /// <param name="offsetPosition"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public bool PlacedOnLayoutTrack(float hitTime, GridPosition offsetPosition, float speed, int layoutlayer) {
            this.HitTime = hitTime;
            this.GridPosition = offsetPosition;
            this.Speed = speed;
            this.LayoutLayer = layoutlayer;
            this.OnLayoutTrack = true;
            return true;
        }
        /// <summary>
        /// Should be invoked by whichever object is creating the layout track, and this is a comboable BeatBlock, if the layout generation object wants
        /// to set a non-default combo value.
        /// </summary>
        /// <param name="hitTime"></param>
        /// <param name="offsetPosition"></param>
        /// <param name="speed"></param>
        /// <param name="comboVal"></param>
        /// <returns></returns>
        public bool PlacedOnLayoutTrack(float hitTime, GridPosition offsetPosition, float speed, int layoutLayer, int comboVal) {
            // Note that setting this property will check and throw if this BB is not comboable.
            this.ComboFactor = comboVal;
            return PlacedOnLayoutTrack(hitTime, offsetPosition, speed, layoutLayer);
        }

        /// <summary>
        /// Called by the Game manager when it is time for this BeatBlock to start. I.e. the game time is (HitTime - Speed)
        /// All we need to do here is register for updates from the game timer source, and trigger the animation object to start it's animation!
        /// </summary>
        /// <param name="timerObject"></param>
        /// <returns></returns>
        public bool ActivateBeatBlock(IMasterGameTimer timerObject) {
            timerObject.UpdateTime += UpdateActiveBeatBlockPreHit;
            currTimerObject = timerObject;
            return AnimationGameObjectController.StartAnimation(this.GridPosition, this.SizeScalingFactor, this.Speed, this.ComboFactor);
        }
        private IMasterGameTimer currTimerObject;

        private void UpdateActiveBeatBlockPreHit(float trackTime) {
            // This should only be called when we are an active BeatBlock, and we have not yet triggered as 'hit'
            AnimationGameObjectController.Update(AnimationCurve.MapTimeToAnimationPercentage(trackTime));
            if (trackTime >= HitTime) {
                HitboxGameObjectController.StartAnimation(this.GridPosition, this.SizeScalingFactor, this.HitboxPlaybackSpeedScale);
                currTimerObject.UpdateTime -= UpdateActiveBeatBlockPreHit;
                currTimerObject.UpdateTime += UpdateActiveBeatBlockPostHit;
            }
        }
        private void UpdateActiveBeatBlockPostHit(float trackTime) {
            // This should only be called when we are an active BeatBlock, and we have already triggered as 'hit'
            HitboxGameObjectController.Update(trackTime);
            if (trackTime >= HitTime + HitboxGameObjectController.HitDelayOffset + HitboxGameObjectController.HitboxDuration) {
                // Deregister for timer updates, and tell the master timer we have finished, and can once again become inactive
                currTimerObject.UpdateTime -= UpdateActiveBeatBlockPostHit;
                currTimerObject.SignalCompletion(this);
                currTimerObject = null;
                OnLayoutTrack = false;
            }
        }

        // Most of the properties are configured here.
        public BeatBlock(int beatBlockTypeId, float speed, float hitboxPlaybackSpeedScale, IAnimationCurve animationCurve, float intensity, bool comboable, int comboFactor, int layoutLayer, float sizeScalingFactor, bool sizeScalable, GameSpaceOccupationOverTimeTemplate hitBoxSpaceOccupation, GameSpaceOccupationOverTimeTemplate animationSpaceOccupation, IAnimationGameObjectController animationGameObjectController, IHitboxGameObjectController hitboxGameObjectController) {
            BeatBlockTypeId = beatBlockTypeId;
            Speed = speed;
            HitboxPlaybackSpeedScale = hitboxPlaybackSpeedScale;
            AnimationCurve = animationCurve;
            Intensity = intensity;
            Comboable = comboable;
            this.comboFactor = comboFactor;
            LayoutLayer = layoutLayer;
            this.sizeScalingFactor = sizeScalingFactor;
            SizeScalable = sizeScalable;
            this.hitBoxSpaceOccupation = hitBoxSpaceOccupation;
            this.animationSpaceOccupation = animationSpaceOccupation;
            AnimationGameObjectController = animationGameObjectController;
            HitboxGameObjectController = hitboxGameObjectController;
            OnLayoutTrack = false;

            GameSpaceAreaOccupation = new BeatBlockGameSpaceOccupation(() => this.hitBoxSpaceOccupation, () => this.animationSpaceOccupation, () => this.GridPosition);
        }
    }

    /// <summary>
    /// Interface through which the BeatBlock object can control and trigger the animation gameobject which is attached to it.
    /// </summary>
    public interface IAnimationGameObjectController {
        /// <summary>
        /// Identifies the 'type' of animation this is. Will map to values in some global configuration mapping, which will be shared by the gameObject pooling system.
        /// I.e., when the 'start animation' method is called, this controller will acquire a GameObject from the pool which corresponds with this TypeId!
        /// </summary>
        int AnimationTypeId { get; }

        /// <summary>
        /// This will instantiate an animation-gameobject in the game world on the back-plane with a 'offset' position from (0,0), and start the animation.
        /// This method should always be called by the Beat-Block object when the game-time is equal to (BeatBlock.HitTime - Speed)
        /// </summary>
        /// <param name="offset"> Positional offset (x,y) to spawn the animation object at, at the back plane. </param>
        /// <param name="scalingFactor"> Scaling to apply to the GameObject </param>
        /// <param name="speed"> Informs the gameobject scripts how long its animation will be in total, in case it will need to factor that in </param>
        /// <returns></returns>
        bool StartAnimation(GridPosition offset, float scalingFactor, float speed, int comboFactor);

        /// <summary>
        /// Called every update if and only if the animation object is already alive. At each update, the timeIndex parameter will inform what percentage of the
        /// playback the animation should advance to on this frame. This method should also 'terminate' the object (return it to pool and deactivate it) if it is
        /// called with a timeIndex greater than or equal to 1.
        /// </summary>
        /// <param name="timeIndex"> what percentage of the playback the animation should advance to on this frame.</param>
        /// <returns></returns>
        bool Update(float timeIndex);
    }
    public interface IHitboxGameObjectController {
        int HitboxTypeId { get; }

        float HitDelayOffset { get; }
        float HitboxDuration { get; }

        bool StartAnimation(GridPosition offset, float sizeScalingFactor, float playbackSpeedScalingFactor);

        /// <summary>
        /// Called every update if and only if the hitbox object is already alive.
        /// Note that the hitbox GameObject will update itself through the use of the Unity Engine. This update call is just to constantly inform this Controller
        /// object what the current track time is, in beats.
        /// This method should also 'terminate' the object (return it to pool and deactivate it) if it is
        /// called with a timeIndex greater than or equal to (1 + HitDelayOffset + HitboxDuration)
        /// </summary>
        /// <param name="timeIndex"></param>
        /// <returns></returns>
        bool Update(float timeIndex);
    }
    public interface IMasterGameTimer {
        event Action<float> UpdateTime;
        void SignalCompletion(BeatBlock blockWhichFinished);
    }
}
