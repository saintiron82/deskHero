# DeskWarrior 스탯 시스템 설계서

## 개요
- 인게임 스탯: 7종 (골드로 업그레이드)
- 영구 스탯: 19종 (크리스탈로 업그레이드)
- 총 26종

---

## 인게임 스탯 (골드) - 7종

| ID | 이름 | 효과 | 기본값 | 최대 |
|----|------|------|--------|------|
| keyboard_power | 키보드 공격력 | 키보드 입력 시 +n 데미지 | 1 | 무제한 |
| mouse_power | 마우스 공격력 | 마우스 입력 시 +n 데미지 | 1 | 무제한 |
| gold_flat | 골드+ | 몬스터 처치 시 +n 골드 | 0 | 무제한 |
| gold_multi | 골드* | 골드 획득량 ×n% | 0% | 무제한 |
| time_thief | 시간 도둑 | 처치 시 +n초 (최대 기본시간까지) | 0 | 기본시간 |
| combo_flex | 콤보 유연성 | 리듬 허용 오차 ±n초 | 0.01 | TBD |
| combo_damage | 콤보 데미지 | 콤보 시 데미지 +n% | 0% | TBD |

---

## 영구 스탯 (크리스탈) - 19종

### A. 기본 능력 (5종)
| ID | 이름 | 효과 | 기본값 |
|----|------|------|--------|
| base_attack | 기본 공격력 | 모든 공격에 +n | 0 |
| attack_percent | 공격력 배수 | 데미지 ×n% | 0% |
| crit_chance | 크리티컬 확률 | +n% | 10% |
| crit_damage | 크리티컬 배율 | ×n | 2.0 |
| multi_hit | 멀티히트 확률 | n% 확률로 2배 타격 | 0% |

### B. 재화 보너스 (4종)
| ID | 이름 | 효과 | 기본값 |
|----|------|------|--------|
| gold_flat_perm | 영구 골드+ | 처치 시 +n 골드 | 0 |
| gold_multi_perm | 영구 골드* | 골드 ×n% | 0% |
| crystal_flat | 크리스탈+ | 보스 드롭 +n | 0 |
| crystal_multi | 크리스탈* | 드롭 확률 +n% | 0% |

### C. 유틸리티 (2종)
| ID | 이름 | 효과 | 기본값 |
|----|------|------|--------|
| time_extend | 기본 시간 연장 | 제한시간 +n초 | 30초 |
| upgrade_discount | 업그레이드 할인 | 골드 비용 -n% | 0% |

### D. 시작 보너스 (8종)
| ID | 이름 | 효과 | 기본값 |
|----|------|------|--------|
| start_level | 시작 레벨 | 레벨 +n에서 시작 | 1 |
| start_gold | 시작 골드 | 골드 +n에서 시작 | 0 |
| start_keyboard | 시작 키보드 | keyboard_power +n | 0 |
| start_mouse | 시작 마우스 | mouse_power +n | 0 |
| start_gold_flat | 시작 골드+ | gold_flat +n | 0 |
| start_gold_multi | 시작 골드* | gold_multi +n% | 0 |
| start_combo_flex | 시작 콤보유연성 | combo_flex +n | 0 |
| start_combo_damage | 시작 콤보데미지 | combo_damage +n% | 0 |

---

## 데미지 계산 공식

```
① 기본 = BasePower (keyboard/mouse_power)
② +가산 = 기본 + base_attack
③ ×배수 = ② × (1 + attack_percent)
④ ×크리티컬 = ③ × crit_damage (확률: crit_chance)
⑤ ×멀티히트 = ④ × 2 (확률: multi_hit)
⑥ ×콤보 = ⑤ × (1 + combo_damage) (리듬 발동 시, 스택별 2/4/8배)

최종 데미지 = (int)⑥
```

---

## 골드 획득 공식

```
기본 = 몬스터 기본 골드 + (레벨 × 성장치)
+가산 = 기본 + gold_flat + gold_flat_perm
×배수 = +가산 × (1 + gold_multi + gold_multi_perm)

획득 골드 = (int)×배수
```

---

## 콤보 시스템

### 발동 조건
- 연속 입력의 시간 간격이 일정할 때 (허용 오차: ±0.01초 + combo_flex)

### 콤보 스택
| 스택 | 배율 | 조건 |
|------|------|------|
| 1 | ×2 | 리듬 발동 |
| 2 | ×4 | 3초 내 재발동 |
| 3 | ×8 | 3초 내 재발동 (최대) |

### 유지/해제
- 유지: 3초 내 리듬 입력 유지
- 해제: 3초 경과 또는 리듬 깨짐

---

## 시간 도둑 제한

- 최대 연장: 기본 시간까지 (예: 40초 제한 → +40초까지)
- 즉, 최대 2배 시간까지만 가능

---

## 재화 시스템

### 골드 (인게임)
- 획득: 몬스터 처치
- 사용: 인게임 스탯 7종 업그레이드
- 리셋: 세션 종료 시

### 크리스탈 (영구)
- 획득: 보스 드롭, 골드 변환 (1000:1), 업적
- 사용: 영구 스탯 19종 업그레이드
- 리셋: 없음 (영구 보존)

---

## 성장 곡선

### 비용 공식

```
cost = base × (1 + level × growth_rate) × multiplier^(level / softcap_interval)
```

### 파라미터 설명

| 파라미터 | 역할 | 조절 효과 |
|----------|------|----------|
| base_cost | 1레벨 기본 비용 | 전체 비용 스케일 |
| growth_rate | 선형 증가율 | 초반~중반 기울기 |
| multiplier | 지수 배율 | 후반 가파름 |
| softcap_interval | 지수 적용 간격 | 소프트캡 주기 |

### 조절 가이드

| 목표 | 방법 |
|------|------|
| 초반 빠르게 | base ↓, growth_rate ↓ |
| 중반 완만하게 | softcap_interval ↑ |
| 후반 벽 만들기 | multiplier ↑ |
| 전체 쉽게 | 모든 값 ↓ |

### 예시 계산 (keyboard_power 기준)

```
base=100, growth=0.5, multi=1.5, interval=10

Lv1:  100 × 1.5  × 1.5^0 = 150
Lv5:  100 × 3.5  × 1.5^0 = 350
Lv10: 100 × 6.0  × 1.5^1 = 900
Lv20: 100 × 11.0 × 1.5^2 = 2,475
Lv50: 100 × 26.0 × 1.5^5 = 19,760
```

### Config 파일

- 인게임: `config/InGameStatGrowth.json`
- 영구: `config/PermanentStats.json`

---

## 버전
- v1.0 - 2026-01-15 초안 확정
- v1.1 - 2026-01-16 성장 곡선 설계 추가
