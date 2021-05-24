using System;
using UnityEngine;
using UnityEngine.UI;

public class AnimatableMapHex : IAnimatable {
    public AnimatableMapHex(UnityMapHex mapHex, TimeTransformer timeTransformer) {
        this.mapHex = mapHex;
        this.timeTransformer = timeTransformer;
        _isDone = false;

        Transform transform = this.mapHex.gameObject.transform;
        travelDistance = getTravelDistance();
        startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        elapsedSinceUpdate = 0f;
    }

    public bool isDone() {
        return _isDone;
    }

    public void tick() {
        elapsedSinceUpdate += Time.deltaTime;
        if (elapsedSinceUpdate >= updateFreq) {
            elapsedSinceUpdate = 0f;
            float dtNormal = animationManager.getDtNormalized(cycleDuration, startTime);
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

    private Vector3 startPos;
    private float elapsedSinceUpdate;
    private float travelDistance;

    // constants
    private float maxTravelDistance = 10.0f;
    private float cycleDuration = 0.75f; // in seconds
    private float updateFreq = 0.0025f;
    private float startTime;
}
