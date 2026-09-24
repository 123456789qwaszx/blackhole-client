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
        private const int DiscPixels = 64;
        private const int RingPixels = 128;

        private readonly SamplePresentation _presentation;
        private readonly Camera _camera;
        private readonly Transform _root;
        private readonly Texture2D _discTexture;
        private readonly Texture2D _ringTexture;
        private readonly Sprite _disc;
        private readonly Sprite _ring;
        private readonly SpriteRenderer _hq;
        private readonly SpriteRenderer _hqGlow;
        private readonly Dictionary<EnemyId, EnemyView> _enemies = new Dictionary<EnemyId, EnemyView>();
        private readonly HashSet<EnemyId> _seen = new HashSet<EnemyId>();
        private readonly List<EnemyId> _gone = new List<EnemyId>();
        // Skill은 판 안에서 사라지지 않는다. 판이 바뀌면 Reset이 지운다.
        private readonly Dictionary<PassiveSkill, SkillView> _skills = new Dictionary<PassiveSkill, SkillView>();

        public WorldView(Transform parent, Camera camera, SamplePresentation presentation)
        {
            _presentation = presentation;
            _camera = camera;
            _root = new GameObject("World View").transform;
            _root.SetParent(parent, false);
            _discTexture = CreateTexture("Disc", DiscPixels, radius => Mathf.Clamp01(DiscPixels / 2f - 0.5f - radius));
            _ringTexture = CreateTexture("Ring", RingPixels, radius => Mathf.Clamp01(1.5f - Mathf.Abs(RingPixels / 2f - 2f - radius)));
            _disc = CreateSprite(_discTexture, DiscPixels);
            _ring = CreateSprite(_ringTexture, RingPixels);
            _hqGlow = CreateRenderer("HQ Glow", _disc, presentation.HqGlowColor, 0);
            _hq = CreateRenderer("HQ", _disc, presentation.HqColor, 1);
        }

        public void Synchronize(World world)
        {
            SynchronizeHq(world.Hq);
            SynchronizeEnemies(world.Enemies);
            SynchronizeSkills(world.Players);
        }

        // 이전 판의 Enemy·Skill View를 모두 지운다. HQ 원판처럼 판과 무관한 객체는 유지한다.
        public void Reset()
        {
            foreach (EnemyView view in _enemies.Values) Destroy(view.Renderer);
            _enemies.Clear();
            foreach (SkillView view in _skills.Values)
            {
                Destroy(view.Fill);
                Destroy(view.Ring);
            }
            _skills.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_disc);
            Object.Destroy(_ring);
            Object.Destroy(_discTexture);
            Object.Destroy(_ringTexture);
        }

        private void SynchronizeHq(Hq hq)
        {
            Vector3 position = SceneSpace.ToScene(hq.Position);
            _camera.transform.position = new Vector3(position.x, position.y, _camera.transform.position.z);
            _hq.transform.position = position;
            _hqGlow.transform.position = position;
            _hq.transform.localScale = Vector3.one * _presentation.HqDisplayDiameter;
            _hqGlow.transform.localScale = Vector3.one * (_presentation.HqDisplayDiameter + _presentation.HqGlowExtra);
        }

        private void SynchronizeEnemies(IReadOnlyList<Enemy> enemies)
        {
            float now = Time.unscaledTime;
            _seen.Clear();
            // 매 프레임 경로: IReadOnlyList를 인덱스로 돈다(인터페이스 foreach는 열거자를 할당한다).
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                _seen.Add(enemy.Id);
                if (!_enemies.TryGetValue(enemy.Id, out EnemyView view))
                {
                    Color color = _presentation.EnemyColor(enemy.Definition.Id);
                    view = new EnemyView(CreateRenderer(enemy.Definition.Id + " #" + enemy.Id.Value, _disc, color, 2),
                        color, enemy.Health);
                    _enemies.Add(enemy.Id, view);
                }
                view.Renderer.transform.position = SceneSpace.ToScene(enemy.Position);
                // 크기는 게임 수치(반지름)를 그대로 쓴다.
                view.Renderer.transform.localScale = Vector3.one * enemy.Stats.Size * 2;

                // 피격 표시: 남은 HP 비율만큼 본래 색, 피해를 받은 순간 잠깐 밝게.
                if (enemy.Health < view.LastHealth) view.HitAt = now;
                view.LastHealth = enemy.Health;
                Color health = Color.Lerp(_presentation.DepletedEnemyColor, view.Color, enemy.Health / enemy.Stats.MaxHealth);
                view.Renderer.color = Color.Lerp(health, _presentation.HitFlashColor, Fade(now - view.HitAt, _presentation.HitFlashSeconds));
            }

            // 목록에서 사라진 Enemy의 View를 지운다. 사망 연출은 M4에서 사망 기록을 읽어 따로 한다.
            _gone.Clear();
            foreach (EnemyId id in _enemies.Keys)
                if (!_seen.Contains(id)) _gone.Add(id);
            foreach (EnemyId id in _gone)
            {
                Destroy(_enemies[id].Renderer);
                _enemies.Remove(id);
            }
        }

        // 범위 원 = Skill의 기준점과 실행 반경. 화면이 따로 정한 크기가 없다.
        // 기준점이 없으면(조준점 없음) 원을 감춘다. 틱마다 안쪽이 잠깐 밝아진다 — 맞은 적이 없어도.
        private void SynchronizeSkills(IReadOnlyList<Player> players)
        {
            float now = Time.unscaledTime;
            for (int p = 0; p < players.Count; p++)
            {
                IReadOnlyList<PassiveSkill> skills = players[p].Skills;
                for (int s = 0; s < skills.Count; s++)
                {
                    PassiveSkill skill = skills[s];
                    if (!_skills.TryGetValue(skill, out SkillView view))
                    {
                        string name = players[p].Id + " " + skill.Definition.Id;
                        view = new SkillView(
                            CreateRenderer(name + " Fill", _disc, _presentation.SkillFillColor, 3),
                            CreateRenderer(name + " Ring", _ring, _presentation.SkillRingColor, 4),
                            skill.TickCount);
                        _skills.Add(skill, view);
                    }

                    if (skill.TickCount != view.LastTick)
                    {
                        view.LastTick = skill.TickCount;
                        view.TickAt = now;
                    }

                    bool visible = skill.TryGetOrigin(out Point2 origin);
                    view.Fill.enabled = visible;
                    view.Ring.enabled = visible;
                    if (!visible) continue;

                    Vector3 position = SceneSpace.ToScene(origin);
                    Vector3 scale = Vector3.one * skill.Stats.Radius * 2;
                    view.Fill.transform.position = position;
                    view.Ring.transform.position = position;
                    view.Fill.transform.localScale = scale;
                    view.Ring.transform.localScale = scale;
                    view.Fill.color = Color.Lerp(_presentation.SkillFillColor, _presentation.SkillTickColor,
                        Fade(now - view.TickAt, _presentation.SkillTickSeconds));
                }
            }
        }

        // 0초에 1, duration초 뒤 0으로 줄어드는 연출 가중치.
        private static float Fade(float elapsed, float duration) => Mathf.Clamp01(1 - elapsed / duration);

        private static void Destroy(SpriteRenderer view)
        {
            view.gameObject.SetActive(false);
            Object.Destroy(view.gameObject);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        // 지름 1 단위의 원형 스프라이트.
        private static Sprite CreateSprite(Texture2D texture, int pixels) =>
            Sprite.Create(texture, new Rect(0, 0, pixels, pixels), new Vector2(0.5f, 0.5f), pixels);

        // 중심으로부터의 픽셀 거리 → 알파.
        private static Texture2D CreateTexture(string name, int pixels, Func<float, float> alpha)
        {
            var texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear };
            var colors = new Color[pixels * pixels];
            float center = pixels / 2f - 0.5f;
            for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
                colors[y * pixels + x] = new Color(1, 1, 1, alpha(new Vector2(x - center, y - center).magnitude));
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private sealed class EnemyView
        {
            public readonly SpriteRenderer Renderer;
            public readonly Color Color;
            public float LastHealth;
            public float HitAt = float.NegativeInfinity;

            public EnemyView(SpriteRenderer renderer, Color color, float health)
            {
                Renderer = renderer;
                Color = color;
                LastHealth = health;
            }
        }

        private sealed class SkillView
        {
            public readonly SpriteRenderer Fill;
            public readonly SpriteRenderer Ring;
            public int LastTick;
            public float TickAt = float.NegativeInfinity;

            public SkillView(SpriteRenderer fill, SpriteRenderer ring, int tick)
            {
                Fill = fill;
                Ring = ring;
                LastTick = tick;
            }
        }
    }
}
