using UnityEngine;

namespace BlackHole.Unity
{
    // v2 호스트의 진입점. M1에서 조립 루트(판 시작·종료·재시작, 입력, 화면)가 된다.
    // 지금(M0)은 씬에 붙어 켜지고 꺼지는 것만 확인한다.
    public sealed class GameHost : MonoBehaviour
    {
        private void OnEnable() => Debug.Log("[GameHost] enabled", this);

        private void OnDisable() => Debug.Log("[GameHost] disabled", this);
    }
}
