@echo off
chcp 65001 >nul
echo Начинаем копирование мода HortigenesisIE...

:: ВАЖНО: Проверь и при необходимости измени путь к папке Mods в твоей игре!
set "DEST_DIR=C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Hortigenesis Immersive Expansion"

:: Указываем исходную папку (папка мода внутри корня, где лежит батник)
set "SRC_DIR=%~dp0Hortigenesis Immersive Expansion"

:: /MIR - Зеркалирование (удаляет лишнее в игре, копирует новое)
:: Так как Source и .git лежат снаружи этой папки, нам не нужны длинные списки исключений.
robocopy "%SRC_DIR%" "%DEST_DIR%" /MIR

:: Robocopy считает успешными коды до 7 включительно. 8 и выше — критическая ошибка.
if %ERRORLEVEL% GEQ 8 (
echo [ОШИБКА] Что-то пошло не так при копировании. Проверь пути!
) else (
echo [УСПЕХ] Мод успешно обновлен в папке игры!
)

pause