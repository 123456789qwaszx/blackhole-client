using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적·전투 시스템: 한 판(GameSession)과 그 적의 표현(EnemyView)의 수명을 가진다.
    //
    // 스스로 시작하거나 끝내지 않는다. 상위 오케스트레이터(BattleOrchestrator)가 정해진 순서 안에서 부를 때만
    // 시작(StartAsync)하고 정리(ShutdownAsync)한다. 시간이 끝나면 알릴 뿐(TimeExpired)이고, 정리는 오케스트레이터가 요청한다.
    // 사운드·조준 같은 다른 시스템의 정리는 이 시스템의 일이 아니다.
    //
    // 시작 단계:
    //   1. 업그레이드에서 바뀐 수치 받기 — 판을 조립한다: 진행 상태를 묶고 이 판의 적 수치를 확정한다(지금은 보정 없음).
    //   2. 적 소환 단계 진입 — 전투 시작 공급을 내보내고 판을 진행 단계로 넣는다.
    // 종료 단계:
    //   1. 종료 요청(사유)             2. 화면에서 관리하던 적의 수가 0(남은 적·요청 정리 — 처치 아님)
    //   3. 죽은 적의 처리 완료          4. 처치 집계를 계산해 보관(원자료)
    //   5. 화면의 연출 정리             6. 모두 끝났으면 완전 초기화
    // 종료 뒤에 남는 것은 UI와 원자료(LastRawData)뿐이다. 판을 시작했던 다른 흔적은 없다.
    internal sealed class BattleSystem
    {
        private enum State { Idle, Starting, Running, ShuttingDown, Faulted }

        private readonly GameContent _content;
        // 판 조립이 참가자마다 산 노드로 업그레이드 표를 만들 때 쓴다.
        private readonly NodeTree _nodes;
        private readonly EnemyView _enemyView;
        private State _state = State.Idle;
        private IReadOnlyList<PlayerState> _players;
        private bool _timeExpiredRaised;

        public Checklist StartSteps { get; } = new Checklist(
            "Receive upgraded stats",
            "Enter spawning phase");

        public Checklist EndSteps { get; } = new Checklist(
            "End requested",
            "Enemies on screen: 0",
            "Dead enemies processed",
            "Kill tally stored",
            "Presentation cleared",
            "Fully reset");

        // 진행 중인(또는 정리 중인) 판. 시작 전과 완전 초기화 뒤에는 null이다.
        public GameSession Session { get; private set; }
        // 마지막으로 정리한 판의 원자료. 정리가 끝난 뒤에도 남는다.
        public BattleRawData LastRawData { get; private set; }
        public bool IsIdle => _state == State.Idle;
        public bool IsRunning => _state == State.Running;
        // 정리를 요청할 수 있는가: 진행 중이거나, 앞선 정리가 실패해 멈춘 상태.
        public bool CanShutdown => _state == State.Running || _state == State.Faulted;

        // 판의 시간이 끝났다. 정리하지 않고 알리기만 한다(한 판에 한 번).
        public event Action TimeExpired;

        public BattleSystem(GameContent content, NodeTree nodes, EnemyView enemyView)
        {
            _content = content;
            _nodes = nodes;
            _enemyView = enemyView;
        }

        // 전투 진입을 위한 초기화. 오케스트레이터만 부른다.
        public Task StartAsync(IReadOnlyList<PlayerState> players, int stage, int seed)
        {
            if (_state != State.Idle)
                throw new InvalidOperationException($"준비된 상태에서만 시작할 수 있다. 지금: {_state}.");

            _state = State.Starting;
            _players = players;
            _timeExpiredRaised = false;
            StartSteps.Reset();
            EndSteps.Reset();

            // 1. 업그레이드에서 바뀐 수치 받기: 조립이 이 판의 적 수치 표를 확정하고, 참가자마다 산 노드로 업그레이드 표를 만든다.
            //    표를 읽어 수치를 바꾸는 시스템은 아직 없다(UPGRADE_LINK_PLAN 6절).
            Session = SessionAssembler.CreateBattle(_content, players, stage, seed, _nodes);
            StartSteps.Mark(0, StepState.Done);

            // 2. 적 소환 단계 진입.
            Session.Begin();
            _enemyView.Reset();
            _enemyView.Synchronize(Session.World);
            StartSteps.Mark(1, StepState.Done);

            _state = State.Running;
            return Task.CompletedTask;
        }

        public void Tick(float delta)
        {
            if (_state != State.Running)
                return;

            Session.Advance(delta);
            _enemyView.Synchronize(Session.World);

            if (Session.Phase == SessionPhase.Ended && !_timeExpiredRaised)
            {
                _timeExpiredRaised = true;
                TimeExpired?.Invoke();
            }
        }

        public void TogglePause()
        {
            if (_state == State.Running)
                Session.TogglePause();
        }

        // 전투 종료 뒤 자신의 모든 것을 정리한다. 오케스트레이터만 부른다.
        // 단계 하나라도 확인에 실패하면 멈추고(Faulted) 완전 초기화하지 않는다. 다시 부르면 처음부터 확인한다.
        public async Task<BattleRawData> ShutdownAsync(SessionEndReason reason)
        {
            if (!CanShutdown)
                throw new InvalidOperationException($"진행 중인 판이 없다. 지금: {_state}.");

            _state = State.ShuttingDown;
            EndSteps.Reset();

            try
            {
                // 1. 종료 요청. 시간이 끝나 이미 끝난 판이면 처음 사유가 남는다.
                Session.RequestEnd(reason);
                EndSteps.Rename(0, $"End requested: {Session.Result.Reason}");
                EndSteps.Mark(0, StepState.Done);

                // 2. 화면에서 관리하던 적의 수가 0. 남은 적은 처치가 아니라 정리다.
                //    처리되지 않은 생성·파괴 요청도 함께 버린다 — 끝난 판은 새 적도, 새 사망도 만들지 않는다.
                Session.ClearRemainingEnemies();
                World world = Session.World;
                Verify(1, world.Enemies.Count == 0 && world.PendingSpawns.Count == 0 && world.PendingDestroys.Count == 0);

                // 3. 죽은 적의 처리 완료(사망 효과·보상 처리가 붙으면 그것이 끝났는지까지).
                Verify(2, !Session.World.HasPendingDeathProcessing);

                // 4. 처치 집계를 계산해 보관.
                LastRawData = Session.CreateRawData();
                Verify(3, LastRawData != null);

                // 5. 화면의 연출 정리. 지운 객체는 프레임 끝에 사라지므로 한 프레임 기다린 뒤 확인한다.
                _enemyView.Reset();
                await Awaitable.NextFrameAsync();
                Verify(4, _enemyView.IsClear);

                // 6. 완전 초기화: 판을 버리고, 진행 상태가 전투에서 풀렸는지 확인한다.
                Verify(5, PlayersReleased());
                Session = null;
                _players = null;
                _timeExpiredRaised = false;
                _state = State.Idle;
                return LastRawData;
            }
            catch
            {
                _state = State.Faulted;
                throw;
            }
        }

        private void Verify(int step, bool passed)
        {
            EndSteps.Mark(step, passed ? StepState.Done : StepState.Failed);

            if (!passed)
                throw new InvalidOperationException($"전투 정리 단계 실패: {EndSteps.NameOf(step)}.");
        }

        private bool PlayersReleased()
        {
            foreach (PlayerState player in _players)
            {
                if (player.InBattle)
                    return false;
            }

            return true;
        }
    }
}
