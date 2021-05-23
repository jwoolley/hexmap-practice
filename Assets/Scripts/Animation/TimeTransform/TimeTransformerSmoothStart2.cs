public class TimeTransformerSmoothStart2 : TimeTransformer {
    public override float getAdjustedTime(float t) {
        return t * t;
    }
}