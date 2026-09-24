using System;
using System.Collections.Generic;
using BlackHole.Sample;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 소리 샘플. 레포에 오디오 에셋이 없어 코드로 합성한 임시 소리다. 게임 규칙이 아니다(Core는 모른다).
    // 소리 이름, 한 프레임 최대 재생 수, 음량, 목소리 상한은 CONTENT_DEFINITION 5.3의 재생 정책 가설을 시험하는 [임시] 값이다.
    internal sealed class SampleSounds : IDisposable
    {
        public const string BreakerHit = "breaker-hit";
        public const string LaserCharge = "laser-charge";
        public const string LaserFire = "laser-fire";
        public const string BreakSmall = "break-small";
        public const string BreakElectric = "break-electric";
        public const string LightningZap = "lightning-zap";
        public const string HqGrow = "hq-grow";
        public const string Milestone = "milestone";

        private const int Rate = 22050;

        private readonly Dictionary<string, SampleSound> _sounds = new Dictionary<string, SampleSound>(StringComparer.Ordinal);

        // 파괴음은 Enemy 종류마다 고른다. 정해지지 않은 종류는 일반 파괴음이다.
        private readonly Dictionary<string, string> _deathSounds = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { SampleContent.ElectricEnemyId, BreakElectric }
        };

        // 동시에 울릴 수 있는 소리 수. 모두 울리는 중이면 새 재생을 버린다.
        public int VoiceLimit { get; } = 12;

        public SampleSounds()
        {
            var noise = new System.Random(7);

            Add(BreakerHit, 0.35f, 1, 0.07f, t => Sine(t, 520) * 0.8f + Noise(noise) * 0.2f);
            Add(LaserCharge, 0.15f, 2, 0.25f, t => Sine(t, 300 + 2400 * t));
            Add(LaserFire, 0.4f, 2, 0.18f, t => Sine(t, 180) * 0.5f + Noise(noise) * 0.5f);
            Add(BreakSmall, 0.3f, 3, 0.09f, t => Noise(noise));
            Add(BreakElectric, 0.35f, 2, 0.14f, t => Noise(noise) * Square(t, 90));
            Add(LightningZap, 0.2f, 1, 0.08f, t => Square(t, 1400) * 0.6f + Noise(noise) * 0.4f);
            Add(HqGrow, 0.3f, 1, 0.35f, t => (Sine(t, 440 + 200 * t) + Sine(t, 660 + 300 * t)) * 0.5f);
            Add(Milestone, 0.35f, 1, 0.5f, t => Sine(t, t < 0.16f ? 523 : t < 0.32f ? 659 : 784));
        }

        public SampleSound Get(string id) => _sounds[id];

        public string DeathSound(string enemyTypeId) =>
            enemyTypeId != null && _deathSounds.TryGetValue(enemyTypeId, out string id) ? id : BreakSmall;

        public void Dispose()
        {
            foreach (SampleSound sound in _sounds.Values)
                Object.Destroy(sound.Clip);
            _sounds.Clear();
        }

        private void Add(string id, float volume, int maxPerFrame, float seconds, Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                // 5ms에 올라가고 끝까지 지수로 줄어든다. 끝에서 딸깍 소리가 나지 않게 0으로 닫는다.
                float envelope = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-4 * t / seconds) * (1 - t / seconds);
                samples[i] = Mathf.Clamp(wave(t) * envelope, -1, 1);
            }

            AudioClip clip = AudioClip.Create(id, count, 1, Rate, false);
            clip.SetData(samples, 0);
            _sounds.Add(id, new SampleSound(clip, volume, maxPerFrame));
        }

        private static float Sine(float t, float hz) => Mathf.Sin(2 * Mathf.PI * hz * t);

        private static float Square(float t, float hz) => Sine(t, hz) >= 0 ? 1 : -1;

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2 - 1);
    }

    internal sealed class SampleSound
    {
        public AudioClip Clip { get; }
        public float Volume { get; }
        // 한 프레임에 이 소리를 낼 수 있는 최대 수.
        public int MaxPerFrame { get; }

        public SampleSound(AudioClip clip, float volume, int maxPerFrame)
        {
            Clip = clip;
            Volume = volume;
            MaxPerFrame = maxPerFrame;
        }
    }
}
