using BlackHole.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // 입력 장치를 한 프레임의 호스트 요청으로 해석한다. 게임 상태를 읽거나 바꾸지 않는다.
    // 조준(Aim)은 마우스가 가리키는 규칙 좌표일 뿐이다. 어느 Player의 AimPoint로 넣을지는 GameHost가 정한다.
    internal sealed class HostInput
    {
        private readonly Camera _camera;

        public bool Restart { get; private set; }
        public bool TogglePause { get; private set; }
        // 마우스가 없거나 화면 밖이면 null이다.
        public Point2? Aim { get; private set; }

        public HostInput(Camera camera)
        {
            _camera = camera;
        }

        public void Read()
        {
            Keyboard keyboard = Keyboard.current;
            
            Restart = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            TogglePause = keyboard != null && keyboard.pKey.wasPressedThisFrame;
            
            Aim = ReadAim();
        }

        private Point2? ReadAim()
        {
            Mouse mouse = Mouse.current;
            
            if (mouse == null) 
                return null;

            Vector2 screen = mouse.position.ReadValue();
            
            if (screen.x < 0 
                || screen.y < 0 
                || screen.x >= Screen.width 
                || screen.y >= Screen.height)
                return null;
            
            return SceneSpace.ToRules(
                _camera.ScreenToWorldPoint(screen));
        }
    }
}
