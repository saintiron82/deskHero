# AutoAlphaChannel (Python v1.1)

이미지의 배경을 자동으로 투명하게 변환해주는 유틸리티입니다. GUI와 CLI 모드를 모두 지원합니다.

## 🚀 기능
- **Auto 모드**: 이미지의 배경색을 자동 감지하여 투명화
- **정밀 제어**: 색상 허용치(Tolerance) 및 가장자리 다듬기(Erosion) 지원
- **폴더 감시**: 폴더 내 파일 변경 실시간 감지
- **스포이드**: 클릭 한 번으로 배경색 지정
- **배치 처리**: CLI를 통한 대량 이미지 일괄 처리

## 💻 사용 방법 (GUI)
`AutoAlphaChannel.exe`를 실행하면 GUI 모드로 동작합니다.
1. 파일 또는 폴더를 드래그앤드롭하세요.
2. 옵션을 설정하고 '처리 시작'을 누르세요.

## 🤖 에이전트/스크립트 연동 가이드 (CLI)
다른 프로그램이나 AI 에이전트가 이 도구를 호출하여 사용할 수 있습니다.

### 기본 문법
```bash
AutoAlphaChannel.exe -i [입력경로] [옵션...]
```

### 파라미터 설명
| 인자 | 설명 | 기본값 | 예시 |
|------|------|--------|------|
| `-i`, `-input` | 입력 파일 또는 폴더 경로 (필수) | - | `-i "C:\Images"` |
| `-o`, `-output` | 출력 폴더 경로 (생략 시 원본 위치) | - | `-o "C:\Output"` |
| `-mode` | 0:Auto, 1:Single, 2:Tolerance | 0 | `-mode 0` |
| `-color` | 제거할 색상 (Hex 코드) | #FFFFFF | `-color "#FF00FF"` |
| `-tolerance` | 색상 허용 오차 (0~100) | 30 | `-tolerance 50` |
| `-erosion` | 가장자리 깎기 픽셀 수 (0~10) | 1 | `-erosion 2` |
| `-overwrite` | 원본 파일 덮어쓰기 여부 | False | `-overwrite` |

### Python 호출 예시
```python
import subprocess

# Auto 모드로 폴더 내 모든 이미지 처리
cmd = [
    "AutoAlphaChannel.exe",
    "-i", "C:/User/Images/Characters",
    "-mode", "0",
    "-erosion", "1"
]
subprocess.run(cmd)
```
