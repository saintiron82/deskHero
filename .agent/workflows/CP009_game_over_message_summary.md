# CP009: 조건부 게임 오버 메시지 시스템

## Quick Summary

**현재:** 8개 메시지 랜덤 선택
**개선:** 150+ 메시지, 플레이어 성과 기반 선택

---

## 핵심 개선사항

### Before
```json
"game_over_messages": [
  "Your finger needs a gym membership.",
  "Monster: 'Is that all you got?'",
  ...
]
```
→ 랜덤 선택, 맥락 없음

### After
```json
"game_over_messages": {
  "conditions": [
    {
      "condition": { "death_type": "boss", "level_min": 10 },
      "messages": ["Boss: 'Thanks for the free XP!'", ...]
    }
  ],
  "level_based": {
    "1-3": { "encourage": [...], "humor": [...] },
    "50+": { "epic": [...] }
  }
}
```
→ 조건 평가 → 맥락에 맞는 메시지

---

## 메시지 선택 플로우

```
게임 오버 발생
    ↓
데이터 수집: Level, Gold, Damage, DeathType
    ↓
조건 평가 (우선순위 순)
 ✓ 첫 플레이? → "First time? Everyone's a noob..."
 ✓ 보스 사망? → "Boss laughed so hard..."
 ✓ 고레벨? → "LEVEL {level}. You are LEGENDARY."
    ↓
레벨 구간별 카테고리 선택
 - Lv1-3: 격려형
 - Lv4-9: 유머형
 - Lv10-19: 도발형
 - Lv20-49: 서사형
 - Lv50+: 전설형
    ↓
변수 치환
 "{level}" → "47"
 "{gold}" → "12,450G"
    ↓
메시지 표시
```

---

## 메시지 예시

### Level 1 (첫 플레이)
```
"First time? Everyone's a noob at level 1."
"Tutorial boss defeated you. Classic."
```

### Level 15 (보스 사망)
```
"Boss: 'Thanks for the free XP!'"
"Boss laughed so hard it evolved twice."
```

### Level 35 (고레벨 달성)
```
"Level 35. You died a hero."
"Legends don't die. They respawn."
```

### Level 50+ (전설)
```
"LEVEL 52. You are LEGENDARY."
"You didn't die. You ascended."
```

### 부자 플레이어
```
"12,450G? You died rich and happy."
"Financial success: ✓ | Survival: ✗"
```

---

## 구현 파일

### 제공 파일
1. **메시지 데이터베이스**
   - `C:\Users\saint\Game\DeskWarrior\.agent\game_over_messages_spec.json`
   - 150+ 메시지, 8개 조건, 5개 레벨 구간

2. **설계 문서**
   - `C:\Users\saint\Game\DeskWarrior\.agent\workflows\game_over_message_system_design.md`
   - 전체 시스템 아키텍처, 구현 가이드

### 수정 대상 파일
1. `C:\Users\saint\Game\DeskWarrior\config\GameData.json`
   - 메시지 구조 변경
2. `C:\Users\saint\Game\DeskWarrior\Models\GameData.cs`
   - 새 데이터 클래스 추가
3. `C:\Users\saint\Game\DeskWarrior\Managers\GameManager.cs`
   - 메시지 선택 로직 추가
   - DeathType 감지 추가

---

## Acceptance Criteria

- [ ] 레벨별 메시지 선택 동작
- [ ] 보스 사망 시 전용 메시지 표시
- [ ] 첫 플레이 시 격려 메시지 표시
- [ ] 변수 치환 동작 ({level}, {gold}, {damage})
- [ ] Fallback 메시지 동작
- [ ] 성능: 메시지 선택 < 10ms

---

## 기대 효과

| 지표 | Before | After (목표) |
|------|--------|--------------|
| 메시지 다양성 | 8개 | 150+ |
| 재시작률 | ~60% | 80% |
| 개인화 수준 | 0% | 100% |
| 플레이어 몰입도 | 중 | 고 |

---

## Next Step

1. lily에게 구현 요청
2. QA 테스트
3. 사용자 피드백 수집
4. 메시지 품질 개선 iteration

---

**작성:** jina (Game Designer)
**구현:** lily (Developer)
**승인:** Pending
