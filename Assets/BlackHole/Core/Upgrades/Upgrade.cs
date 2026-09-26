using System;

namespace BlackHole.Core
{
    // 업그레이드가 수치를 바꾸는 방식.
    // 같은 수치에 붙은 업그레이드는 (기본값 + Σ더하기) × (1 + Σ비율) × Π곱하기로 합친다(UpgradeTable).
    public enum UpgradeOperation
    {
        // 기본값에 더한다. 예: 질량 단계 +1.
        Add,
        // 비율을 더한다. 0.25는 +25%다. 여러 개는 합으로 쌓인다(원작의 "Currently: 175%").
        Percent,
        // 곱한다. 여러 개는 곱으로 쌓인다(원작의 황금 자릿수: ×10 두 번이면 ×100).
        Multiply,
    }

    // 업그레이드 하나: 어느 수치를, 어떤 방식으로, 얼마만큼 바꾸는가.
    // 수치 이름은 그 수치를 가져가는 시스템이 정한다. 업그레이드 시스템은 이름을 해석하지 않고, 같은 수치끼리 모으는 열쇠로만 쓴다.
    // 값의 범위(정수인가, 0 ~ 1인가)도 가져가는 시스템이 본다. 여기서는 유한한 수인지만 본다.
    public readonly struct Upgrade
    {
        public string Stat { get; }
        public UpgradeOperation Operation { get; }
        public float Value { get; }

        public Upgrade(string stat, UpgradeOperation operation, float value)
        {
            if (string.IsNullOrWhiteSpace(stat))
                throw new ArgumentException("수치 이름이 비어 있다.", nameof(stat));

            if (!Enum.IsDefined(typeof(UpgradeOperation), operation))
                throw new ArgumentOutOfRangeException(nameof(operation));

            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "유한한 값이 필요하다.");

            Stat = stat;
            Operation = operation;
            Value = value;
        }

        public override string ToString() => $"{Stat} {Operation} {Value}";
    }
}
