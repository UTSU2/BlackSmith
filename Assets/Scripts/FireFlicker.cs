using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlackSmith
{
    /// <summary>
    /// 화로 불빛이 불규칙하게 일렁이도록 Light2D 세기와 번짐 스프라이트의 투명도를 흔든다.
    /// 둘 다 선택 사항이며, 연결하지 않은 쪽은 건너뛴다.
    /// </summary>
    public class FireFlicker : MonoBehaviour
    {
        [SerializeField] private Light2D fireLight;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private float lightBase = 0.6f;
        [SerializeField] private float lightAmplitude = 0.15f;
        [SerializeField] private float glowBaseAlpha = 0.22f;
        [SerializeField] private float glowAmplitude = 0.08f;
        [SerializeField] private float glowScaleAmplitude = 0.08f;
        [SerializeField] private float speed = 3.5f;

        private float seed;
        private Vector3 glowBaseScale = Vector3.one;

        private void Awake()
        {
            seed = Random.value * 100f;
            if (glow != null)
            {
                glowBaseScale = glow.transform.localScale;
            }
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            // 느린 흔들림과 빠른 흔들림을 섞어 불규칙하게 만든다.
            float n = Mathf.PerlinNoise(t, 0f) * 0.7f + Mathf.PerlinNoise(t * 3.1f, 7.3f) * 0.3f;
            float centered = n * 2f - 1f;

            if (fireLight != null)
            {
                fireLight.intensity = lightBase + centered * lightAmplitude;
            }

            if (glow != null)
            {
                Color c = glow.color;
                c.a = Mathf.Clamp01(glowBaseAlpha + centered * glowAmplitude);
                glow.color = c;
                glow.transform.localScale = glowBaseScale * (1f + centered * glowScaleAmplitude);
            }
        }
    }
}
