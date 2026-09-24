using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 화면 객체만 가진다. 매 프레임 World를 읽어 맞추고, 게임 상태를 바꾸지 않는다(월드를 복사하지 않는다).
    // HQ가 화면의 시각적 중심이다: 카메라는 원점이 아니라 HQ 위치를 본다.
    // 판이 바뀌면 SessionLauncher가 Reset을 먼저 부른다.
    internal sealed class WorldView : IDisposable
    {
        private readonly SamplePresentation _presentation;
        private readonly Camera _camera;
        private readonly Transform _root;
        private readonly Texture2D _texture;
        private readonly Sprite _disc;
        private readonly SpriteRenderer _hq;
        private readonly SpriteRenderer _hqGlow;
        private readonly Dictionary<EnemyId, SpriteRenderer> _enemies = new Dictionary<EnemyId, SpriteRenderer>();
        private readonly HashSet<EnemyId> _seen = new HashSet<EnemyId>();
        private readonly List<EnemyId> _gone = new List<EnemyId>();

        public WorldView(Transform parent, Camera camera, SamplePresentation presentation)
        {
            _presentation = presentation;
            _camera = camera;
            _root = new GameObject("World View").transform;
            _root.SetParent(parent, false);
            _texture = CreateDiscTexture();
            _disc = Sprite.Create(_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            _hqGlow = CreateDisc("HQ Glow", presentation.HqGlowColor, 0);
            _hq = CreateDisc("HQ", presentation.HqColor, 1);
        }

        public void Synchronize(World world)
        {
            SynchronizeHq(world.Hq);
            SynchronizeEnemies(world.Enemies);
        }

        // 이전 판의 Enemy View를 모두 지운다. HQ 원판처럼 판과 무관한 객체는 유지한다.
        public void Reset()
        {
            foreach (SpriteRenderer view in _enemies.Values) Destroy(view);
            _enemies.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_disc);
            Object.Destroy(_texture);
        }

        private void SynchronizeHq(Hq hq)
        {
            Vector3 position = ToScene(hq.Position);
            _camera.transform.position = new Vector3(position.x, position.y, _camera.transform.position.z);
            _hq.transform.position = position;
            _hqGlow.transform.position = position;
            _hq.transform.localScale = Vector3.one * _presentation.HqDisplayDiameter;
            _hqGlow.transform.localScale = Vector3.one * (_presentation.HqDisplayDiameter + _presentation.HqGlowExtra);
        }

        private void SynchronizeEnemies(IReadOnlyList<Enemy> enemies)
        {
            _seen.Clear();
            // 매 프레임 경로: IReadOnlyList를 인덱스로 돈다(인터페이스 foreach는 열거자를 할당한다).
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                _seen.Add(enemy.Id);
                if (!_enemies.TryGetValue(enemy.Id, out SpriteRenderer view))
                {
                    view = CreateDisc(enemy.Definition.Id + " #" + enemy.Id.Value,
                        _presentation.EnemyColor(enemy.Definition.Id), 2);
                    _enemies.Add(enemy.Id, view);
                }
                view.transform.position = ToScene(enemy.Position);
                // 크기는 게임 수치(반지름)를 그대로 쓴다.
                view.transform.localScale = Vector3.one * enemy.Stats.Size * 2;
            }

            // 목록에서 사라진 Enemy의 View를 지운다. 사망 연출은 M4에서 사망 기록을 읽어 따로 한다.
            _gone.Clear();
            foreach (EnemyId id in _enemies.Keys)
                if (!_seen.Contains(id)) _gone.Add(id);
            foreach (EnemyId id in _gone)
            {
                Destroy(_enemies[id]);
                _enemies.Remove(id);
            }
        }

        private static void Destroy(SpriteRenderer view)
        {
            view.gameObject.SetActive(false);
            Object.Destroy(view.gameObject);
        }

        // 규칙 좌표 → Unity 좌표. 규칙 평면은 z = 0이다.
        private static Vector3 ToScene(Point2 point) => new Vector3(point.X, point.Y, 0);

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
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Disc", filterMode = FilterMode.Bilinear };
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
