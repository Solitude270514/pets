@echo off
chcp 65001 >nul
title 像素編輯器
cd /d "%~dp0"
echo 像素編輯器：關掉這個視窗就會停止
python editor.py
if errorlevel 1 (
  echo.
  echo 啟動失敗：請確認已安裝 Python，以及 numpy、Pillow 套件（pip install numpy pillow）
  pause
)
