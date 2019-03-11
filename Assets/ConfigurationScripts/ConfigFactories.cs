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

    // Map named BeatBlock archetypes to AnimCurve, AnimObj, and HitboxObj archetypes -------------------------------------------
    static class ArchetypeMappings {
        public static int AnimCurve(int beatBlockType) { return animCurves[beatBlockType]; }
        public static Dictionary<int, int> animCurves = new Dictionary<int, int> {
            { BeatBlockArchetypes.ONE_BEAT_SMALL_BLOCK, AnimCurveArchetypes.LINEAR_DEFAULT },
            { BeatBlockArchetypes.TWO_BEAT_SMALL_BLOCK, AnimCurveArchetypes.LINEAR_DEFAULT },
            { BeatBlockArchetypes.ONE_BEAT_BIG_BLOCK, AnimCurveArchetypes.QUADRATIC_DEFAULT },
            { BeatBlockArchetypes.TWO_BEAT_BIG_BLOCK, AnimCurveArchetypes.QUADRATIC_DEFAULT }
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

    public class SimpleBeatBlockArchetypeFactory : IArchetypeFactory<BeatBlock> {

        public int NumArchetypes {
            get { return BeatBlockArchetypes.Length; }
        }

        private BeatBlockBuilder_Recyclable builder;
        private readonly AnimationCurveArchetypeFactory animCurveFactory;

        public SimpleBeatBlockArchetypeFactory(IArchetypeFactory<IAnimationGameObjectController> animControllerFactory, 
                                               IArchetypeFactory<IHitboxGameObjectController> hitboxControllerFactory,
                                               IArchetypeFactory<GameSpaceOccupationOverTimeTemplate> animOccupationFactory,
                                               IArchetypeFactory<GameSpaceOccupationOverTimeTemplate> hitboxOccupationFactory) {

            builder = new BeatBlockBuilder_Recyclable();
            animCurveFactory = new AnimationCurveArchetypeFactory();

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
                    .HitBoxSpaceOccupation(hitboxOccupationFactory.BuildArchetype(blockType))
                    .AnimationSpaceOccupation(animOccupationFactory.BuildArchetype(blockType))
                    .AnimationGameObjectController(animControllerFactory.BuildArchetype(blockType))
                    .HitBoxGameObjectController(hitboxControllerFactory.BuildArchetype(blockType));

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
                    .HitBoxSpaceOccupation(hitboxOccupationFactory.BuildArchetype(blockType))
                    .AnimationSpaceOccupation(animOccupationFactory.BuildArchetype(blockType))
                    .AnimationGameObjectController(animControllerFactory.BuildArchetype(blockType))
                    .HitBoxGameObjectController(hitboxControllerFactory.BuildArchetype(blockType));

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
                    .HitBoxSpaceOccupation(hitboxOccupationFactory.BuildArchetype(blockType))
                    .AnimationSpaceOccupation(animOccupationFactory.BuildArchetype(blockType))
                    .AnimationGameObjectController(animControllerFactory.BuildArchetype(blockType))
                    .HitBoxGameObjectController(hitboxControllerFactory.BuildArchetype(blockType));

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
                    .HitBoxSpaceOccupation(hitboxOccupationFactory.BuildArchetype(blockType))
                    .AnimationSpaceOccupation(animOccupationFactory.BuildArchetype(blockType))
                    .AnimationGameObjectController(animControllerFactory.BuildArchetype(blockType))
                    .HitBoxGameObjectController(hitboxControllerFactory.BuildArchetype(blockType));

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
