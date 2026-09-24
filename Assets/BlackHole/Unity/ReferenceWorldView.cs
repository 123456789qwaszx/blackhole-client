using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 화면 객체만 소유한다. Destroy와 보상/사망 판정은 연결하지 않는다.
    internal sealed class ReferenceWorldView : IDisposable
    {
        private readonly Transform _root;
        private readonly Sprite _disc;
        private readonly Texture2D _texture;
        private readonly SpriteRenderer _hole;
        private readonly SpriteRenderer _halo;
        private readonly SpriteRenderer _cast;
        private readonly Dictionary<int, SpriteRenderer> _targets = new Dictionary<int, SpriteRenderer>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _removed = new List<int>();

        public ReferenceWorldView(Transform parent)
        {
            _root = new GameObject("Reference Visuals").transform;
            _root.SetParent(parent, false);
            _texture = CreateDiscTexture();
            _disc = Sprite.Create(_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            _halo = CreateDisc("Accretion Glow", new Color(0.38f, 0.18f, 0.8f), 0);
            _hole = CreateDisc("Black Hole", new Color(0.005f, 0.005f, 0.012f), 1);
            _cast = CreateDisc("Cast Area", new Color(0.4f, 0.7f, 1, 0.2f), 3);
        }

        public void Synchronize(Playfield field, Vector2 aim, float castRadius, bool showCast)
        {
            float diameter = field.Growth.AbsorptionRadius * 2;
            _hole.transform.localScale = Vector3.one * diameter;
            _halo.transform.localScale = Vector3.one * (diameter + 0.22f);
            _cast.enabled = showCast;
            _cast.transform.position = new Vector3(aim.x, aim.y, 0);
            _cast.transform.localScale = Vector3.one * castRadius * 2;
            _seen.Clear();

            foreach (TargetState target in field.Targets)
            {
                _seen.Add(target.Id);
                if (!_targets.TryGetValue(target.Id, out SpriteRenderer view))
                {
                    view = CreateDisc(target.Definition.Id + " #" + target.Id, Color.white, 2);
                    _targets.Add(target.Id, view);
                }
                Point2 position = target.Position;
                view.transform.position = new Vector3(position.X, position.Y, 0);
                // 표현 메타데이터는 이번 두 콘텐츠를 위한 샘플이다. 게임 규칙에는 들어가지 않는다.
                bool heavy = target.Definition.Id == "heavy";
                view.transform.localScale = Vector3.one * (heavy ? 0.55f : 0.32f);
                Color baseColor = heavy ? new Color(1, 0.55f, 0.23f) : new Color(0.2f, 0.9f, 0.95f);
                view.color = target.Phase == TargetPhase.Defeated
                    ? new Color(0.42f, 0.44f, 0.5f)
                    : Color.Lerp(baseColor * 0.4f, baseColor, target.Health / target.Definition.MaxHealth);
            }

            _removed.Clear();
            foreach (int id in _targets.Keys)
                if (!_seen.Contains(id)) _removed.Add(id);
            foreach (int id in _removed)
            {
                _targets[id].gameObject.SetActive(false);
                Object.Destroy(_targets[id].gameObject);
                _targets.Remove(id);
            }
        }

        public void ClearTargets()
        {
            foreach (SpriteRenderer view in _targets.Values)
            {
                view.gameObject.SetActive(false);
                Object.Destroy(view.gameObject);
            }
            _targets.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_disc);
            Object.Destroy(_texture);
        }

        private SpriteRenderer CreateDisc(string name, Color color, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = _disc;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Texture2D CreateDiscTexture()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "Reference Disc";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float radius = new Vector2(x - 31.5f, y - 31.5f).magnitude;
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(31.5f - radius));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
