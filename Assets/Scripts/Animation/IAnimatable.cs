public interface IAnimatable {
    bool isDone();
    void tick();
    void register(AnimationManager animationManager);
}