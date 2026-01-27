# -*- mode: python ; coding: utf-8 -*-
import os

# 현재 디렉토리
BASE_DIR = os.path.dirname(os.path.abspath(SPEC))

a = Analysis(
    ['balance_dashboard_qt.py'],
    pathex=[BASE_DIR, os.path.join(BASE_DIR, 'tools')],
    binaries=[],
    datas=[
        # 생성된 공식 모듈 포함 (exe 내부)
        (os.path.join(BASE_DIR, 'tools', 'stat_formulas_generated.py'), 'tools'),
        # config는 포함하지 않음 - exe 외부의 config/ 폴더 참조
    ],
    hiddenimports=[
        'stat_formulas_generated',
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='BalanceDashboard',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
