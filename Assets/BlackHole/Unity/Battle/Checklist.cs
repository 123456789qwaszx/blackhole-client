using System;

namespace BlackHole.Unity
{
    internal enum StepState { Pending, Done, Failed }

    // 순서가 있는 체크리스트: 단계 이름과 상태. 시작·종료 과정의 어디까지 끝났는지를 콘솔에 보여 준다.
    // 바뀔 때마다 Version이 올라 읽는 쪽이 다시 그릴지 정할 수 있다.
    internal sealed class Checklist
    {
        private readonly string[] _names;
        private readonly StepState[] _states;

        public int Count => _names.Length;
        public int Version { get; private set; }

        public bool AllDone
        {
            get
            {
                foreach (StepState state in _states)
                {
                    if (state != StepState.Done)
                        return false;
                }

                return true;
            }
        }

        public Checklist(params string[] names)
        {
            _names = (string[])names.Clone();
            _states = new StepState[names.Length];
        }

        public string NameOf(int step) => _names[step];
        public StepState StateOf(int step) => _states[step];

        public void Reset()
        {
            Array.Clear(_states, 0, _states.Length);
            Version++;
        }

        public void Mark(int step, StepState state)
        {
            _states[step] = state;
            Version++;
        }

        public void Rename(int step, string name)
        {
            _names[step] = name;
            Version++;
        }
    }
}
