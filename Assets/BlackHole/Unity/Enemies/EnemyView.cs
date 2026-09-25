using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 적 시스템의 화면. 매 프레임 World의 살아 있는 적을 읽어 스프라이트를 맞춘다. 게임 상태를 바꾸지 않는다.
    // 외형은 적 종류 에셋(EnemyKind)이 가진다. 스프라이트가 없는 종류는 임시 원으로 그린다.
    // 목록에서 빠진 적(사망)의 스프라이트는 바로 지운다. 파괴·흡수 연출은 연출 작업에서 사망 기록을 읽어 더한다.
    // 규칙 평면은 장면의 z = 0이고 x·y는 같다. HQ(원점)가 장면의 원점이다.
    internal sealed class EnemyView : IDisposable
    {
        private const int DiscPixels = 64;

        private readonly Transform _root;
        private readonly Dictionary<string, EnemyKind> _kinds = new Dictionary<string, EnemyKind>(StringComparer.Ordinal);
        private readonly Dictionary<EnemyId, SpriteRenderer> _views = new Dictionary<EnemyId, SpriteRenderer>();
        private readonly HashSet<EnemyId> _seen = new HashSet<EnemyId>();
        private readonly List<EnemyId> _gone = new List<EnemyId>();
        private readonly Texture2D _discTexture;
        private readonly Sprite _disc;

        public EnemyView(Transform parent, IReadOnlyList<EnemyKind> kinds)
        {
            _root = new GameObject("Enemy View").transform;
            _root.SetParent(parent, false);

            foreach (EnemyKind kind in kinds)
            {
                if (kind != null && !string.IsNullOrEmpty(kind.Id))
                    _kinds[kind.Id] = kind;
            }

            _discTexture = CreateDisc();
            _disc = Sprite.Create(_discTexture, new Rect(0, 0, DiscPixels, DiscPixels), new Vector2(0.5f, 0.5f), DiscPixels);
        }

        public void Synchronize(World world)
        {
            _seen.Clear();
            IReadOnlyList<Enemy> enemies = world.Enemies;

            // 매 프레임 경로: IReadOnlyList를 인덱스로 돈다(인터페이스 foreach는 열거자를 할당한다).
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                _seen.Add(enemy.Id);

                if (!_views.TryGetValue(enemy.Id, out SpriteRenderer view))
                {
                    view = Create(enemy);
                    _views.Add(enemy.Id, view);
                }

                view.transform.localPosition = new Vector3(enemy.Position.X, enemy.Position.Y, 0);
            }

            _gone.Clear();

            foreach (EnemyId id in _views.Keys)
            {
                if (!_seen.Contains(id))
                    _gone.Add(id);
            }

            foreach (EnemyId id in _gone)
            {
                Object.Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        // 판이 바뀌거나 전투 화면을 떠날 때 모든 적 스프라이트를 지운다. 정리는 처치가 아니므로 연출도 없다.
        public void Reset()
        {
            foreach (SpriteRenderer view in _views.Values)
                Object.Destroy(view.gameObject);

            _views.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_disc);
            Object.Destroy(_discTexture);
        }

        private SpriteRenderer Create(Enemy enemy)
        {
            _kinds.TryGetValue(enemy.Definition.Id, out EnemyKind kind);

            var view = new GameObject($"{enemy.Definition.Id} #{enemy.Id.Value}");
            view.transform.SetParent(_root, false);

            var renderer = view.AddComponent<SpriteRenderer>();
            renderer.sprite = kind != null && kind.Sprite != null ? kind.Sprite : _disc;
            renderer.color = kind != null ? kind.Color : Color.white;

            // 크기는 규칙 수치(반지름)를 그대로 쓴다: 스프라이트의 긴 변이 지름이 되게 맞춘다.
            Vector3 bounds = renderer.sprite.bounds.size;
            float longest = Mathf.Max(bounds.x, bounds.y);
            view.transform.localScale = Vector3.one * (enemy.Stats.Size * 2 / longest);

            return renderer;
        }

        // 지름 1 단위(DiscPixels 픽셀)의 흰 원. 가장자리 한 픽셀을 부드럽게 한다.
        private static Texture2D CreateDisc()
        {
            var texture = new Texture2D(DiscPixels, DiscPixels, TextureFormat.RGBA32, false)
            {
                name = "Enemy Disc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            float center = (DiscPixels - 1) / 2f;
            float radius = DiscPixels / 2f - 0.5f;
            var pixels = new Color32[DiscPixels * DiscPixels];

            for (int y = 0; y < DiscPixels; y++)
            {
                for (int x = 0; x < DiscPixels; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255);
                    pixels[y * DiscPixels + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
