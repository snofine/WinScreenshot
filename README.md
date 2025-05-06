# WinScreenshot

Удобный инструмент для создания скриншотов в Windows с поддержкой горячих клавиш.

## Возможности

- Создание скриншотов выделенной области
- Создание скриншотов всего экрана
- Создание скриншотов активного окна
- Настраиваемые горячие клавиши для каждого типа скриншота
- Автоматическое сохранение в выбранную папку
- Копирование в буфер обмена
- Работа в системном трее

## Требования

- Windows 10 или новее
- .NET 6.0 или новее

## Установка

### Вариант 1: Установщик (рекомендуется)
1. Скачайте `WinScreenshot-Setup.exe` из раздела [Releases](https://github.com/snofine/WinScreenshot/releases)
2. Запустите установщик и следуйте инструкциям
3. Программа будет установлена в Program Files и добавлена в меню Пуск

### Вариант 2: Портативная версия
1. Скачайте `WinScreenshot-Portable.zip` из раздела [Releases](https://github.com/snofine/WinScreenshot/releases)
2. Распакуйте архив в любую папку
3. Запустите `WinScreenshot.exe`

## Использование

После запуска программа сворачивается в системный трей. Доступны следующие действия:

- Клик правой кнопкой мыши по иконке в трее открывает меню
- Настройка горячих клавиш через меню "Настройки"
- По умолчанию используется клавиша PrintScreen для создания скриншота выделенной области

## Сборка из исходного кода

1. Клонируйте репозиторий:
```bash
git clone https://github.com/snofine/WinScreenshot.git
```

2. Откройте решение в Visual Studio 2022
3. Восстановите пакеты NuGet
4. Соберите проект

## Лицензия

MIT License

---

# WinScreenshot

A convenient screenshot tool for Windows with hotkey support.

## Features

- Capture selected area
- Capture full screen
- Capture active window
- Customizable hotkeys for each screenshot type
- Automatic saving to selected folder
- Copy to clipboard
- System tray operation

## Requirements

- Windows 10 or newer
- .NET 6.0 or newer

## Installation

### Option 1: Installer (recommended)
1. Download `WinScreenshot-Setup.exe` from the [Releases](https://github.com/snofine/WinScreenshot/releases) section
2. Run the installer and follow the instructions
3. The program will be installed in Program Files and added to the Start menu

### Option 2: Portable version
1. Download `WinScreenshot-Portable.zip` from the [Releases](https://github.com/snofine/WinScreenshot/releases) section
2. Extract the archive to any folder
3. Run `WinScreenshot.exe`

## Usage

After launch, the application minimizes to the system tray. Available actions:

- Right-click the tray icon to open the menu
- Configure hotkeys through the "Settings" menu
- PrintScreen key is used by default for capturing selected area

## Building from source

1. Clone the repository:
```bash
git clone https://github.com/snofine/WinScreenshot.git
```

2. Open the solution in Visual Studio 2022
3. Restore NuGet packages
4. Build the project

## License

MIT License 