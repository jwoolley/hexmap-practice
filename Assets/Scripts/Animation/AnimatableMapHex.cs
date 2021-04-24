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
            float dtNormal = animationManager.getDtNormalized(cycleDuration);
            float dt = timeTransformer.getAdjustedTime(dtNormal);
            float dz = travelDistance * dt;
            mapHex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, startPos.z + dz);
        }
    }

    public void register(AnimationManager animationManager) {
        animationManager.register(this);
        this.animationManager = animationManager;
    }

    private int getTravelDistance() {
        return (int)(maxTravelDistance - mapHex.gameObject.transform.position.z);
    }

    private UnityMapHex mapHex;
    private bool _isDone;

    private AnimationManager animationManager;
    private TimeTransformer timeTransformer;

    private Vector3 startPos;
    private float elapsedSinceUpdate;
    private float travelDistance;

    // constants
    private float maxTravelDistance = 2.0f;
    private float cycleDuration = 10.0f; // in seconds
    private float updateFreq = 0.0025f;
    private int marginWidth = 5;
}
