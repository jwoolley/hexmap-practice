public class TimeTransformerSmoothStart3 : TimeTransformer {
    public override float getAdjustedTime(float t) {
        return t * t * t;
    }
}