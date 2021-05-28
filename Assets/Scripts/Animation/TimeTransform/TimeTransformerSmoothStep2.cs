public class TimeTransformerSmoothStep2 : TimeTransformer {
    public override float getAdjustedTime(float t) {
        return ((t) * (t * t) + (1 - t)*(1 - (1 - t) * (1 - t)));
    }
}