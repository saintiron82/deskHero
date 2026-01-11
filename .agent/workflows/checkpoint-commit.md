---
description: 체크포인트 완료 시 자동 커밋 실행
---

// turbo-all

## 체크포인트 완료 후 커밋 절차

1. `docs/CHECKPOINTS.md` 업데이트
   - 해당 CP 상태를 "✓"로 변경
   - 회고 섹션 작성

2. 모든 변경사항 스테이징
```powershell
git add -A
```

3. 커밋 (형식: `feat(CPnnn): 설명`)
```powershell
git commit -m "feat(CP00X): checkpoint description"
```

4. 다음 체크포인트 준비
   - CHECKPOINTS.md에서 다음 CP 상태를 "진행중"으로 변경
