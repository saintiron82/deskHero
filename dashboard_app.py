"""
DeskWarrior Balance Dashboard - Standalone Application
PyWebView 기반 데스크톱 앱
"""

import json
import os
import sys
import webview


def get_resource_path(relative_path: str) -> str:
    """PyInstaller 패키징 시 리소스 경로 처리 (읽기용)"""
    if hasattr(sys, '_MEIPASS'):
        return os.path.join(sys._MEIPASS, relative_path)
    else:
        return os.path.join(os.path.dirname(os.path.abspath(__file__)), relative_path)


def get_config_dir() -> str:
    """config 폴더 경로 (쓰기 가능한 경로)"""
    if hasattr(sys, '_MEIPASS'):
        # 패키징된 경우: exe와 같은 폴더의 config
        exe_dir = os.path.dirname(sys.executable)
        return os.path.join(exe_dir, 'config')
    else:
        # 개발 환경
        return os.path.join(os.path.dirname(os.path.abspath(__file__)), 'config')


class Api:
    """JavaScript에서 호출 가능한 Python API"""

    def load_config(self, filename: str) -> dict:
        """JSON 설정 파일 로드"""
        try:
            filepath = os.path.join(get_config_dir(), filename)
            with open(filepath, 'r', encoding='utf-8') as f:
                return {'success': True, 'data': json.load(f)}
        except Exception as e:
            return {'success': False, 'error': str(e)}

    def save_config(self, filename: str, data: dict) -> dict:
        """JSON 설정 파일 저장"""
        try:
            filepath = os.path.join(get_config_dir(), filename)

            # 백업 생성
            if os.path.exists(filepath):
                backup_path = filepath + '.backup'
                with open(filepath, 'r', encoding='utf-8') as f:
                    backup_data = f.read()
                with open(backup_path, 'w', encoding='utf-8') as f:
                    f.write(backup_data)

            # 저장
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)

            return {'success': True, 'message': f'{filename} 저장 완료'}
        except Exception as e:
            return {'success': False, 'error': str(e)}

    def list_configs(self) -> dict:
        """config 폴더의 JSON 파일 목록"""
        try:
            config_dir = get_config_dir()
            files = [f for f in os.listdir(config_dir) if f.endswith('.json')]
            return {'success': True, 'files': files, 'path': config_dir}
        except Exception as e:
            return {'success': False, 'error': str(e)}

    def get_config_path(self) -> str:
        """config 폴더 경로 반환"""
        return get_config_dir()


def main():
    # dashboard/index.html 경로
    html_path = get_resource_path('dashboard/index.html')

    if not os.path.exists(html_path):
        print(f"Error: {html_path} not found")
        sys.exit(1)

    # 파일 URL로 변환
    html_url = f'file:///{html_path.replace(os.sep, "/")}'

    # API 인스턴스 생성
    api = Api()

    # 웹뷰 창 생성
    window = webview.create_window(
        title='DeskWarrior Balance Dashboard',
        url=html_url,
        width=1400,
        height=900,
        resizable=True,
        min_size=(1024, 600),
        js_api=api
    )

    # 앱 실행
    webview.start()


if __name__ == '__main__':
    main()
