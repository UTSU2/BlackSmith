using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.UI
{
    /// <summary>
    /// 적중 이펙트(파티클 프리팹)를 UI 영역 안의 지정 위치에 띄운다. 미니게임 뷰가 판정 이벤트를 받아 Play를 부른다.
    /// 프리팹은 scalingMode Hierarchy로 만들어 캔버스 배율을 따르고, 재생이 끝나면 stopAction Destroy로 스스로 사라진다.
    /// 이펙트를 바꾸려면 프리팹만 교체하면 된다. (docs/superpowers/specs/2026-11-17-hit-effects-design.md)
    /// </summary>
    public class HitEffectSpawner : MonoBehaviour
    {
        [Tooltip("띄울 파티클 프리팹.")]
        [SerializeField] private ParticleSystem prefab;
        [Tooltip("이펙트를 넣을 부모. 비워 두면 이 오브젝트를 쓴다. Play의 위치는 이 부모의 로컬 좌표다.")]
        [SerializeField] private RectTransform container;

        private readonly List<ParticleSystem> active = new List<ParticleSystem>();

        /// <summary>살아 있는 이펙트 수.</summary>
        public int ActiveCount
        {
            get
            {
                RemoveDestroyed();
                return active.Count;
            }
        }

        /// <summary>container 로컬 위치에 scale 배율로 이펙트를 띄운다. scale이 0 이하이거나 프리팹이 없으면 아무것도 하지 않는다.</summary>
        public void Play(Vector2 localPosition, float scale)
        {
            if (prefab == null || scale <= 0f) return;

            Transform parent = container != null ? container : transform;
            ParticleSystem effect = Instantiate(prefab, parent);
            effect.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one * scale;
            effect.Play(true);

            RemoveDestroyed();
            active.Add(effect);
        }

        /// <summary>떠 있는 이펙트를 모두 즉시 지운다. 미니게임 화면을 닫을 때 부른다.</summary>
        public void Clear()
        {
            foreach (ParticleSystem effect in active)
            {
                if (effect == null) continue;
                // Destroy는 프레임 끝에 처리되므로 먼저 꺼서 바로 보이지 않게 한다.
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
            active.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void RemoveDestroyed()
        {
            active.RemoveAll(effect => effect == null || !effect.gameObject.activeSelf);
        }
    }
}
