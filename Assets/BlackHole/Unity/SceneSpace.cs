using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 규칙 좌표와 Unity 장면 좌표의 변환을 한 곳에 둔다. 규칙 평면은 장면의 z = 0이고, x·y는 같다.
    // 화면(규칙 → 장면)과 입력(장면 → 규칙)이 같은 변환을 쓴다.
    internal static class SceneSpace
    {
        public static Vector3 ToScene(Point2 point) => new Vector3(point.X, point.Y, 0);

        public static Point2 ToRules(Vector3 scene) => new Point2(scene.x, scene.y);
    }
}
