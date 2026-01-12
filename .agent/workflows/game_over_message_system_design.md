# 게임 오버 메시지 시스템 설계서

**작성자:** jina (Game Designer AI)
**작성일:** 2026-01-12
**대상 프로젝트:** DeskWarrior v1.0
**관련 기능:** CP009 - 조건부 게임 오버 메시지 시스템

---

## 1. Executive Summary

### 1.1 배경 및 목표
현재 DeskWarrior의 게임 오버 메시지는 단순 배열에서 랜덤 선택됩니다. 이 방식은:
- ❌ 플레이어의 성과를 반영하지 못함
- ❌ 초보자와 고수에게 같은 메시지 제공
- ❌ 특수 상황(보스 사망 등)에 대한 맥락 부족
- ❌ 재도전 동기 부여 부족

**목표:** 플레이어의 게임 데이터를 분석해 맥락에 맞는 메시지를 제공하여 **재도전율 30% 향상**

### 1.2 핵심 컨셉
**"Your Death, Your Story"** - 모든 실패는 고유한 이야기

---

## 2. 시스템 아키텍처

### 2.1 메시지 선택 로직

```
게임 오버 발생
    ↓
플레이어 데이터 수집
 - Level (도달 레벨)
 - Gold (총 골드)
 - Damage (총 데미지)
 - Kills (처치 수)
 - Death Type (보스/일반/시간초과)
    ↓
조건부 규칙 평가 (우선순위 순)
 1. 첫 플레이 감지 (priority: 100)
 2. 보스 사망 (priority: 95)
 3. 고레벨 타임아웃 (priority: 90)
 4. 성과 지표 (priority: 80-74)
    ↓
Level-Based 카테고리 선택
 - 1-3: 초보자 격려
 - 4-9: 유머
 - 10-19: 도발
 - 20-49: 서사적
 - 50+: 전설적
    ↓
Fallback 메시지 (조건 미충족 시)
```

### 2.2 Performance Metrics 정의

| 지표 | 조건식 | 의미 |
|------|--------|------|
| **RICH** | `gold > level * 100` | 골드 파밍 최적화 플레이어 |
| **POOR** | `gold < level * 50` | 효율 낮은 플레이어 |
| **GLASS_CANNON** | `damage / gold > 2.0` | 딜은 높지만 보상 낮음 |
| **EFFICIENT** | `damage / gold < 1.0` | 최소 입력 최대 효율 |

### 2.3 Death Context 감지

```csharp
// 예상 구현 로직 (lily에게 전달할 스펙)
public enum DeathType {
    Normal,      // 일반 몬스터에게 사망
    Boss,        // 보스 몬스터에게 사망
    Timeout      // 시간 초과
}

// GameManager에 추가 필요
public DeathType GetDeathContext() {
    if (RemainingTime <= 0) return DeathType.Timeout;
    if (CurrentMonster.IsBoss) return DeathType.Boss;
    return DeathType.Normal;
}
```

---

## 3. 메시지 데이터베이스

### 3.1 전체 메시지 통계
- **총 메시지 수:** 150+
- **조건부 메시지:** 30개 (8개 조건)
- **레벨별 메시지:** 100개 (5개 구간)
- **Fallback 메시지:** 9개

### 3.2 카테고리별 분포

| 카테고리 | 수량 | 목적 | 타이밍 |
|---------|------|------|--------|
| ENCOURAGE | 25 | 재도전 유도 | 초반 사망 (Lv1-9) |
| HUMOR | 40 | 긴장 완화 | 중반 사망 (Lv4-19) |
| TAUNT | 30 | 오기 자극 | 보스/고레벨 사망 |
| EPIC | 25 | 성취감 | 고레벨 사망 (Lv20+) |
| STATS | 20 | 데이터 피드백 | 모든 구간 |
| FALLBACK | 9 | 기본 안전망 | 조건 미충족 시 |

### 3.3 레벨 구간별 메시지 전략

#### Level 1-3 (초보자)
**전략:** 격려 위주, 부담 최소화
```
- "First time? Everyone's a noob at level 1."
- "Tutorial boss defeated you. Classic."
- "Level {level}. Baby steps, literally."
```

#### Level 4-9 (학습 구간)
**전략:** 유머로 좌절 완화
```
- "Evolution failed. Again."
- "Try using both hands next time."
- "Your mouse filed a complaint."
```

#### Level 10-19 (중급자)
**전략:** 도발로 오기 자극
```
- "Level {level} and you choked?"
- "So close to level 20. So far from victory."
- "Almost competent. Almost."
```

#### Level 20-49 (고급자)
**전략:** 서사적 표현으로 성취감 부여
```
- "Level {level}. You died a hero."
- "Legends don't die. They respawn."
- "Your sacrifice will not be forgotten."
```

#### Level 50+ (전문가)
**전략:** 전설적 표현, 존중
```
- "LEVEL {level}. You are LEGENDARY."
- "You didn't die. You ascended."
- "Even in death, you are LEGENDARY."
```

### 3.4 특수 조건 메시지

#### 보스 사망 (Priority: 95)
```json
{
  "condition": {
    "death_type": "boss",
    "level_min": 10
  },
  "messages": [
    "Boss: 'Thanks for the free XP!'",
    "Boss laughed so hard it evolved twice.",
    "Boss fight MVP: The Boss."
  ]
}
```

#### 부자 플레이어 (Priority: 80)
```json
{
  "condition": {
    "gold_per_level_min": 100
  },
  "messages": [
    "{gold}G? You died rich and happy.",
    "Richest corpse in the graveyard: You.",
    "Financial success: ✓ | Survival: ✗"
  ]
}
```

---

## 4. 변수 활용 전략

### 4.1 사용 가능한 변수
- `{level}` - 도달한 레벨
- `{gold}` - 획득한 총 골드
- `{damage}` - 총 데미지
- `{kills}` - 처치한 몬스터 수

### 4.2 변수 활용 예시

**데이터 중심 메시지:**
```
"FINAL: Lv{level} | {gold}G | {damage}DMG | {kills} Kills"
→ "FINAL: Lv47 | 12,450G | 98,234DMG | 47 Kills"
```

**서사적 메시지:**
```
"Level {level}. {damage} damage. Epic run."
→ "Level 35. 45,678 damage. Epic run."
```

**유머 메시지:**
```
"{gold}G can't buy you new fingers."
→ "8,234G can't buy you new fingers."
```

---

## 5. 구현 가이드 (for lily)

### 5.1 필요한 데이터 클래스 확장

```csharp
// Models/GameData.cs에 추가
public class GameOverMessageCondition
{
    [JsonPropertyName("level_min")]
    public int? LevelMin { get; set; }

    [JsonPropertyName("level_max")]
    public int? LevelMax { get; set; }

    [JsonPropertyName("death_type")]
    public string? DeathType { get; set; }

    [JsonPropertyName("gold_per_level_min")]
    public double? GoldPerLevelMin { get; set; }

    [JsonPropertyName("gold_per_level_max")]
    public double? GoldPerLevelMax { get; set; }

    [JsonPropertyName("damage_gold_ratio_min")]
    public double? DamageGoldRatioMin { get; set; }

    [JsonPropertyName("damage_gold_ratio_max")]
    public double? DamageGoldRatioMax { get; set; }

    [JsonPropertyName("is_first_play")]
    public bool? IsFirstPlay { get; set; }
}

public class GameOverMessageRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("condition")]
    public GameOverMessageCondition Condition { get; set; } = new();

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = new();
}
```

### 5.2 메시지 선택 로직 Pseudocode

```csharp
public string SelectGameOverMessage(GameStats stats)
{
    // 1. 조건부 규칙 평가 (우선순위 순)
    foreach (var rule in _rules.OrderByDescending(r => r.Priority))
    {
        if (EvaluateCondition(rule.Condition, stats))
        {
            return rule.Messages[Random.Next(rule.Messages.Count)];
        }
    }

    // 2. 레벨 기반 선택
    var levelRange = GetLevelRange(stats.Level);
    var category = SelectCategory(stats);
    var messages = _levelMessages[levelRange][category];
    if (messages.Count > 0)
    {
        return messages[Random.Next(messages.Count)];
    }

    // 3. Fallback
    return _fallbackMessages[Random.Next(_fallbackMessages.Count)];
}

private bool EvaluateCondition(GameOverMessageCondition condition, GameStats stats)
{
    if (condition.LevelMin.HasValue && stats.Level < condition.LevelMin) return false;
    if (condition.LevelMax.HasValue && stats.Level > condition.LevelMax) return false;
    if (condition.DeathType != null && stats.DeathType != condition.DeathType) return false;

    if (condition.GoldPerLevelMin.HasValue)
    {
        if ((double)stats.Gold / stats.Level < condition.GoldPerLevelMin) return false;
    }

    if (condition.DamageGoldRatioMin.HasValue)
    {
        if ((double)stats.Damage / stats.Gold < condition.DamageGoldRatioMin) return false;
    }

    // ... 기타 조건 평가

    return true;
}
```

### 5.3 변수 치환 로직

```csharp
public string FormatMessage(string template, GameStats stats)
{
    return template
        .Replace("{level}", stats.Level.ToString())
        .Replace("{gold}", stats.Gold.ToString())
        .Replace("{damage}", stats.Damage.ToString())
        .Replace("{kills}", stats.Kills.ToString());
}
```

---

## 6. 테스트 시나리오

### 6.1 Unit Test Cases

| 시나리오 | Input | 예상 Output |
|---------|-------|-------------|
| 첫 플레이 | Level=1, First=true | "First time? Everyone's a noob..." |
| 보스 사망 | Level=10, Death=Boss | "Boss: 'Thanks for the free XP!'" |
| 부자 플레이어 | Level=10, Gold=1500 | "{gold}G? You died rich and happy." |
| 고레벨 달성 | Level=50 | "LEVEL {level}. You are LEGENDARY." |
| 조건 미충족 | Level=5, Normal | Fallback 메시지 |

### 6.2 A/B 테스트 제안

**가설:** 조건부 메시지가 재도전율을 향상시킨다

**측정 지표:**
- 재시작 버튼 클릭률
- 게임 오버 → 재시작까지 시간
- 세션당 평균 재시작 횟수

**실험 그룹:**
- A: 기존 랜덤 메시지 (8개)
- B: 조건부 메시지 시스템 (150+개)

**예상 결과:**
- B 그룹 재시작 클릭률 +30%
- B 그룹 재시작 시간 -20%

---

## 7. 확장 로드맵

### Phase 1 (현재)
- ✅ 조건부 메시지 시스템 설계
- ✅ 150+ 메시지 데이터베이스
- ✅ 변수 치환 시스템

### Phase 2 (향후)
- 🔮 플레이 스타일 분석 ("You're a keyboard warrior!")
- 🔮 연속 실패 격려 시스템 (3회 연속 같은 레벨 사망 시)
- 🔮 개인화 메시지 ("You've died to this boss 5 times. Try upgrading?")

### Phase 3 (장기)
- 🔮 다국어 지원 (한국어/영어/일본어)
- 🔮 커뮤니티 메시지 투표 시스템
- 🔮 AI 생성 동적 메시지 (GPT 통합)

---

## 8. 성공 지표 (KPI)

| 지표 | 현재 (추정) | 목표 | 측정 방법 |
|------|------------|------|-----------|
| 재시작률 | 60% | 80% | Restart 클릭 / 총 게임 오버 |
| 평균 세션 플레이 수 | 3회 | 5회 | 세션당 게임 시작 횟수 |
| 게임 오버 화면 체류 시간 | 5초 | 3초 | 빠른 재도전 = 높은 동기 |
| 메시지 다양성 | 8개 | 150+ | 중복 없는 경험 |

---

## 9. 리스크 및 대응

| 리스크 | 확률 | 영향 | 대응 방안 |
|--------|------|------|-----------|
| 메시지가 너무 많아 품질 저하 | 중 | 중 | Lily와 함께 리뷰, 사용자 피드백 수집 |
| 조건 평가 성능 저하 | 저 | 저 | 조건 수 10개 이하 유지, 우선순위 최적화 |
| 부적절한 메시지 | 저 | 고 | 모든 메시지 사전 검수, 신고 시스템 |
| 변수 치환 버그 | 중 | 중 | Unit Test 100% 커버리지 |

---

## 10. Handoff to lily

### 10.1 구현 태스크

**User Story:**
```
As a player,
I want to see personalized game over messages that reflect my performance,
So that I feel recognized for my efforts and motivated to try again.
```

**Acceptance Criteria:**
- [ ] GameData.json에 조건부 메시지 구조 적용
- [ ] GameOverMessageCondition, GameOverMessageRule 클래스 구현
- [ ] 메시지 선택 로직 구현 (우선순위 평가)
- [ ] 변수 치환 시스템 구현 ({level}, {gold}, etc.)
- [ ] DeathType 감지 로직 추가 (Boss/Normal/Timeout)
- [ ] Unit Test 작성 (최소 5개 시나리오)
- [ ] 게임 오버 화면에 선택된 메시지 표시
- [ ] Fallback 메시지 동작 확인

### 10.2 제공 파일
- ✅ `C:\Users\saint\Game\DeskWarrior\.agent\game_over_messages_spec.json` - 전체 메시지 데이터베이스
- ✅ `C:\Users\saint\Game\DeskWarrior\.agent\workflows\game_over_message_system_design.md` - 본 문서

### 10.3 구현 우선순위
1. **P0 (필수):** 레벨 기반 메시지 선택 (5개 구간)
2. **P1 (중요):** 조건부 규칙 평가 (보스 사망, 첫 플레이)
3. **P2 (권장):** 성과 지표 조건 (RICH, POOR, etc.)
4. **P3 (추후):** 플레이 스타일 분석

### 10.4 테스트 요청
```csharp
// 테스트 시나리오
[Test]
public void GameOver_FirstPlay_ShowsEncouragingMessage()
{
    var stats = new GameStats { Level = 1, IsFirstPlay = true };
    var message = _messageSelector.SelectMessage(stats);

    Assert.IsTrue(message.Contains("First") || message.Contains("noob"));
}

[Test]
public void GameOver_BossDeath_ShowsTauntMessage()
{
    var stats = new GameStats { Level = 10, DeathType = "boss" };
    var message = _messageSelector.SelectMessage(stats);

    Assert.IsTrue(message.Contains("Boss") || message.Contains("boss"));
}
```

### 10.5 성능 요구사항
- 메시지 선택 시간: < 10ms
- 메모리 오버헤드: < 500KB (메시지 데이터)
- JSON 파싱 시간: < 50ms (게임 시작 시)

---

## 11. 메시지 철학

### 11.1 디자인 원칙
1. **Never Punish:** 플레이어를 비난하지 않음
2. **Always Motivate:** 재도전 동기 부여
3. **Respect Effort:** 높은 레벨 도달 시 존중 표현
4. **Inject Humor:** 좌절을 웃음으로 전환
5. **Show Data:** 투명한 성과 피드백

### 11.2 금지 사항
- ❌ 인신공격성 메시지
- ❌ 부정적 낙인 ("You suck", "Loser")
- ❌ 의미 없는 메시지 ("Game Over")
- ❌ 과도한 길이 (20단어 초과)

### 11.3 메시지 작성 가이드
- ✅ 간결함 (5-15 단어)
- ✅ 명확한 감정 (격려/유머/도발)
- ✅ 문화적 중립성
- ✅ 변수 활용으로 개인화
- ✅ 다양성 (같은 조건에서도 5+ 메시지)

---

## 12. 결론

이 시스템은 단순한 메시지 배열을 **플레이어 성과 인식 시스템**으로 진화시킵니다.

**핵심 가치:**
- 🎯 개인화된 경험
- 📊 데이터 기반 피드백
- 💪 재도전 동기 부여
- 😄 감정적 공감

**예상 효과:**
- 재시작률 +30%
- 플레이어 만족도 향상
- 게임 체류 시간 증가
- 소셜 미디어 공유 증가 ("이런 메시지 나왔어!" 스크린샷)

---

**Next Step:** lily에게 구현 요청 → QA 테스트 → 사용자 피드백 수집 → 메시지 개선 iteration

**문의:** jina (Game Design AI)
**승인 대기:** lily (Implementation Lead)
