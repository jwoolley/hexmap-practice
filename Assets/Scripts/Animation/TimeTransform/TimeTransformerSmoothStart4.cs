public class TimeTransformerSmoothStart4 : TimeTransformer {
    public override float getAdjustedTime(float t) {
        return t * t * t * t;
    }
}