"""
설정 로더 및 matplotlib 폰트 설정
"""

import json
import os
import sys

import matplotlib
import matplotlib.font_manager as fm


def _get_tools_dir() -> str:
    """tools 디렉토리 경로 (exe/Python 모두 지원)"""
    if hasattr(sys, '_MEIPASS'):
        # PyInstaller exe 실행 시
        return os.path.join(sys._MEIPASS, 'tools')
    else:
        # Python 직접 실행 시
        # dashboard/ 폴더의 상위 폴더 기준
        base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        return os.path.join(base_dir, 'tools')


def get_config_dir() -> str:
    """config 디렉토리 경로"""
    if hasattr(sys, '_MEIPASS'):
        # 패키징된 exe 실행 시: exe가 dist/ 폴더에 있으므로 상위 폴더의 config 사용
        exe_dir = os.path.dirname(sys.executable)
        # dist/BalanceDashboard.exe -> dist/../config = config
        config_dir = os.path.join(os.path.dirname(exe_dir), 'config')
        if os.path.exists(config_dir):
            return config_dir
        # 폴백: exe 옆의 config 폴더
        return os.path.join(exe_dir, 'config')
    # dashboard/ 폴더의 상위 폴더 기준
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    return os.path.join(base_dir, 'config')


def load_json(filename: str) -> dict:
    """JSON 파일 로드"""
    filepath = os.path.join(get_config_dir(), filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        return json.load(f)


def save_json(filename: str, data: dict):
    """JSON 파일 저장 (자동 백업)"""
    filepath = os.path.join(get_config_dir(), filename)
    if os.path.exists(filepath):
        with open(filepath, 'r', encoding='utf-8') as f:
            backup = f.read()
        with open(filepath + '.backup', 'w', encoding='utf-8') as f:
            f.write(backup)
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


def setup_matplotlib_fonts():
    """matplotlib 한글 폰트 설정"""
    matplotlib.use('QtAgg')

    # 한글 폰트 설정 (Windows: Malgun Gothic)
    plt_font_path = None
    for font in fm.fontManager.ttflist:
        if 'Malgun' in font.name or 'malgun' in font.fname.lower():
            plt_font_path = font.fname
            break

    if plt_font_path:
        matplotlib.rcParams['font.family'] = fm.FontProperties(fname=plt_font_path).get_name()
    else:
        # 폴백: 시스템 기본 sans-serif
        matplotlib.rcParams['font.family'] = 'sans-serif'
        matplotlib.rcParams['font.sans-serif'] = ['Malgun Gothic', 'NanumGothic', 'Arial Unicode MS', 'DejaVu Sans']

    matplotlib.rcParams['axes.unicode_minus'] = False  # 마이너스 기호 깨짐 방지


def init_stat_formulas():
    """stat_formulas_generated 모듈 초기화 및 반환"""
    sys.path.insert(0, _get_tools_dir())
    import stat_formulas_generated as SF
    return SF


# 모듈 로드 시 SF 초기화
SF = init_stat_formulas()
