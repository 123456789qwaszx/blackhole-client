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
        private const int LinePixels = 4;

        private readonly SamplePresentation _presentation;
        private readonly Camera _camera;
        private readonly Transform _root;
        private readonly Texture2D _discTexture;
        private readonly Texture2D _ringTexture;
        private readonly Texture2D _lineTexture;
        private readonly Sprite _disc;
        private readonly Sprite _ring;
        private readonly Sprite _line;
        private readonly SpriteRenderer _hq;
        private readonly SpriteRenderer _hqGlow;
        private readonly Dictionary<EnemyId, EnemyView> _enemies = new Dictionary<EnemyId, EnemyView>();
        private readonly List<AbsorbingView> _absorbing = new List<AbsorbingView>();
        private long _lastDeathSequence;
        private readonly List<FadingLine> _lightning = new List<FadingLine>();
        private long _lastDeathEffectHitSequence;
        // 레이저 예고 선은 매 프레임 예고 중인 발사에 맞춰 다시 쓴다. 발사 선은 발사 기록마다 하나씩 만든다.
        private readonly List<SpriteRenderer> _telegraphs = new List<SpriteRenderer>();
        private readonly List<FadingLine> _laserFires = new List<FadingLine>();
        private long _lastLaserFireSequence;
        private readonly HashSet<EnemyId> _seen = new HashSet<EnemyId>();
        private readonly List<EnemyId> _gone = new List<EnemyId>();
        // Skill은 판 안에서 사라지지 않는다. 판이 바뀌면 Reset이 지운다.
        private readonly Dictionary<BreakerSkill, SkillView> _skills = new Dictionary<BreakerSkill, SkillView>();

        public WorldView(Transform parent, Camera camera, SamplePresentation presentation)
        {
            _presentation = presentation;
            _camera = camera;
            _root = new GameObject("World View").transform;
            _root.SetParent(parent, false);
            _discTexture = CreateTexture("Disc", DiscPixels, radius => Mathf.Clamp01(DiscPixels / 2f - 0.5f - radius));
            _ringTexture = CreateTexture("Ring", RingPixels, radius => Mathf.Clamp01(1.5f - Mathf.Abs(RingPixels / 2f - 2f - radius)));
            _lineTexture = CreateTexture("Line", LinePixels, radius => 1);
            _disc = CreateSprite(_discTexture, DiscPixels);
            _ring = CreateSprite(_ringTexture, RingPixels);
            _line = CreateSprite(_lineTexture, LinePixels);
            _hqGlow = CreateRenderer("HQ Glow", _disc, presentation.HqGlowColor, 0);
            _hq = CreateRenderer("HQ", _disc, presentation.HqColor, 1);
        }

        public void Synchronize(World world)
        {
            SynchronizeHq(world.Hq);
            SynchronizeEnemies(world.Enemies, world.Deaths, world.Hq.Position);
            SynchronizeDeathEffects(world.DeathEffectHits);
            SynchronizeSkills(world.Players);
            SynchronizeLasers(world.Players, world.LaserFires);
        }

        // 이전 판의 Enemy·Skill View를 모두 지운다. HQ 원판처럼 판과 무관한 객체는 유지한다.
        public void Reset()
        {
            foreach (EnemyView view in _enemies.Values) Destroy(view.Renderer);
            _enemies.Clear();
            foreach (AbsorbingView view in _absorbing) Destroy(view.Renderer);
            _absorbing.Clear();
            _lastDeathSequence = 0;
            foreach (FadingLine view in _lightning) Destroy(view.Renderer);
            _lightning.Clear();
            _lastDeathEffectHitSequence = 0;
            foreach (SpriteRenderer view in _telegraphs) Destroy(view);
            _telegraphs.Clear();
            foreach (FadingLine view in _laserFires) Destroy(view.Renderer);
            _laserFires.Clear();
            _lastLaserFireSequence = 0;
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
            Object.Destroy(_line);
            Object.Destroy(_discTexture);
            Object.Destroy(_ringTexture);
            Object.Destroy(_lineTexture);
        }

        private void SynchronizeHq(Hq hq)
        {
            Vector3 position = SceneSpace.ToScene(hq.Position);
            _camera.transform.position = new Vector3(position.x, position.y, _camera.transform.position.z);
            _hq.transform.position = position;
            _hqGlow.transform.position = position;

            // 크기는 성장 Level을 읽은 표현이다. 출현 거리 같은 규칙 좌표에는 영향이 없다.
            float diameter = _presentation.HqDiameter(hq.Level);
            _hq.transform.localScale = Vector3.one * diameter;
            _hqGlow.transform.localScale = Vector3.one * (diameter + _presentation.HqGlowExtra);
        }

        private void SynchronizeEnemies(IReadOnlyList<Enemy> enemies, IReadOnlyList<DeathRecord> deaths, Point2 hq)
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

            // 사망 기록에는 같은 Advance의 모든 하위 단계가 누적된다. 같은 배치를
            // 여러 번 읽어도 Sequence로 한 번만 연출을 만든다.
            for (int i = 0; i < deaths.Count; i++)
            {
                DeathRecord death = deaths[i];
                if (death.Sequence <= _lastDeathSequence) continue;
                _lastDeathSequence = death.Sequence;
                SpriteRenderer renderer;
                if (_enemies.TryGetValue(death.EnemyId, out EnemyView existing))
                {
                    renderer = existing.Renderer;
                    _enemies.Remove(death.EnemyId);
                }
                else
                {
                    renderer = CreateRenderer(death.EnemyTypeId + " death #" + death.EnemyId.Value,
                        _disc, _presentation.EnemyColor(death.EnemyTypeId), 2);
                    renderer.transform.localScale = Vector3.one * death.Size * 2;
                }
                renderer.transform.position = SceneSpace.ToScene(death.Position);
                _absorbing.Add(new AbsorbingView(renderer, death.Position, now));
            }

            Vector3 destination = SceneSpace.ToScene(hq);
            for (int i = _absorbing.Count - 1; i >= 0; i--)
            {
                AbsorbingView view = _absorbing[i];
                float progress = Mathf.Clamp01((now - view.StartedAt) / _presentation.AbsorbSeconds);
                view.Renderer.transform.position = Vector3.Lerp(SceneSpace.ToScene(view.Start), destination, progress);
                view.Renderer.transform.localScale = view.InitialScale * (1 - progress);
                if (progress < 1) continue;
                Destroy(view.Renderer);
                _absorbing.RemoveAt(i);
            }

            // 일반적인 목록 제거(전투 정리 등)는 처치 연출을 만들지 않는다.
            _gone.Clear();
            foreach (EnemyId id in _enemies.Keys)
                if (!_seen.Contains(id)) _gone.Add(id);
            foreach (EnemyId id in _gone)
            {
                Destroy(_enemies[id].Renderer);
                _enemies.Remove(id);
            }
        }

        // 사망 효과의 적중 기록을 연출로 바꾼다. 피해는 이미 처리됐고, 연출이 끝나는지와 무관하다.
        // 사망 기록처럼 같은 Advance의 모든 하위 단계가 쌓이므로 Sequence로 한 번만 만든다.
        private void SynchronizeDeathEffects(IReadOnlyList<DeathEffectHit> hits)
        {
            float now = Time.unscaledTime;

            for (int i = 0; i < hits.Count; i++)
            {
                DeathEffectHit hit = hits[i];
                if (hit.Sequence <= _lastDeathEffectHitSequence) continue;
                _lastDeathEffectHitSequence = hit.Sequence;
                if (!(hit.Effect is ChainLightningDefinition)) continue;

                SpriteRenderer renderer = CreateRenderer("Lightning #" + hit.Sequence, _line, _presentation.LightningColor, 5);
                PlaceLine(renderer, hit.From, hit.To, _presentation.LightningWidth);
                _lightning.Add(new FadingLine(renderer, now));
            }

            // 일시정지 중에도 실제 시간으로 옅어진다(흡수 연출과 같은 [임시]).
            for (int i = _lightning.Count - 1; i >= 0; i--)
            {
                FadingLine view = _lightning[i];
                float alpha = Fade(now - view.StartedAt, _presentation.LightningSeconds);
                Color color = _presentation.LightningColor;
                view.Renderer.color = new Color(color.r, color.g, color.b, color.a * alpha);
                if (alpha > 0) continue;
                Destroy(view.Renderer);
                _lightning.RemoveAt(i);
            }
        }

        // 관통 레이저. 예고 중인 발사는 얇은 선이 발사에 가까울수록 진해진다(PendingShots를 매 프레임 읽는다).
        // 발사는 판정 굵기 그대로의 선이 옅어진다. 발사 기록은 Sequence로 한 번만 소비한다. 피해는 이미 처리된 뒤다.
        private void SynchronizeLasers(IReadOnlyList<Player> players, IReadOnlyList<LaserFireRecord> fires)
        {
            float now = Time.unscaledTime;
            int used = 0;

            for (int p = 0; p < players.Count; p++)
            {
                IReadOnlyList<PassiveSkill> skills = players[p].Skills;
                for (int s = 0; s < skills.Count; s++)
                {
                    if (!(skills[s] is PiercingLaserSkill laser)) continue;
                    IReadOnlyList<LaserShot> shots = laser.PendingShots;
                    for (int i = 0; i < shots.Count; i++)
                    {
                        if (used == _telegraphs.Count)
                            _telegraphs.Add(CreateRenderer("Laser Telegraph", _line, _presentation.LaserTelegraphColor, 5));

                        SpriteRenderer renderer = _telegraphs[used++];
                        renderer.enabled = true;
                        PlaceLine(renderer, shots[i].Start, shots[i].End, _presentation.LaserTelegraphWidth);
                        float ready = 1 - Mathf.Clamp01(shots[i].RemainingTelegraph / laser.Stats.TelegraphDuration);
                        renderer.color = Color.Lerp(_presentation.LaserTelegraphColor, _presentation.LaserTelegraphReadyColor, ready);
                    }
                }
            }

            for (int i = used; i < _telegraphs.Count; i++)
                _telegraphs[i].enabled = false;

            for (int i = 0; i < fires.Count; i++)
            {
                LaserFireRecord fire = fires[i];
                if (fire.Sequence <= _lastLaserFireSequence) continue;
                _lastLaserFireSequence = fire.Sequence;

                SpriteRenderer renderer = CreateRenderer("Laser Fire #" + fire.Sequence, _line, _presentation.LaserFireColor, 6);
                PlaceLine(renderer, fire.Start, fire.End, fire.Width);
                _laserFires.Add(new FadingLine(renderer, now));
            }

            for (int i = _laserFires.Count - 1; i >= 0; i--)
            {
                FadingLine view = _laserFires[i];
                float alpha = Fade(now - view.StartedAt, _presentation.LaserFireSeconds);
                Color color = _presentation.LaserFireColor;
                view.Renderer.color = new Color(color.r, color.g, color.b, color.a * alpha);
                if (alpha > 0) continue;
                Destroy(view.Renderer);
                _laserFires.RemoveAt(i);
            }
        }

        // 범위 원 = Skill의 기준점과 실행 반경. 화면이 따로 정한 크기가 없다.
        // 기준점이 없으면(조준점 없음) 원을 감춘다. 틱마다 안쪽이 잠깐 밝아진다 — 맞은 적이 없어도.
        // 범위 원은 Breaker만 그린다. 레이저는 SynchronizeLasers가 그린다.
        private void SynchronizeSkills(IReadOnlyList<Player> players)
        {
            float now = Time.unscaledTime;
            for (int p = 0; p < players.Count; p++)
            {
                IReadOnlyList<PassiveSkill> skills = players[p].Skills;
                for (int s = 0; s < skills.Count; s++)
                {
                    if (!(skills[s] is BreakerSkill skill)) continue;
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

        // 선 스프라이트(1 × 1)를 두 규칙 좌표 사이에 굵기 width로 놓는다.
        private static void PlaceLine(SpriteRenderer renderer, Point2 from, Point2 to, float width)
        {
            Vector3 start = SceneSpace.ToScene(from);
            Vector3 end = SceneSpace.ToScene(to);
            Vector3 span = end - start;
            renderer.transform.position = (start + end) / 2;
            renderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            renderer.transform.localScale = new Vector3(span.magnitude, width, 1);
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

        private sealed class AbsorbingView
        {
            public readonly SpriteRenderer Renderer;
            public readonly Point2 Start;
            public readonly float StartedAt;
            public readonly Vector3 InitialScale;

            public AbsorbingView(SpriteRenderer renderer, Point2 start, float startedAt)
            {
                Renderer = renderer;
                Start = start;
                StartedAt = startedAt;
                InitialScale = renderer.transform.localScale;
            }
        }

        private sealed class FadingLine
        {
            public readonly SpriteRenderer Renderer;
            public readonly float StartedAt;

            public FadingLine(SpriteRenderer renderer, float startedAt)
            {
                Renderer = renderer;
                StartedAt = startedAt;
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
