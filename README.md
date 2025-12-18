# NamuWikiViewer

**NamuWikiViewer**는 Windows App SDK (WinUI 3) 및 .NET 10을 기반으로 제작된 비공식 나무위키 뷰어 애플리케이션입니다.

> **면책 조항 (Disclaimer)**: 본 애플리케이션은 나무위키(namu.wiki)의 비공식 뷰어이며, 나무위키 및 그 운영진과는 어떠한 제휴나 관련이 없습니다.

## 스크린샷
<img width="2196" height="1380" alt="Screenshot" src="https://github.com/user-attachments/assets/1b3ac363-af7b-48f8-8a5d-b92d7c4eabac" /><br>
<img width="2196" height="1380" alt="Screenshot2" src="https://github.com/user-attachments/assets/41d7c5fe-c8a1-4673-a771-69e890557320" /><br>
<img width="2196" height="1380" alt="Screenshot3" src="https://github.com/user-attachments/assets/90f4c481-99ef-42cb-bf9c-d96dfeb72c56" />


## 개발자

- **airtaxi (Howon Lee)**


## 주요 기능

- **쾌적한 열람 환경**: 불필요한 요소를 제거하고 나무위키 열람에 최적화된 UI를 제공합니다.
- **광고 차단**: 설정에서 광고 차단 기능을 활성화하여 쾌적하게 문서를 읽을 수 있습니다.
- **방문 기록**: 방문한 문서 기록을 저장하고 관리할 수 있습니다. (설정에서 비활성화 가능)
- **다음에 보기**: 문서를 읽다가 나중에 보고 싶은 링크를 '다음에 보기'로 등록하여 하단 바에서 쉽게 접근할 수 있습니다.
- **글꼴 크기 조절**: 문서의 글꼴 크기를 확대하거나 축소하여 가독성을 높일 수 있습니다.
- **문서 도구**: 편집, 토론, 역사, 역링크 등 나무위키의 주요 기능에 빠르게 접근할 수 있습니다.
- **스크롤바 숨기기**: 몰입감 있는 열람을 위해 스크롤바를 숨길 수 있습니다.


## 저사양 모드 (Low Spec Mode)

메모리 용량이 낮은 기기를 위해 **페이지 이동 시 브라우저 인스턴스 저장 안함** 옵션을 제공합니다.

- 이 옵션을 활성화 하면 페이지 이동 시 브라우저 인스턴스를 저장하지 않습니다.
- 단, 뒤로가기 시 로딩 시간이 길어지며 스크롤 위치가 저장되지 않습니다.
- 메모리 용량이 낮은 경우 권장합니다.


## 사용된 오픈소스 라이브러리

이 프로젝트는 다음의 훌륭한 오픈소스 라이브러리들을 활용하여 제작되었습니다.

| 라이브러리 | 설명 | 라이선스 | 저장소 |
| :--- | :--- | :--- | :--- |
| **CommunityToolkit.Mvvm** | .NET Community Toolkit MVVM 라이브러리 | MIT License | [GitHub](https://github.com/CommunityToolkit/dotnet) |
| **DevWinUI** | WinUI 컨트롤 및 도구 모음 | MIT License | [GitHub](https://github.com/ghost1372/DevWinUI) |
| **Microsoft.WindowsAppSDK** | Windows App SDK | MIT License | [GitHub](https://github.com/microsoft/WindowsAppSDK) |
| **RestSharp** | REST 및 HTTP API 클라이언트 | Apache-2.0 License | [GitHub](https://github.com/restsharp/RestSharp) |
| **WinUIEx** | WinUI 확장 라이브러리 | MIT License | [GitHub](https://github.com/dotMorten/WinUIEx) |


## 라이선스 (License)

이 프로젝트는 **MIT License**를 따릅니다. 자세한 내용은 `LICENSE.txt` 파일을 참고하세요.

---
Copyright © airtaxi (Howon Lee). All rights reserved.
