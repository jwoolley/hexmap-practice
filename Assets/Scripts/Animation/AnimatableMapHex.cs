using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimatableMapHex : IAnimatable {

    public AnimatableMapHex(UnityMapHex mapHex, TimeTransformer timeTransformer)
        : this(mapHex, timeTransformer, new AnimatableHexCycleTiming())
    { }

    public AnimatableMapHex(UnityMapHex mapHex, TimeTransformer timeTransformer, AnimatableHexCycleTiming cycleTiming) {
        this.mapHex = mapHex;
        this.timeTransformer = timeTransformer;
        this.cycleTiming = cycleTiming;
        this.timeTransformerForCycleDuration = new TimeTransformerSmoothStart2();
        _isDone = false;

        Transform transform = this.mapHex.gameObject.transform;
        travelDistance = getTravelDistance();
        startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        elapsedSinceUpdate = 0f;
    }

    public bool isDone() {
        return _isDone;
    }

    static float totalTimeElapsed = 0.0f;

    private float getNormalizedCycleDuration() {
        return Math.Abs(cycleTiming.durationStart - (cycleTiming.durationStart - cycleTiming.durationEnd)
            * timeTransformerForCycleDuration.getAdjustedTime(totalTimeElapsed / cycleTiming.timeToMaxSpeed));
    }

    public void tick() {
        elapsedSinceUpdate += Time.deltaTime;
        totalTimeElapsed = Math.Min(totalTimeElapsed + Time.deltaTime, cycleTiming.timeToMaxSpeed);
        //totalTimeElapsed += Time.deltaTime;

        if (elapsedSinceUpdate >= updateFreq) {
            elapsedSinceUpdate = 0f;
            float normalizedCycleDuration = getNormalizedCycleDuration();
            float dtNormal = animationManager.getDtNormalized(normalizedCycleDuration, startTime);

            if (dtNormal >= 1.0f) {
                _isDone = true;
                dtNormal = 1.0f;
            }

            float dt = timeTransformer.getAdjustedTime(dtNormal);
            float dz = travelDistance * dt;
            float newZPos = startPos.z - dz;

            if (!float.IsNaN(newZPos)) {
                mapHex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, newZPos);
            }
        }
    }

    public void register(AnimationManager animationManager) {
        animationManager.register(this);
        this.animationManager = animationManager;
        startTime = animationManager.elapsed;
    }

    private int getTravelDistance() {
        return -(int)(maxTravelDistance - mapHex.gameObject.transform.position.z);
    }

    private UnityMapHex mapHex;
    private bool _isDone;

    private AnimationManager animationManager;
    private TimeTransformer timeTransformer;

    private TimeTransformer timeTransformerForCycleDuration;

    private Vector3 startPos;
    private float elapsedSinceUpdate;
    private float travelDistance;

    // constants
    private const float maxTravelDistance = 10.0f;

    private AnimatableHexCycleTiming cycleTiming;

    public class AnimatableHexCycleTiming {
        public const float DEFAULT_MIN_TIME_TO_MAX_SPEED = 4.0f; // in seconds
        public const float DEFAULT_MAX_TIME_TO_MAX_SPEED = 10.0f; // in seconds
        private const float DEFAULT_DURATION_START = 0.5f; // in seconds
        private const float DEFAULT_DURATION_END = 0.1f; // in seconds
        private const float DEFAULT_TIME_TO_MAX_SPEED = 16.0f; // in seconds

        public float durationStart { get; private set; } // in seconds
        public float durationEnd { get; private set; } // in seconds
        public float timeToMaxSpeed { get; private set; } // in seconds

        public AnimatableHexCycleTiming(
            float timeToMaxSpeed = DEFAULT_TIME_TO_MAX_SPEED,
            float durationStart=DEFAULT_DURATION_START,
            float durationEnd = DEFAULT_DURATION_END
        ) {
            this.durationStart = durationStart;
            this.durationStart = durationEnd;
            this.timeToMaxSpeed = timeToMaxSpeed;
        }
    }

    private float updateFreq = 0.0025f;
    private float startTime;
}