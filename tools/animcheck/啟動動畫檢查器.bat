@echo off
chcp 65001 >nul
title 動畫檢查器
cd /d "%~dp0"
echo 動畫檢查器：關掉這個視窗就會停止
python animcheck.py
if errorlevel 1 (
  echo.
  echo 啟動失敗：請確認已安裝 Python，以及 numpy、Pillow 套件（pip install numpy pillow）
  pause
)
