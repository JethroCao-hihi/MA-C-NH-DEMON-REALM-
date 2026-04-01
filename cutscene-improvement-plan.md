# Kế hoạch cải thiện hệ thống Cutscene (evidence-first)

## TL;DR
Codebase đã có khung cutscene cơ bản và editor tool tạo prefab, nhưng đang thiếu tích hợp PlayableDirector/Cinemachine trong runtime cutscene, thiếu test chuyên biệt, thiếu cơ chế branching, localization bridge, persistence trạng thái, profiler marker và fallback handler. Kế hoạch P0-P3 bên dưới ưu tiên vá khoảng trống theo thứ tự phụ thuộc rõ ràng, đồng thời tách phần việc có thể chạy song song.

## Discovery Evidence

| Area | File (full path) | Symbol | Evidence note |
|---|---|---|---|
| Core cutscene | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneDirector.cs | CutsceneDirector | Tồn tại file điều phối cutscene. |
| Core cutscene | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\SubtitleController.cs | SubtitleController | Tồn tại controller subtitle cho cutscene. |
| Core cutscene UI | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneSkipUI.cs | CutsceneSkipUI | Tồn tại UI skip. |
| Core cutscene setup | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneSetup.cs | CutsceneSetup | Tồn tại file setup cutscene. |
| Editor tool | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Editor\CutscenePrefabCreator.cs | CutscenePrefabCreator | Đã có công cụ editor tạo prefab cutscene. |
| Related system | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Music\MusicManager.cs | MusicManager | Hệ âm nhạc liên quan khi vào/ra cutscene. |
| Related system | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\SceneTransition\SceneController.cs | SceneController | Hệ chuyển cảnh liên quan lifecycle cutscene. |
| Related system | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\SceneTransition\LevelManager.cs | LevelManager | Quản lý level có thể ảnh hưởng trigger cutscene. |
| Related system | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Player\PlayerRespawn.cs | PlayerRespawn | Luồng respawn cần đồng bộ với trạng thái cutscene. |
| Package | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Packages\manifest.json | com.unity.timeline 1.8.10 | Package Timeline đã cài. |
| Package | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Packages\manifest.json | com.unity.cinemachine 3.1.6 | Package Cinemachine đã cài. |
| Package | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Packages\manifest.json | com.unity.inputsystem 1.18.0 | Input System đã cài. |
| Package | C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Packages\manifest.json | com.unity.test-framework 1.6.0 | Test Framework đã cài. |

## Not Found / Gaps

- Chưa thấy tích hợp PlayableDirector trong các script cutscene hiện tại.
- Chưa thấy sử dụng Cinemachine trong các script cutscene hiện tại.
- Chưa có test file chuyên cutscene trong Assets\Tests.
- Chưa có hệ thống branching cho cutscene.
- Chưa có localization bridge cho subtitle/cutscene text.
- Chưa có persistence trạng thái cutscene (đã xem/chưa xem/đang dở).
- Chưa có profiler markers cho đường chạy cutscene.
- Chưa có fallback/error handler chuyên biệt cho cutscene.

## Plan P0-P3

### P0 - Ổn định runtime cutscene nền tảng
- Mục tiêu: Khóa lỗi runtime cơ bản và tạo baseline kiểm thử.
- Hạng mục:
  - P0.1 Tích hợp PlayableDirector vào CutsceneDirector/CutsceneSetup.
  - P0.2 Chuẩn hóa luồng vào/ra cutscene với MusicManager, SceneController, LevelManager, PlayerRespawn.
  - P0.3 Bổ sung CutsceneErrorHandler và fallback tối thiểu (fail an toàn về gameplay).
  - P0.4 Tạo test khởi đầu cho cutscene (EditMode + PlayMode).
- Phụ thuộc:
  - P0.2 phụ thuộc P0.1.
  - P0.3 phụ thuộc P0.1.
  - P0.4 phụ thuộc P0.1 để có API ổn định.
- Có thể chạy song song:
  - P0.2 và P0.3 song song sau khi P0.1 xong.

### P1 - Camera và input cutscene chuẩn hóa
- Mục tiêu: Dùng package đã cài để giảm custom logic rời rạc.
- Hạng mục:
  - P1.1 Tích hợp Cinemachine vào luồng cutscene (camera blend/shot switching).
  - P1.2 Đồng bộ CutsceneSkipUI với Input System (giữ thống nhất keyboard/gamepad).
  - P1.3 Mở rộng editor tool hiện có để tự cấu hình component cần thiết cho prefab cutscene.
- Phụ thuộc:
  - P1.1 phụ thuộc P0.
  - P1.2 phụ thuộc P0.
  - P1.3 phụ thuộc P1.1 và P1.2 (để prefab sinh ra đúng chuẩn mới).
- Có thể chạy song song:
  - P1.1 và P1.2 song song.

### P2 - Tính năng còn thiếu cho narrative flow
- Mục tiêu: Bổ sung khả năng nhánh, ngôn ngữ và trạng thái.
- Hạng mục:
  - P2.1 Thêm branching model tối thiểu cho cutscene.
  - P2.2 Thêm localization bridge cho subtitle/text cutscene.
  - P2.3 Thêm persistence trạng thái cutscene (đã xem, skip, điểm resume).
- Phụ thuộc:
  - P2.1 phụ thuộc P0.
  - P2.2 phụ thuộc P0.
  - P2.3 phụ thuộc P2.1.
- Có thể chạy song song:
  - P2.1 và P2.2 song song.

### P3 - Quan sát hiệu năng và hardening
- Mục tiêu: Có dữ liệu profiler và cơ chế rollback an toàn.
- Hạng mục:
  - P3.1 Thêm profiler marker cho play, skip, subtitle update, transition.
  - P3.2 Mở rộng fallback cases (missing reference, timeline lỗi, skip giữa chừng).
  - P3.3 Hoàn thiện regression test matrix cho cutscene.
- Phụ thuộc:
  - P3.1 phụ thuộc P0.
  - P3.2 phụ thuộc P0 và P2.
  - P3.3 phụ thuộc P1 và P2.
- Có thể chạy song song:
  - P3.1 và P3.2 song song.

## Relevant files

### Existing files to modify
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneDirector.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneSetup.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneSkipUI.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\SubtitleController.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Editor\CutscenePrefabCreator.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Music\MusicManager.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\SceneTransition\SceneController.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\SceneTransition\LevelManager.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Player\PlayerRespawn.cs

### New files proposed
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneErrorHandler.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneBranchController.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneLocalizationBridge.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Scripts\Cutscene\CutsceneStateStore.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Tests\EditMode\Cutscene\CutsceneDirectorEditModeTests.cs
- C:\Users\thanh\Downloads\MA-CANH-DEMON-REALM-Dev\MA-CANH-DEMON-REALM-Dev\Assets\Tests\PlayMode\Cutscene\CutsceneDirectorPlayModeTests.cs

## Verification Matrix

| Phase item | Verification type | Cách xác minh cụ thể |
|---|---|---|
| P0.1 PlayableDirector integration | PlayMode | Chạy cutscene từ trigger đến kết thúc, xác nhận timeline play/stop đúng vòng đời. |
| P0.2 Runtime handoff với hệ liên quan | Manual | Kiểm tra vào/ra cutscene không làm kẹt scene transition, nhạc, respawn. |
| P0.3 Error handler + fallback cơ bản | PlayMode + Manual | Tạo thiếu reference có chủ đích và xác nhận game quay về trạng thái chơi an toàn. |
| P0.4 Baseline tests | EditMode + PlayMode | Test runner chạy pass bộ test cutscene mới. |
| P1.1 Cinemachine integration | PlayMode | Chạy shot chuyển camera, kiểm tra blend và restore camera sau cutscene. |
| P1.2 Input System cho skip | PlayMode + Manual | Test skip bằng keyboard/gamepad, UI phản hồi đúng hành vi. |
| P1.3 Editor prefab creator mở rộng | Manual | Tạo prefab cutscene mới từ editor tool và chạy thành công ở PlayMode. |
| P2.1 Branching | PlayMode + Manual | Chọn nhánh khác nhau và xác nhận luồng tiếp theo đúng. |
| P2.2 Localization bridge | EditMode + PlayMode | Đổi locale và xác nhận subtitle/text cập nhật đúng key. |
| P2.3 State persistence | PlayMode + Manual | Restart scene/game và xác nhận trạng thái xem/skip/resume còn đúng. |
| P3.1 Profiler markers | Profiler | Mở Unity Profiler, thấy marker cutscene theo từng bước quan trọng. |
| P3.2 Fallback mở rộng | PlayMode + Manual | Gây lỗi timeline hoặc skip giữa chừng và xác nhận không hard-lock. |
| P3.3 Regression matrix | EditMode + PlayMode + Manual | Chạy đầy đủ test + checklist thủ công trước khi merge. |

## Scope

### In-scope
- Củng cố hệ cutscene hiện có trong Assets\Scripts\Cutscene.
- Tích hợp PlayableDirector và Cinemachine vào runtime cutscene.
- Đồng bộ skip/input, subtitle, scene/music/player handoff.
- Bổ sung branching tối thiểu, localization bridge, state persistence.
- Bổ sung test cutscene (EditMode/PlayMode), profiler marker, fallback/error handler.

### Out-of-scope
- Viết mới toàn bộ framework cutscene thay cho hệ hiện tại.
- Mở rộng sang multiplayer synchronization.
- Pipeline cinematic nâng cao ngoài cutscene gameplay hiện hữu.
- Tối ưu đồ họa toàn project không gắn trực tiếp với luồng cutscene.

## Risks & Rollback

- Rủi ro: Tích hợp PlayableDirector/Cinemachine gây regress luồng cutscene cũ.
  - Giảm thiểu: Bật dần theo feature flag, ví dụ CUTSCENE_USE_PLAYABLE_DIRECTOR và CUTSCENE_USE_CINEMACHINE.
- Rủi ro: Branching/persistence làm lệch trạng thái quest hoặc level flow.
  - Giảm thiểu: Tách lớp state store, thêm test hồi quy cho load/restart.
- Rủi ro: Fallback mới che lỗi và khó quan sát.
  - Giảm thiểu: Log lỗi chuẩn hóa + profiler marker tại điểm failover.

Phương án rollback cụ thể:
- Tạo tag trước mỗi phase: cutscene-p0-baseline, cutscene-p1-camera-input, cutscene-p2-branch-loc-state, cutscene-p3-hardening.
- Nếu phát sinh regress nghiêm trọng, tắt feature flag của phase mới và reset về tag phase trước để phát hành hotfix nhanh.
