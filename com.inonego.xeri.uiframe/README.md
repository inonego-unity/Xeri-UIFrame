# Xeri UIFrame

`com.inonego.xeri.uiframe`은 Unity 애플리케이션 UI의 뼈대입니다. Presentation(표시 위치·순서), Screen·Modal, Focus와 Gameplay Input 권한을 관리하고, 그 위에서 동작하는 선택 모듈 Window와 Tray를 제공합니다.

## 구성

- **UIFrame 핵심** — `UIRuntime`, `UIContext`, Presentation, Screen, Modal, Focus, Input, Scene Fade·Splash·Spotlight
- **Window**(선택 모듈) — Workspace, Window Session, Simple/Application Window, persistence, View acquire/release와 UI-local session
- **Tray**(선택 모듈) — entry list, selection, close request, optional UITK reorder
- **도구** — 관리 도구(ControlCenter), 게임 내 진단 창

UIFrame 핵심은 Window와 Tray를 참조하지 않습니다. Picker, Popup, Drag & Drop, Bar 같은 개별 UI 컨트롤은 `com.inonego.xeri`가 제공합니다.

## Namespace

```text
inonego.Xeri.UI.Frame          UIFrame 핵심
inonego.Xeri.UI.Frame.Editor   관리 도구(Editor)
inonego.Xeri.UI.Window         Window 모듈
inonego.Xeri.UI.Window.Editor
inonego.Xeri.UI.Tray           Tray 모듈
```

`inonego.Xeri.UI`는 Xeri Core의 컨트롤 네임스페이스입니다.

## 문서

[Documentation~/index.md](Documentation~/index.md)에서 시작합니다.
