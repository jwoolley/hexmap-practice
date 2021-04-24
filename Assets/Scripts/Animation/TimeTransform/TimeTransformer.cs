using UnityEngine;

abstract public class TimeTransformer {
    public TimeTransformer() {
    }
    abstract public float getAdjustedTime(float t);
}