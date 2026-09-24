using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 계약용 콘텐츠와 판 조립. 샘플 콘텐츠의 [임시] 값에 기대지 않도록, 계약은 필요한 값을 여기서 직접 정한다.
    internal static class TestContent
    {
        public static readonly PlayerId First = new PlayerId(1);
        public static readonly PlayerId Second = new PlayerId(2);
        public const string EnemyId = "test-enemy";

        // 기본: Enemy 1종(HP 10, 속도 1, 크기 0.3, 반시계 공전), 1초마다 HQ에서 거리 3에 출현, 최대 10.
        public static ContentData Data(float timeLimit = 60, float hqX = 0, float hqY = 0) => new ContentData
        {
            Session = new SessionData { TimeLimit = timeLimit },
            Hq = new HqData { X = hqX, Y = hqY },
            Enemies = new List<EnemyData> { Enemy(EnemyId, 10, 1, 0.3f) },
            Spawn = new SpawnData
            {
                Interval = 1, MaxAlive = 10, Distance = 3, AngleStep = 1,
                Order = new List<string> { EnemyId }
            }
        };

        public static EnemyData Enemy(string id, float health, float speed, float size, bool clockwise = false) =>
            new EnemyData
            {
                Id = id, MaxHealth = health, MoveSpeed = speed, Size = size,
                Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = clockwise }
            };

        public static GameContent Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded,
                result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Content;
        }

        // 참가자를 따로 주지 않으면 Player 1명.
        public static GameSession Session(ContentData data, params PlayerId[] participants) =>
            SessionAssembler.Create(Load(data), participants.Length == 0 ? new[] { First } : participants);

        public static void HasDiagnostic(ContentLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new System.InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }

        public static float DistanceToHq(GameSession game, Enemy enemy) =>
            (float)System.Math.Sqrt(enemy.Position.DistanceSquared(game.World.Hq.Position));
    }
}
