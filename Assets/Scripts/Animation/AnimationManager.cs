using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour {
    private List<IAnimatable> animatables = new List<IAnimatable>();
    private float elapsed;
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
        Debug.Log("AnimationManager.Update called");
        animatables.ForEach(animatable => {
            animatable.tick();
        });

        animatables.RemoveAll(animatable => animatable.isDone());
    }

    public virtual float getDtNormalized(float cycleDuration) {
        elapsed += Time.deltaTime;

        if (elapsed > cycleDuration) {
            elapsed -= cycleDuration;
            velocity = -velocity; // THIS IS BAD
        }

        return velocity > 0 ? elapsed / cycleDuration : 1 - elapsed / cycleDuration;
    }
}