# Xeri UIFrame

Xeri UIFrame은 Unity 애플리케이션 UI의 **뼈대**입니다. UI가 어느 Layer에 어떤 순서로 표시되는지, 어떤 Screen과 Modal이 열려 있는지, Focus와 Gameplay Input을 누가 갖는지를 한곳에서 관리합니다. Unity의 UI Toolkit과 UGUI를 대체하지 않고 렌더링과 이벤트 전달은 Unity에 맡기며, 표시 위치·상태·수명·입력 권한만 소유합니다.

## 구성

| 구분 | 내용 | 네임스페이스 |
|---|---|---|
| **UIFrame 핵심** | `UIRuntime`, `UIContext`, Presentation(Layout·Layer·Target·Host), Screen·Modal, Focus·Input 권한, Scene Fade·Splash·Spotlight, UITK/UGUI backend, URP/HDRP 출력 | `inonego.Xeri.UI.Frame` |
| **선택 모듈** | Window(Workspace·Window Session·View·persistence), Tray(항목 표시·선택·닫기 요청·재정렬) | `inonego.Xeri.UI.Window`, `inonego.Xeri.UI.Tray` |
| **도구** | 관리 도구(설정·레이아웃·씬 탐색·진단·실행 상태), 게임 내 진단 창 | `inonego.Xeri.UI.Frame.Editor` |

UIFrame 핵심은 Window와 Tray를 알지 못합니다. 두 모듈은 UIFrame 위에서 동작하는 선택 사항이며 필요한 프로젝트만 사용합니다.

## 다른 패키지와의 관계

- **Xeri Core**(`com.inonego.xeri`)가 Picker, ContextMenu, Popup, Drag & Drop, Bar 같은 개별 UI 컨트롤(`inonego.Xeri.UI`)을 제공합니다. UIFrame은 Core 위에서 Badge, Drag & Drop 같은 일부 컨트롤을 사용하지만 컨트롤은 UIFrame을 알지 못합니다. 컨트롤만 필요한 프로젝트는 UIFrame 없이 Core만 사용합니다.
- Fabric, Game 등 다른 Xeri 패키지는 UIFrame에 의존하지 않습니다.

## Package

- UPM package: `com.inonego.xeri.uiframe`
- Runtime assembly: `inonego.Xeri.UI.Frame` (선택 모듈 Window·Tray 포함)
- Editor assembly: `inonego.Xeri.UI.Frame.Editor`
- Base dependency: `com.inonego.xeri`

## 문서

- [Xeri UIFrame Manual](com.inonego.xeri.uiframe/Documentation~/index.md)
- [Window와 USS 테마](com.inonego.xeri.uiframe/Documentation~/window/index.md)
- [Window View Source와 저장](com.inonego.xeri.uiframe/Documentation~/window/view-source.md)
- [Presentation Architecture](com.inonego.xeri.uiframe/Documentation~/architecture/presentation.md)
- [문서 작성 규칙](Docs/documentation-style.md)
- [문서 사이트 유지보수](Docs/site-maintenance.md)

## 로컬 문서 빌드

```pwsh
./build-docs.ps1
```

API metadata 생성에는 패키지의 각 production assembly에 대응하는 Unity 생성 `.csproj`가 필요합니다. 준비·검증 절차는 [문서 사이트 유지보수](Docs/site-maintenance.md)를 참고합니다.
