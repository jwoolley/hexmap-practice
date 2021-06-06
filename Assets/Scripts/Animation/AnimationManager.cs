using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour {
    private List<IAnimatable> animatables = new List<IAnimatable>();
    public float elapsed { get; private set; }
    private float velocity;

    public void register(IAnimatable animatable) {
        if (!animatables.Contains(animatable)) {
            animatables.Add(animatable);
        }
    }

    public virtual void Awake() {
        if (StaticAnimationManager.instance == null) {
            StaticAnimationManager.instance = this;
        }
        animatables.Clear();
        elapsed = 0.0f;
        velocity = 1.0f;
    }

    public virtual void Update() {
        // Debug.Log("AnimationManager.Update called");
        animatables.ForEach(animatable => {
            animatable.tick();
        });

        elapsed += Time.deltaTime;

        animatables.RemoveAll(animatable => animatable.isDone());
    }

    public virtual float getDtNormalized(float duration, float startTime=0) {
        return (elapsed - startTime) / duration;
    }
}