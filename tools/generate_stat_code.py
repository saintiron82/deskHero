"""
스탯 공식 코드 생성기
- StatFormulas.json을 읽어서 Python/C# 코드 생성
- 단일 소스에서 양쪽 코드가 동일하게 동작하도록 보장
"""

import json
import os
from datetime import datetime
from typing import Dict, Any

# ============================================================
# 공식 파서
# ============================================================

def parse_formula(formula: str) -> Dict[str, str]:
    """공식을 Python, C#으로 변환"""

    # Python 변환
    py_formula = formula
    py_formula = py_formula.replace('pow(', 'math.pow(')
    py_formula = py_formula.replace('min(', 'min(')
    py_formula = py_formula.replace('max(', 'max(')

    # C# 변환
    cs_formula = formula
    cs_formula = cs_formula.replace('pow(', 'Math.Pow(')
    cs_formula = cs_formula.replace('min(', 'Math.Min(')
    cs_formula = cs_formula.replace('max(', 'Math.Max(')

    return {
        'python': py_formula,
        'csharp': cs_formula
    }


# ============================================================
# Python 코드 생성
# ============================================================

def generate_python_code(formulas: Dict, constants: Dict) -> str:
    """Python 코드 생성"""

    lines = [
        '"""',
        'DeskWarrior 스탯 공식 (자동 생성)',
        f'생성일: {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}',
        '경고: 이 파일을 직접 수정하지 마세요!',
        '      config/StatFormulas.json을 수정 후 generate_stat_code.py 실행',
        '"""',
        '',
        'import math',
        '',
        '# ============================================================',
        '# 상수',
        '# ============================================================',
        '',
    ]

    # 상수
    for name, value in constants.items():
        lines.append(f'{name} = {value}')

    lines.extend([
        '',
        '',
        '# ============================================================',
        '# 공식 함수',
        '# ============================================================',
        '',
    ])

    # 함수
    for formula_id, formula_data in formulas.items():
        name = formula_data.get('name', formula_id)
        params = formula_data.get('params', [])
        formula = formula_data.get('formula', '')
        return_type = formula_data.get('return_type', 'double')
        description = formula_data.get('description', '')

        parsed = parse_formula(formula)
        py_formula = parsed['python']

        # 함수 시그니처
        param_str = ', '.join(params)

        lines.append(f'def calc_{formula_id}({param_str}):')
        lines.append(f'    """')
        lines.append(f'    {name}')
        if description:
            lines.append(f'    {description}')
        lines.append(f'    공식: {formula}')
        lines.append(f'    """')

        if return_type == 'int':
            lines.append(f'    return int({py_formula})')
        else:
            lines.append(f'    return {py_formula}')

        lines.append('')
        lines.append('')

    return '\n'.join(lines)


# ============================================================
# C# 코드 생성
# ============================================================

def generate_csharp_code(formulas: Dict, constants: Dict) -> str:
    """C# 코드 생성"""

    lines = [
        '// ============================================================',
        '// DeskWarrior 스탯 공식 (자동 생성)',
        f'// 생성일: {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}',
        '// 경고: 이 파일을 직접 수정하지 마세요!',
        '//       config/StatFormulas.json을 수정 후 generate_stat_code.py 실행',
        '// ============================================================',
        '',
        'using System;',
        '',
        'namespace DeskWarrior.Helpers',
        '{',
        '    /// <summary>',
        '    /// 스탯 공식 계산 (자동 생성)',
        '    /// </summary>',
        '    public static class StatFormulas',
        '    {',
        '        #region Constants',
        '',
    ]

    # 상수
    for name, value in constants.items():
        if isinstance(value, float):
            lines.append(f'        public const double {name} = {value};')
        else:
            lines.append(f'        public const int {name} = {value};')

    lines.extend([
        '',
        '        #endregion',
        '',
        '        #region Formula Methods',
        '',
    ])

    # 함수
    for formula_id, formula_data in formulas.items():
        name = formula_data.get('name', formula_id)
        params = formula_data.get('params', [])
        formula = formula_data.get('formula', '')
        return_type = formula_data.get('return_type', 'double')
        description = formula_data.get('description', '')

        parsed = parse_formula(formula)
        cs_formula = parsed['csharp']

        # C# 타입 매핑
        cs_return_type = 'int' if return_type == 'int' else 'double'

        # 파라미터 타입 추론 (기본 double)
        param_list = []
        for p in params:
            if 'level' in p or 'stack' in p or 'amount' in p or 'flat' in p:
                param_list.append(f'int {p}')
            else:
                param_list.append(f'double {p}')

        param_str = ', '.join(param_list)

        # 메서드 이름 (PascalCase)
        method_name = ''.join(word.capitalize() for word in formula_id.split('_'))

        lines.append(f'        /// <summary>')
        lines.append(f'        /// {name}')
        if description:
            lines.append(f'        /// {description}')
        lines.append(f'        /// 공식: {formula}')
        lines.append(f'        /// </summary>')
        lines.append(f'        public static {cs_return_type} Calc{method_name}({param_str})')
        lines.append(f'        {{')

        if return_type == 'int':
            lines.append(f'            return (int)({cs_formula});')
        else:
            lines.append(f'            return {cs_formula};')

        lines.append(f'        }}')
        lines.append('')

    lines.extend([
        '        #endregion',
        '    }',
        '}',
    ])

    return '\n'.join(lines)


# ============================================================
# 검증 코드 생성
# ============================================================

def generate_verification_code(formulas: Dict, constants: Dict) -> str:
    """검증용 테스트 코드 생성 (Python)"""

    lines = [
        '"""',
        'DeskWarrior 스탯 공식 검증 테스트',
        f'생성일: {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}',
        '"""',
        '',
        'from stat_formulas_generated import *',
        '',
        'def test_upgrade_cost():',
        '    """업그레이드 비용 테스트"""',
        '    # 레벨 1, base=100, growth=0.5, multi=1.5, interval=10',
        '    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 1)',
        '    expected = 150  # 100 * (1 + 1*0.5) * 1.5^(1/10) = 100 * 1.5 * 1.04 = 156',
        '    print(f"upgrade_cost(lv1): {result} (expected ~150)")',
        '    ',
        '    # 레벨 10',
        '    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 10)',
        '    print(f"upgrade_cost(lv10): {result}")',
        '    ',
        '    # 레벨 50',
        '    result = calc_upgrade_cost(100, 0.5, 1.5, 10, 50)',
        '    print(f"upgrade_cost(lv50): {result}")',
        '',
        '',
        'def test_damage():',
        '    """데미지 계산 테스트"""',
        '    # base=10, base_attack=5, attack_percent=0.2, crit=1, multi=1, combo=1',
        '    result = calc_damage(10, 5, 0.2, 1.0, 1.0, 1.0)',
        '    expected = 18  # (10+5) * 1.2 * 1 * 1 * 1 = 18',
        '    print(f"damage(no crit): {result} (expected {expected})")',
        '    ',
        '    # 크리티컬 발동 시',
        '    result = calc_damage(10, 5, 0.2, 2.0, 1.0, 1.0)',
        '    expected = 36  # 18 * 2 = 36',
        '    print(f"damage(crit): {result} (expected {expected})")',
        '    ',
        '    # 콤보 스택 3',
        '    combo_mult = calc_combo_multiplier(0, 3)  # 0% bonus, stack 3 = 8x',
        '    result = calc_damage(10, 5, 0.2, 1.0, 1.0, combo_mult)',
        '    expected = 144  # 18 * 8 = 144',
        '    print(f"damage(combo3): {result} (expected {expected})")',
        '',
        '',
        'def test_combo():',
        '    """콤보 시스템 테스트"""',
        '    # 콤보 배율',
        '    for stack in range(4):',
        '        mult = calc_combo_multiplier(0, stack)',
        '        print(f"combo stack {stack}: x{mult}")',
        '    ',
        '    # 콤보 허용 오차',
        '    for flex in range(0, 21, 5):',
        '        tolerance = calc_combo_tolerance(flex)',
        '        print(f"combo_flex {flex}: +/-{tolerance:.3f}s")',
        '',
        '',
        'def test_gold():',
        '    """골드 획득 테스트"""',
        '    # 기본',
        '    result = calc_gold_earned(100, 0, 0, 0, 0)',
        '    print(f"gold(base): {result}")',
        '    ',
        '    # 보너스 적용',
        '    result = calc_gold_earned(100, 10, 5, 0.2, 0.1)',
        '    expected = int((100 + 10 + 5) * 1.3)  # 115 * 1.3 = 149.5',
        '    print(f"gold(bonus): {result} (expected {expected})")',
        '',
        '',
        'if __name__ == "__main__":',
        '    print("=" * 50)',
        '    print(" 스탯 공식 검증 테스트")',
        '    print("=" * 50)',
        '    print()',
        '    ',
        '    test_upgrade_cost()',
        '    print()',
        '    test_damage()',
        '    print()',
        '    test_combo()',
        '    print()',
        '    test_gold()',
        '    ',
        '    print()',
        '    print("=" * 50)',
        '    print(" 테스트 완료")',
        '    print("=" * 50)',
    ]

    return '\n'.join(lines)


# ============================================================
# 메인
# ============================================================

def main():
    """메인 함수"""

    # 경로 설정
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.dirname(script_dir)
    config_dir = os.path.join(project_dir, 'config')
    helpers_dir = os.path.join(project_dir, 'Helpers')

    formulas_path = os.path.join(config_dir, 'StatFormulas.json')

    print("=" * 60)
    print(" DeskWarrior 스탯 공식 코드 생성기")
    print("=" * 60)

    # JSON 로드
    print(f"\n[1/4] 공식 파일 로드: {formulas_path}")
    try:
        with open(formulas_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        formulas = data.get('formulas', {})
        constants = data.get('constants', {})
        print(f"      - 공식 {len(formulas)}개 로드")
        print(f"      - 상수 {len(constants)}개 로드")
    except Exception as e:
        print(f"      오류: {e}")
        return

    # Python 코드 생성
    print("\n[2/4] Python 코드 생성")
    py_code = generate_python_code(formulas, constants)
    py_path = os.path.join(script_dir, 'stat_formulas_generated.py')
    with open(py_path, 'w', encoding='utf-8') as f:
        f.write(py_code)
    print(f"      -> {py_path}")

    # C# 코드 생성
    print("\n[3/4] C# 코드 생성")
    cs_code = generate_csharp_code(formulas, constants)
    cs_path = os.path.join(helpers_dir, 'StatFormulas.Generated.cs')
    os.makedirs(helpers_dir, exist_ok=True)
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(cs_code)
    print(f"      -> {cs_path}")

    # 검증 코드 생성
    print("\n[4/4] 검증 테스트 코드 생성")
    test_code = generate_verification_code(formulas, constants)
    test_path = os.path.join(script_dir, 'test_stat_formulas.py')
    with open(test_path, 'w', encoding='utf-8') as f:
        f.write(test_code)
    print(f"      -> {test_path}")

    print("\n" + "=" * 60)
    print(" 완료!")
    print("=" * 60)
    print("\n다음 단계:")
    print("  1. python test_stat_formulas.py  # 공식 검증")
    print("  2. dotnet build                  # C# 빌드 확인")
    print()


if __name__ == "__main__":
    main()
