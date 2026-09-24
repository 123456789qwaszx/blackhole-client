using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // Unity 입력 장치를 한 프레임의 요청으로 해석한다. 게임 상태를 읽거나 바꾸지 않는다.
    // 숫자 키 N은 스킬 사용자의 N번째 보유 스킬 칸이다. 스킬 ID나 종류를 알지 못한다.
    internal sealed class ReferenceInput
    {
        private static readonly Key[] SlotKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        private readonly Camera _camera;
        private readonly List<int> _castSlots = new List<int>();

        public bool Restart { get; private set; }
        public bool TogglePause { get; private set; }
        public bool Upgrade { get; private set; }
        // 이번 프레임에 눌린 스킬 칸(0부터, 키 순서). 조준할 마우스가 없으면 비어 있다.
        public IReadOnlyList<int> CastSlots => _castSlots;
        // 규칙 좌표계의 조준점. Unity 좌표 변환은 호스트의 책임이다.
        public Point2 Aim { get; private set; }

        public ReferenceInput(Camera camera)
        {
            _camera = camera;
        }

        public void Read()
        {
            Keyboard keyboard = Keyboard.current;
            Restart = Pressed(keyboard, Key.R);
            TogglePause = Pressed(keyboard, Key.P);
            Upgrade = Pressed(keyboard, Key.U);

            _castSlots.Clear();
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            // 카메라는 z = -10에 있고 규칙 평면은 z = 0이다.
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            Aim = new Point2(world.x, world.y);
            for (int i = 0; i < SlotKeys.Length; i++)
                if (Pressed(keyboard, SlotKeys[i])) _castSlots.Add(i);
        }

        private static bool Pressed(Keyboard keyboard, Key key) =>
            keyboard != null && keyboard[key].wasPressedThisFrame;
    }
}
