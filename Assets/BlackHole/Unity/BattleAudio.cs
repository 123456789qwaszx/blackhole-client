using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 전투 소리. 화면처럼 판의 기록과 상태를 읽기만 하고, 게임 상태를 바꾸지 않는다(CA-005).
    // 판이 바뀌면 SessionLauncher가 Reset을 먼저 부른다.
    //
    // 재생 정책(CONTENT_DEFINITION 5.3, 값은 SampleSounds의 [임시]):
    // - 소리는 사건마다가 아니라 소리 종류마다 한 프레임 최대 수까지 낸다. 동시 사망·연쇄가 많아도 재생 수가 막힌다.
    // - 한 번의 실행이 여러 적을 맞히면 소리는 한 번이고, 맞힌 수는 그 소리의 세기로 나타낸다.
    // - 아무도 맞히지 않은 Breaker 틱은 소리가 없다. 피격음은 따로 없다(공격 쪽 소리가 맡는다).
    // - 동시 재생이 목소리 상한에 닿으면 새 재생을 버린다.
    // - 새 판을 시작하면 이전 판의 소리를 멈춘다.
    internal sealed class BattleAudio : IDisposable
    {
        private readonly SampleSounds _sounds;
        private readonly GameObject _root;
        private readonly AudioSource[] _voices;
        private readonly Dictionary<string, int> _playedThisFrame = new Dictionary<string, int>(StringComparer.Ordinal);
        // 앞 프레임에 본 값. 처음 보는 Skill은 기준만 잡고 소리를 내지 않는다.
        private readonly Dictionary<BreakerSkill, int> _breakerTicks = new Dictionary<BreakerSkill, int>();
        private readonly Dictionary<PiercingLaserSkill, int> _laserPending = new Dictionary<PiercingLaserSkill, int>();
        private long _lastDeathSequence;
        private long _lastDeathEffectHitSequence;
        private long _lastLaserFireSequence;
        private int _lastLevel = -1;
        private float _lastLimit = -1;

        public BattleAudio(Transform parent, SampleSounds sounds)
        {
            _sounds = sounds;
            _root = new GameObject("Battle Audio");
            _root.transform.SetParent(parent, false);
            _voices = new AudioSource[sounds.VoiceLimit];

            for (int i = 0; i < _voices.Length; i++)
            {
                AudioSource voice = _root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 0;
                _voices[i] = voice;
            }
        }

        public void Synchronize(GameSession session)
        {
            _playedThisFrame.Clear();
            World world = session.World;

            SynchronizeSkills(world);

            for (int i = 0; i < world.LaserFires.Count; i++)
            {
                LaserFireRecord fire = world.LaserFires[i];
                if (fire.Sequence <= _lastLaserFireSequence) continue;
                _lastLaserFireSequence = fire.Sequence;
                Play(SampleSounds.LaserFire, fire.HitCount);
            }

            for (int i = 0; i < world.Deaths.Count; i++)
            {
                DeathRecord death = world.Deaths[i];
                if (death.Sequence <= _lastDeathSequence) continue;
                _lastDeathSequence = death.Sequence;
                Play(_sounds.DeathSound(death.EnemyTypeId), 1);
            }

            // [임시] 번개는 발동을 묶는 번호가 없어 한 프레임 최대 수(1)로 "발동 1회에 한 번"을 근사한다.
            for (int i = 0; i < world.DeathEffectHits.Count; i++)
            {
                DeathEffectHit hit = world.DeathEffectHits[i];
                if (hit.Sequence <= _lastDeathEffectHitSequence) continue;
                _lastDeathEffectHitSequence = hit.Sequence;
                Play(SampleSounds.LightningZap, 1);
            }

            if (_lastLevel >= 0 && world.Hq.Level > _lastLevel)
                Play(SampleSounds.HqGrow, 1);
            _lastLevel = world.Hq.Level;

            // Milestone은 시간 연장으로 알아챈다. 시간 없이 공급만 주는 Milestone은 이 방법으로 구별되지 않는다([임시]).
            if (_lastLimit >= 0 && session.TimeLimit.Limit > _lastLimit)
                Play(SampleSounds.Milestone, 1);
            _lastLimit = session.TimeLimit.Limit;
        }

        // 이전 판의 소리를 멈추고 기준을 지운다.
        public void Reset()
        {
            foreach (AudioSource voice in _voices)
                voice.Stop();

            _breakerTicks.Clear();
            _laserPending.Clear();
            _lastDeathSequence = 0;
            _lastDeathEffectHitSequence = 0;
            _lastLaserFireSequence = 0;
            _lastLevel = -1;
            _lastLimit = -1;
        }

        public void Dispose()
        {
            Object.Destroy(_root);
            _sounds.Dispose();
        }

        private void SynchronizeSkills(World world)
        {
            IReadOnlyList<Player> players = world.Players;

            for (int p = 0; p < players.Count; p++)
            {
                IReadOnlyList<PassiveSkill> skills = players[p].Skills;

                for (int s = 0; s < skills.Count; s++)
                {
                    switch (skills[s])
                    {
                        case BreakerSkill breaker:
                            if (_breakerTicks.TryGetValue(breaker, out int ticks) && breaker.TickCount != ticks && breaker.LastTickHitCount > 0)
                                Play(SampleSounds.BreakerHit, breaker.LastTickHitCount);
                            _breakerTicks[breaker] = breaker.TickCount;
                            break;

                        case PiercingLaserSkill laser:
                            // 새 예고 수 = 지금 예고 수 − 앞 프레임 예고 수 + 이번 프레임에 발사해 빠진 수.
                            int pending = laser.PendingShots.Count;
                            if (_laserPending.TryGetValue(laser, out int before)
                                && pending - before + FiredThisFrame(world, players[p].Id, laser) > 0)
                                Play(SampleSounds.LaserCharge, 1);
                            _laserPending[laser] = pending;
                            break;
                    }
                }
            }
        }

        private int FiredThisFrame(World world, PlayerId owner, PiercingLaserSkill laser)
        {
            int fired = 0;

            for (int i = 0; i < world.LaserFires.Count; i++)
            {
                LaserFireRecord fire = world.LaserFires[i];
                if (fire.Sequence > _lastLaserFireSequence && fire.Owner.Equals(owner) && fire.Laser == laser.Definition)
                    fired++;
            }

            return fired;
        }

        // 맞힌 수 1에서 기본 음량의 70%, 5 이상에서 100%.
        private void Play(string id, int hits)
        {
            SampleSound sound = _sounds.Get(id);
            _playedThisFrame.TryGetValue(id, out int played);
            if (played >= sound.MaxPerFrame) return;

            AudioSource voice = FreeVoice();
            if (voice == null) return;

            _playedThisFrame[id] = played + 1;
            voice.clip = sound.Clip;
            voice.volume = sound.Volume * Mathf.Lerp(0.7f, 1, Mathf.Clamp01((hits - 1) / 4f));
            voice.Play();
        }

        private AudioSource FreeVoice()
        {
            foreach (AudioSource voice in _voices)
                if (!voice.isPlaying) return voice;
            return null;
        }
    }
}
