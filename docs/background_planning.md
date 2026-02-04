# 배경(Background) 생성 플랜

## 1. 개요 (Overview)

본 문서는 "Desk Warrior" 게임에 사용될 **지형별 배경 이미지** 생성 계획을 정의합니다.

### 📌 기존 배경 분석
- **파일명**: `bg_floor_sparse_pilot_v2.png`
- **특징**:
  - 16-bit 픽셀아트 스타일
  - 크로마키용 순수 녹색 배경 (`#00FF00`)
  - 바닥 요소만 포함 (돌, 풀, 흙)
  - 하단 50%에 요소 집중, 상단은 투명 처리 영역

---

## 2. 생성 대상 지형 (Terrain Types)

### **Phase 1: 기본 지형 (6종)**

| No. | 지형명 (영문) | 테마 색상 | 주요 요소 | 비고 |
|:---|:---|:---|:---|:---|
| 1 | **초원 (Grassland)** | Green | 풀, 꽃, 돌 | 기본 (완료) |
| 2 | **사막 (Desert)** | Yellow/Orange | 모래, 선인장, 바위 | |
| 3 | **설원 (Snow)** | White/Blue | 눈, 얼음, 고드름 | |
| 4 | **용암 (Lava)** | Red/Orange | 용암, 균열, 화산석 | |
| 5 | **동굴 (Cave)** | Gray/Brown | 바위, 종유석, 수정 | |
| 6 | **늪지 (Swamp)** | Dark Green/Purple | 웅덩이, 죽은 나무, 안개 | |

### **Phase 2: 특수 지형 (6종)**

| No. | 지형명 (영문) | 테마 색상 | 주요 요소 | 비고 |
|:---|:---|:---|:---|:---|
| 7 | **해변 (Beach)** | Blue/Beige | 모래, 조개, 파도 | |
| 8 | **숲 (Forest)** | Dark Green | 나무 그루터기, 버섯, 이끼 | |
| 9 | **심연 (Abyss)** | Purple/Black | 보라빛 균열, 어둠, 기포 | 보스전용 |
| 10 | **신전 (Temple)** | Gold/White | 타일, 기둥 파편, 빛 | 보스전용 |
| 11 | **하늘 (Sky)** | Blue/White | 구름, 바람, 깃털 | 보스전용 |
| 12 | **지옥 (Hell)** | Red/Black | 뼈, 불꽃, 균열 | 최종보스 |

---

## 3. 스타일 가이드라인 (Style Guidelines)

### 🎨 아트 스타일
- **스타일**: 16-bit JRPG (슈퍼 패미컴 시대)
- **배경색**: 순수 밝은 녹색 (`#00FF00` / RGB 0, 255, 0) - 크로마키용 투명 처리
- **디테일**: 굵은 외곽선, 선명한 픽셀 디테일

### 📷 카메라 & 원근감 (CRITICAL)
- **카메라 시점**: 사람 머리 높이에서 바닥을 내려다보는 **저고도 시점**
- **원근감 적용**:
  - **근거리 (하단)**: 오브젝트가 **크게** 보임
  - **원거리 (상단)**: 오브젝트가 **작게** 보임
- **희소 배치**: 오브젝트를 적게, 빈 공간을 많이 확보

### 📐 레이아웃 규칙
```
┌─────────────────────────┐
│                         │  ← 상단 50%: 순수 녹색 (캐릭터 영역)
│     #00FF00 영역        │
│                         │
├─────────────────────────┤
│  ◌   ◯  ▲  ◌  ▲       │  ← 하단 50%: 지형 요소 배치
│ ▲ ◌    ◯    ◌   ◯ ▲   │     (자연스러운 분포)
└─────────────────────────┘
```

---

## 4. 프롬프트 템플릿 (Prompt Template)

```
A pixel art game background floor. **16-bit JRPG style (Super Nintendo era).**
[TERRAIN_NAME] theme with [ELEMENT_1], [ELEMENT_2], and [ELEMENT_3].
Elements placed on the **BOTTOM HALF ONLY**. 
**TOP HALF must be COMPLETELY EMPTY - pure solid bright green #00FF00.**
Sparse distribution, not cluttered. Thick dark outlines, sharp pixel details.
BACKGROUND MUST BE SOLID PURE BRIGHT GREEN #00FF00 RGB(0,255,0) ONLY.
```

---

## 5. 파일 명명 규칙 (Naming Convention)

```
bg_floor_{terrain}_{variant}.png
```

예시:
- `bg_floor_desert_v1.png`
- `bg_floor_snow_v1.png`
- `bg_floor_lava_v1.png`

---

## 6. 저장 위치

| 폴더 | 용도 |
|:---|:---|
| `Resources/Images/Org/Background/` | 원본 (녹색 배경) |
| `Assets/Images/Production/Background/` | 최종 (투명 처리됨) |

---

## 7. 작업 순서

1. **Phase 1 기본 지형** (6종) 생성
2. 사용자 검토 및 승인
3. AutoAlphaChannel로 투명 처리
4. Production 폴더로 이동
5. **Phase 2 특수 지형** (6종) 생성
6. 최종 검토

---

## 8. 현재 상태 (속성 기반)

**게임 속성 시스템**: 물, 불, 바람, 노말, 빛, 어둠

| 속성 | 배경 | 상태 | 파일명 |
|:---|:---|:---|:---|
| 노말 (Normal) | 초원 (Grassland) | **[완료]** | `bg_floor_sparse_pilot_v2.png` |
| 불 (Fire) | 용암 (Lava) | **[완료]** | `bg_floor_lava_v1.png` |
| 바람 (Wind) | 사막 (Desert) | **[완료]** | `bg_floor_desert_v1.png` |
| 물 (Water) | 설원 (Snow) | **[완료]** | `bg_floor_snow_v1.png` |
| 물 (Water) | 해변 (Beach) | **[완료]** | `bg_floor_beach_v1.png` |
| 빛 (Light) | 신전 (Temple) | **[완료]** | `bg_floor_temple_v1.png` |
| 어둠 (Dark) | 동굴 (Cave) | **[완료]** | `bg_floor_cave_v1.png` |

---

**모든 기본 배경 생성 완료!** (7종)


