using UnityEngine;
using UnityEngine.UI;

public class Animatable : MonoBehaviour, IAnimatable {
    [SerializeField]
    private AnimationManager animationManager;

    [SerializeField]
    private TimeTransformer timeTransformer;

    private int marginWidth = 5;
    private RectTransform rectTransform;

    public void Start() {
        isDone = false;
        rectTransform = gameObject.GetComponent<RectTransform>();
        travelDistance = getTravelDistance();
        startPos = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
        elapsedSinceUpdate = 0f;
        animationManager.register(this);
    }

    private int getTravelDistance() {
        return (int) (transform.parent.GetComponent<RectTransform>().rect.width
         - (GetComponent<Image>().sprite.rect.width + marginWidth));
    }

    private float cycleDuration = 10.0f; // in seconds
    private float travelDistance;

    public bool isDone { get; private set; }

    private float elapsedSinceUpdate;
    private float updateFreq = 0.0025f;

    private Vector2 startPos;

    public void tick() {
        elapsedSinceUpdate += Time.deltaTime;
        if (elapsedSinceUpdate >= updateFreq) {
            elapsedSinceUpdate = 0f;
            float dtNormal = animationManager.getDtNormalized(cycleDuration);
            float dt = timeTransformer.getAdjustedTime(dtNormal);
            float dx = travelDistance * dt;

            rectTransform.anchoredPosition = new Vector2(startPos.x + dx, startPos.y);
        }
    }

    bool IAnimatable.isDone() {
        return this.isDone;
    }

    public void register(AnimationManager animationManager) {
        animationManager.register(this);
    }

    [SerializeField]
    float tVal = 0;

    [SerializeField]
    float tElapsed = 0;

    [SerializeField]
    float dtRaw = 0;

    [SerializeField]
    float dtNormal = 0;
}
