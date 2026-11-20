# Xeri UIFrame

Xeri UIFrame은 **Unity 7의 UI Toolkit·UGUI 위에서 애플리케이션 UI의 뼈대를 제공하는 패키지**입니다. Unity의 렌더러를 대체하지 않고, UI가 표시되는 위치(Presentation·Layer), 열려 있는 Screen과 Modal, Focus와 Gameplay Input 권한의 상태·수명을 관리합니다. 그 위에서 동작하는 선택 모듈로 Window와 Tray를 제공합니다.

프로젝트는 `UIRuntime`을 초기화한 다음 `UIContext`와 필요한 기능을 조합합니다. UI Toolkit 전용 구성과 UGUI를 포함한 mixed 구성을 지원합니다.

## 구성

| 구분 | 내용 | 문서 |
| --- | --- | --- |
| UIFrame 핵심 | `UIRuntime`/`UIContext`, Presentation·Layer, Screen·Modal, Focus·Input, Splash·Scene Fade·Spotlight | [UIFrame 핵심 API](core/index.md) |
| 선택 모듈 | Window, Tray. UIFrame 핵심 위에서 동작하며 필요한 경우에만 사용 | [Window](window/index.md), [Tray](tray.md) |
| 도구 | 관리 도구와 게임 내 진단 창 | [관리 도구](control-center.md) |

개별 UI 컨트롤(Picker, Popup, Drag & Drop, Bar 등)은 기반 패키지 Xeri Core(`com.inonego.xeri`)가 제공하며, UIFrame은 그중 일부를 사용합니다.

## 처음 사용하는 경우

1. [설치와 의존성](getting-started/installation.md)에서 Unity 버전과 패키지를 준비합니다.
2. [프로젝트 설정과 시작](getting-started/setup.md)에서 Presentation Layout, UI Settings, Bootstrapper 또는 Scene-owned Runtime을 연결합니다.
3. [UIFrame 핵심 API](core/index.md)에서 Screen·Modal·Presentation 사용 경로를 선택합니다.
4. 결과를 직접 조작해 보려면 [Validation Desktop](validation.md)을 실행합니다.

## 하고 싶은 작업별 안내

| 작업 | 문서 |
| --- | --- |
| Screen 열기·닫기, Gameplay Input 차단, Focus | [Screen과 입력](core/screens.md) |
| Layer 선택, transient UI 표시, Modal, Scene Fade | [Presentation](core/presentation.md) |
| 일시적인 로고·안내 화면 | [Splash](core/splash.md) |
| 특정 UI 요소 강조·주변 입력 차단 | [Spotlight](core/spotlight.md) |
| 이동·최소화·최대화 가능한 Window | [Window](window/index.md) |
| Window View 생성, UI 상태 저장·복원 | [Window View Source](window/view-source.md) |
| Window/작업 항목 선택·닫기·재정렬 | [Tray](tray.md) |
| 체력·진행 값과 증감 표시 | Xeri Core의 Bar(`com.inonego.xeri`) |
| 설정·레이아웃·씬 탐색 및 진단 | [관리 도구](control-center.md) |
| Unity 씬에서 기능을 자유롭게 실험 | [Validation Desktop](validation.md) |
| 시작·표시·입력 오류 해결 | [문제 해결](core/troubleshooting.md) |

## 구성 방식

- **UIRuntime**: Root Presentation, Main Context와 공통 Focus/Input/Transition 서비스 소유
- **UIContext**: Screen/Modal 상태, Focus 및 Gameplay Input 권한 소유
- **PresentationTarget**: 현재 Context의 Layer 또는 프로젝트 공통 Host destination 지정
- **Window Workspace**: 같은 표시 Layer에서 여러 Window의 상태·수명 관리
- **Source**: Screen/View를 생성하고 반환하는 프로젝트 코드

일반 기능은 `UIContext`의 상위 API에서 시작합니다. Child UI World, Scene-authored Layer, backend 확장처럼 조립 제어가 필요한 경우에만 하위 계약을 사용합니다.

## 상세 명세

- [전체 구조](architecture/overview.md): Runtime/Context/Presentation/Window 책임 경계
- [Presentation Architecture](architecture/presentation.md): topology, 순서, backend materialization, lifetime
- [Presentation Composition Contract](architecture/presentation-composition.md): Target·Layer Lease·Host·Scene-authored 조립 계약

게임 데이터, Window View 디자인, 디스크 직렬화와 Unity 자체 렌더링은 Xeri UIFrame이 대신 소유하지 않습니다. 개별 UI 컨트롤(Picker, Popup, Drag & Drop, Bar 등)은 기반 패키지 `com.inonego.xeri`의 영역입니다.
