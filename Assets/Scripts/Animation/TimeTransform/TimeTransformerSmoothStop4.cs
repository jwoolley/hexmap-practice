public class TimeTransformerSmoothStop4 : TimeTransformer {
    public override float getAdjustedTime(float t) {
        return 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t);
    }
}