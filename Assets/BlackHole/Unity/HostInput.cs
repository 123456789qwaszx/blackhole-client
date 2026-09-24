using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // 입력 장치를 한 프레임의 호스트 요청으로 해석한다. 게임 상태를 읽거나 바꾸지 않는다.
    // 지금은 판 조작(재시작, 일시정지)뿐이다. 조준점(AimPoint)은 M3에서 들어온다.
    internal sealed class HostInput
    {
        public bool Restart { get; private set; }
        public bool TogglePause { get; private set; }

        public void Read()
        {
            Keyboard keyboard = Keyboard.current;
            Restart = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            TogglePause = keyboard != null && keyboard.pKey.wasPressedThisFrame;
        }
    }
}
