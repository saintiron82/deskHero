# Task Registry

**목표**: balance_dashboard_qt.py 파일 분리 리팩토링 (3200줄 → 모듈화)
**생성**: 2026-01-27
**갱신**: 2026-01-27

---

## 현재 상태

- **노드**: `NODE-5`
- **상태**: ✅ passed

---

## 트리 구조

✅ **ROOT**: balance_dashboard_qt.py 파일 분리 리팩토링 (3200... 
  ├─ ✅ **NODE-1**: 1. 공통 모듈 분리 (config_loader, game_formula... [LEAF]
  ├─ ✅ **NODE-2**: 2. 소형 탭 분리 (stage_sim, dps_calc, investm... [LEAF]
  ├─ ✅ **NODE-3**: 3. StatEditorTab 분리 (960줄 → 별도 모듈) [LEAF]
  ├─ ✅ **NODE-4**: 4. ComparisonAnalyzerTab 분리 (899줄 → 별도 모... [LEAF]
  ├─ ✅ **NODE-5**: 5. 메인 엔트리포인트 정리 및 테스트 ◀ CURRENT

---

## 노드 상세

### ROOT
- **목표**: balance_dashboard_qt.py 파일 분리 리팩토링 (3200줄 → 모듈화)
- **상태**: ✅ passed
- **깊이**: 0
- **자식**: NODE-1, NODE-2, NODE-3, NODE-4, NODE-5

### NODE-1
- **목표**: 1. 공통 모듈 분리 (config_loader, game_formulas)
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ✅ dashboard/config_loader.py 생성됨
  2. ✅ dashboard/game_formulas.py 생성됨
  3. ✅ from dashboard.config_loader import * 정상 import
  4. ✅ from dashboard.game_formulas import * 정상 import

### NODE-2
- **목표**: 2. 소형 탭 분리 (stage_sim, dps_calc, investment, cps_measure, terminal)
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ⏳ dashboard/tabs/__init__.py 생성
  2. ⏳ stage_simulator.py import 성공
  3. ⏳ dps_calculator.py import 성공
  4. ⏳ investment_guide.py import 성공
  5. ⏳ cps_measure.py import 성공
  6. ⏳ terminal.py import 성공

### NODE-3
- **목표**: 3. StatEditorTab 분리 (960줄 → 별도 모듈)
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT
- **테스트 항목**:
  1. ⏳ dashboard/tabs/stat_editor.py 생성
  2. ⏳ from dashboard.tabs import StatEditorTab 성공
  3. ⏳ StatEditorTab 클래스 포함 (960줄)

### NODE-4
- **목표**: 4. ComparisonAnalyzerTab 분리 (899줄 → 별도 모듈)
- **상태**: ✅ passed
- **깊이**: 1
- **리프**: Yes
- **부모**: ROOT

### NODE-5
- **목표**: 5. 메인 엔트리포인트 정리 및 테스트
- **상태**: ✅ passed
- **깊이**: 1
- **부모**: ROOT
