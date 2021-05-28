using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimatableMapHex : IAnimatable {
    public AnimatableMapHex(UnityMapHex mapHex, TimeTransformer timeTransformer) {
        this.mapHex = mapHex;
        this.timeTransformer = timeTransformer;
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
        return Math.Abs(cycleDurationStart - (cycleDurationStart - cycleDurationEnd)
            * timeTransformerForCycleDuration.getAdjustedTime(totalTimeElapsed / timeToMaxSpeed));
    }

    public void tick() {
        elapsedSinceUpdate += Time.deltaTime;
        totalTimeElapsed = Math.Min(totalTimeElapsed + Time.deltaTime, timeToMaxSpeed);
        //totalTimeElapsed += Time.deltaTime;

        if (elapsedSinceUpdate >= updateFreq) {
            elapsedSinceUpdate = 0f;
            //float dtNormal = animationManager.getDtNormalized(cycleDuration, startTime);
            float dtNormal = animationManager.getDtNormalized(getNormalizedCycleDuration(), startTime);

            if (dtNormal >= 1.0f) {
                _isDone = true;
                dtNormal = 1.0f;
            }
            float dt = timeTransformer.getAdjustedTime(dtNormal);
            float dz = travelDistance * dt;
            float newZPos = startPos.z - dz;
            mapHex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, newZPos);
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
    private const float cycleDuration = 0.6f; // in seconds
    private const float _CYCLE_DURATION_START = 0.5f; // in seconds
    private const float _CYCLE_DURATION_END = 0.1f; // in seconds
    private const float _TIME_TO_MAX_SPEED = 16.0f; // in seconds

    private const float _MIN_TIME_TO_MAX_SPEED = 4.0f;
    private const float _MAX_TIME_TO_MAX_SPEED = 10.0f;

    private static float cycleDurationStart = _CYCLE_DURATION_START; // in seconds
    private static float cycleDurationEnd = _CYCLE_DURATION_END; // in seconds
    private static float timeToMaxSpeed = _TIME_TO_MAX_SPEED; // in seconds 

    public static void calibrateCycleTiming(int numLevels) {
        cycleDurationStart = _CYCLE_DURATION_START;
        cycleDurationEnd = _CYCLE_DURATION_END;
        timeToMaxSpeed = Math.Min(Math.Max(numLevels * 1.0f, _MIN_TIME_TO_MAX_SPEED), _MAX_TIME_TO_MAX_SPEED);
    }

    private float updateFreq = 0.0025f;
    private float startTime;
}