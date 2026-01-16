# 몬스터 생성 진행표 (Monster Generation Progress)

## 1. 진행 현황 요약 (Summary)
*   **전체 목표**: 25종 (일반 20 + 보스 5)
*   **기본 이미지(Base) 완료**: 12 / 25
*   **색상 변형(Variations) 완료**: 20 / 25 (쥐 포함)
*   **현재 상태**: **진행 중**
*   **재개 지점**: **듀라한 (Dullahan)**

---

## 2. 기본 이미지 생성 로그 (Base Image Generation Log)
각 몬스터의 기본(Normal, Green Type) 이미지 생성 여부를 기록합니다.
**출력 파일명**: `monster_{종족}_{속성}.png` (예: `monster_slime_fire.png`)

### **A. 일반 몬스터 (Standard)**
| No. | 몬스터 | 파일명 (Base) | 상태 | 비고 |
| :-- | :--- | :--- | :--- | :--- |
| 1 | **슬라임** | `monster_slimeA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 2 | **박쥐** | `monster_batA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 3 | **스켈레톤** | `monster_skeletonA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 4 | **고블린** | `monster_goblinA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 5 | **오크** | `monster_orcA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 6 | **유령** | `monster_ghostA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 7 | **골렘** | `monster_golemA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 8 | **버섯** | `monster_mushroomA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 9 | **거미** | `monster_spiderA.png` | **[완료]** (B~F 생성 완료) | 3/4 Front View |
| 10 | **좀비** | `monster_zombieA.png` | **[완료]** | |
| 11 | **밴시** | `monster_bansheeA.png` | **[완료]** | |
| 12 | **가고일** | `monster_gargoyleA.png` | **[완료]** | |
| 13 | **켄타우로스** | `monster_centaurA.png` | **[완료]** | 3/4 Front-left View |
| 14 | **메두사** | `monster_medusaA.png` | **[완료]** | 3/4 Front-left View |
| 15 | **바실리스크** | `monster_basiliskA.png` | **[대기]** | |
| 16 | **그리폰** | `monster_griffinA.png` | **[대기]** | |
| 17 | **키메라** | `monster_chimeraA.png` | **[대기]** | |
| 18 | **눈알괴물** | `monster_eyeballA.png` | **[완료]** (B~F 생성 완료) | |
| 19 | **정령** | `monster_elementalA.png` | **[완료]** (B~F 생성 완료) | |
| 20 | **쥐** | `monster_ratA.png` | **[완료]** (B~F 생성 완료) | |
| 21 | **듀라한** | `monster_dullahanA.png` | **[완료]** (B~F 생성 완료) | |
| 22 | **하피** | `monster_harpyA.png` | **[완료]** (B~F 생성 완료) | sexy, cute |
| 23 | **인어(여)** | `monster_female_mermaid.png` | **[완료]** (B~F 생성 완료) | sexy, cute |
| 24 | **인어(남)** | `monster_male_mermaid.png` | **[완료]** (B~F 생성 완료) | |

---

### **B. 보스 몬스터 (Boss)**
| No. | 몬스터 | 파일명 | 상태 | 비고 |
| :-- | :--- | :--- | :--- | :--- |
| 21 | **드래곤** | `boss_dragonA.png` | **[완료]** (Base 생성 완료) | |
| 22 | **기사** | `boss_knightA.png` | **[완료]** (Base 생성 완료) | |
| 23 | **리치** | `boss_lichA.png` | **[완료]** (Base 생성 완료) | |
| 24 | **마왕** | `boss_demonA.png` | **[완료]** (Base 생성 완료) | |
| 25 | **사신** | `boss_reaperA.png` | **[완료]** (Base 생성 완료) | |
| 26 | **서큐버스** | `monster_sercubusA.png` | **[완료]** (Base 생성 완료) | sexy, cute |
---

## 3. 베리에이션 생성 로그 (Variation Generation Log)
Python 스크립트를 통한 4가지 추가 색상(B, C, D, E) 생성 여부입니다.

*   [x] **스크립트 작성**: `generate_monsters.py` (완료)
*   [x] **전체 실행**: 모든 기본 이미지에 대해 변환 수행 (정령까지 완료)

---

## 4. 파일 저장 위치
*   `c:\Users\saintiron\deskHero\Assets\Images\Raw_Green`
