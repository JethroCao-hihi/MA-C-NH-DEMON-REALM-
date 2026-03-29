# Hệ thống Cutscene Mở Đầu - MA CẢNH DEMON REALM

## Tổng quan
Hệ thống cutscene hoàn chỉnh với:
- ✅ Camera movement (pan + smooth transitions)
- ✅ Subtitle system với typewriter effect
- ✅ Skip bằng ESC hoặc SPACE (tap hoặc giữ tuỳ cấu hình)
- ✅ Lock player input trong cutscene
- ✅ Fade in/out transitions
- ✅ Progress bar hiển thị tiến độ
- ✅ Debug logs rõ ràng

## Files Đã Tạo

### Scripts (Assets/Scripts/Cutscene/)
1. **CutsceneDirector.cs** - Điều khiển chính cutscene
2. **SubtitleController.cs** - Hiển thị subtitle với animation
3. **CutsceneSkipUI.cs** - UI skip hint và progress bar
4. **CutsceneSetup.cs** - Auto-setup UI tại runtime

### Editor Scripts (Assets/Editor/)
1. **CutscenePrefabCreator.cs** - Menu tools để tạo cutscene system

---

## HƯỚNG DẪN SETUP TRONG UNITY EDITOR

### Cách 1: Tự Động (Khuyến nghị)

1. **Mở scene Game1.unity**
   - Project > Assets > Scenes > Game1

2. **Tạo Cutscene System qua menu**
   - Menu bar: **Tools > Cutscene > Create Cutscene System in Scene**
   - Một popup sẽ xác nhận việc tạo thành công

3. **Thêm subtitles mặc định (tuỳ chọn)**
   - Menu bar: **Tools > Cutscene > Add Default Subtitles to Director**

4. **Save scene**
   - Ctrl+S

### Cách 2: Thủ Công

#### Bước 1: Tạo CutsceneDirector
1. Hierarchy > Click chuột phải > Create Empty
2. Đặt tên: `CutsceneDirector`
3. Add Component > CutsceneDirector

#### Bước 2: Tạo Canvas cho Cutscene UI
1. Hierarchy > Click chuột phải > UI > Canvas
2. Đặt tên: `CutsceneCanvas`
3. Cấu hình Canvas:
   - Render Mode: Screen Space - Overlay
   - Sort Order: 100 (để hiện trên cùng)

#### Bước 3: Tạo FadePanel
1. Click chuột phải vào CutsceneCanvas > UI > Image
2. Đặt tên: `FadePanel`
3. Cấu hình RectTransform:
   - Anchor Preset: Stretch (Alt+click góc dưới phải)
   - Left, Top, Right, Bottom = 0
4. Image Color: Black (0, 0, 0, 255)
5. Add Component > Canvas Group

#### Bước 4: Tạo SubtitleContainer
1. Click chuột phải vào CutsceneCanvas > Create Empty
2. Đặt tên: `SubtitleContainer`
3. Add Component > Canvas Group
4. RectTransform:
   - Anchor Min: (0.1, 0.05)
   - Anchor Max: (0.9, 0.18)

5. Tạo child Background:
   - Click chuột phải vào SubtitleContainer > UI > Image
   - Đặt tên: `SubtitleBackground`
   - Color: Black với alpha 0.7 (0, 0, 0, 178)
   - Stretch full parent

6. Tạo child Text:
   - Click chuột phải vào SubtitleContainer > UI > Text - TextMeshPro
   - Đặt tên: `SubtitleText`
   - Font Size: 36
   - Alignment: Center
   - Color: White
   - Stretch full parent với padding -20

7. Add Component vào SubtitleContainer: `SubtitleController`

#### Bước 5: Tạo SkipHintContainer
1. Click chuột phải vào CutsceneCanvas > Create Empty
2. Đặt tên: `SkipHintContainer`
3. RectTransform:
   - Anchor: Top Center
   - Pos Y: -50
   - Width: 500, Height: 50

4. Tạo child Text:
   - UI > Text - TextMeshPro
   - Đặt tên: `SkipHintText`
   - Text: "Nhấn SPACE hoặc ESC để bỏ qua"
   - Font Size: 24
   - Alignment: Center
   - Color: White với alpha 0.7

#### Bước 6: Tạo ProgressBar
1. Click chuột phải vào CutsceneCanvas > UI > Slider
2. Đặt tên: `ProgressBar`
3. RectTransform:
   - Anchor: Bottom Center
   - Width: 50% of screen
   - Height: 15px
4. Slider settings:
   - Interactable: OFF
   - Min Value: 0, Max Value: 1

#### Bước 7: Gắn CutsceneSkipUI
1. Chọn CutsceneCanvas (hoặc tạo empty child)
2. Add Component > CutsceneSkipUI
3. Kéo thả references:
   - Skip Hint Container → SkipHintContainer
   - Skip Hint Text → SkipHintText
   - Progress Bar Container → ProgressBar
   - Progress Slider → ProgressBar

#### Bước 8: Cấu hình CutsceneDirector
1. Chọn CutsceneDirector
2. Kéo thả references:
   - Cutscene UI → SubtitleContainer's parent (hoặc CutsceneCanvas)
   - Fade Panel → FadePanel's CanvasGroup
   - Camera Transform → Main Camera

3. Cấu hình Subtitles (trong Inspector):
   ```
   Element 0:
     Text: "Trong vương quốc bị lãng quên..."
     Trigger Time: 2
     Display Duration: 4
   
   Element 1:
     Text: "Bóng tối đã thức tỉnh từ cõi Ma Cảnh..."
     Trigger Time: 7
     Display Duration: 4
   
   Element 2:
     Text: "Chỉ có một người có thể ngăn chặn thảm họa..."
     Trigger Time: 12
     Display Duration: 4
   
   Element 3:
     Text: "Hành trình bắt đầu..."
     Trigger Time: 18
     Display Duration: 4
   ```

---

## CÁCH TEST

### Quick Test
1. Mở scene Game1
2. Nhấn Play (Ctrl+P)
3. Quan sát:
   - Màn hình fade từ đen
   - Camera pan
   - Subtitles xuất hiện tuần tự
   - Sau 2s: hint "Nhấn SPACE hoặc ESC để bỏ qua" xuất hiện
   - Progress bar chạy từ 0% đến 100%

4. Test Skip:
   - Nhấn SPACE hoặc ESC bất kỳ lúc nào
   - Cutscene sẽ kết thúc ngay lập tức
   - Player controls được mở lại

5. Kiểm tra Console:
   ```
   [CutsceneDirector] === BẮT ĐẦU CUTSCENE ===
   [CutsceneDirector] Found player: Player
   [CutsceneDirector] PlayerMovement disabled
   [CutsceneDirector] PlayerAttack disabled
   [CutsceneDirector] Phase 1: Fade In
   [CutsceneDirector] Phase 2: Camera + Subtitles
   [SubtitleController] Showing: "..." for Xs
   ...
   [CutsceneDirector] === CUTSCENE KẾT THÚC ===
   [CutsceneDirector] PlayerMovement enabled
   [CutsceneDirector] PlayerAttack enabled
   ```

### Test Skip
1. Play scene
2. Nhấn SPACE ngay khi cutscene bắt đầu
3. Console sẽ hiện:
   ```
   [CutsceneDirector] === SKIP CUTSCENE ===
   [CutsceneDirector] === CUTSCENE KẾT THÚC ===
   ```

---

## CẤU HÌNH NÂNG CAO

### Camera Waypoints (tuỳ chọn)
Thêm custom camera positions trong Inspector:
```
Camera Waypoints:
  Element 0:
    Position: (5, 2, 0)
    Duration: 8
    Hold Time: 2
    Use Smooth Step: ✓
  
  Element 1:
    Position: (10, 0, 0)
    Duration: 6
    Hold Time: 1
    Use Smooth Step: ✓
```

### Cutscene Music (tuỳ chọn)
1. Tạo AudioSource trên CutsceneDirector
2. Kéo AudioClip vào "Cutscene Music"
3. Hoặc để trống - sẽ dùng MusicManager có sẵn

### Timing Adjustments
- `cutsceneDuration`: Tổng thời gian (mặc định 25s)
- `fadeInDuration`: Thời gian fade từ đen (mặc định 1.5s)
- `fadeOutDuration`: Thời gian fade ra đen (mặc định 1s)
- `musicFadeOutDuration`: Thời gian fade music (mặc định 1s)

### Skip Settings (CutsceneDirector)
- `useHoldToSkip`: Bật/tắt cơ chế giữ phím để skip.
- `holdToSkipDuration`: Thời gian cần giữ SPACE/ESC để bỏ qua (mặc định 1.2s).
- Nếu tắt `useHoldToSkip`, hệ thống dùng hành vi cũ: nhấn 1 lần SPACE/ESC để skip.

### Watched State Persistence
- `cutsceneId`: ID định danh cutscene để lưu trạng thái đã xem.
- `autoSkipIfWatched`: Nếu bật, cutscene đã xem sẽ tự động bỏ qua ở lần vào sau.
- Trạng thái được lưu bằng `PlayerPrefs` thông qua `CutsceneStateStore`.
- `skipCutsceneIfCheckpointExists`: Nếu bật, intro cutscene sẽ tự skip khi scene hiện tại đã có checkpoint được lưu (hữu ích khi respawn/đi từ checkpoint).

### Boss Intro/Outro Hooks
- `BossController`:
  - `bossIntroCutsceneDirector`: Director dùng cho boss intro (ưu tiên field này, nếu trống sẽ tự tìm trong scene).
  - `playBossIntroCutscene`: Bật/tắt phát boss intro cutscene khi boss vào trận.
  - `lockBossAiDuringIntro`: Khóa AI boss trong lúc intro đang chạy.
- `BossHealth`:
  - `bossOutroCutsceneDirector`: Director dùng cho boss outro.
  - `playBossOutroCutsceneOnDeath`: Bật/tắt phát outro cutscene khi boss chết trước khi `Destroy`.
- Cả intro/outro đều có fallback subtitle mặc định theo dark fantasy narrative qua `ApplySubtitlesIfEmpty(...)` trong `CutsceneDirector`.
- Boss intro/outro dùng runtime override ID (`boss_intro`, `boss_outro`) và bỏ qua quy tắc skip theo checkpoint/đã xem.

### Boss Intro/Outro/After-Credits
- Hệ boss giờ luôn tự bảo đảm có cutscene system:
  - Nếu thiếu `CutsceneDirector`, hệ thống tự tạo GameObject runtime: `===== BOSS CUTSCENE SYSTEM =====`
  - Tự add `CutsceneDirector` + `CutsceneSetup`
  - `playOnStart` được tắt runtime để không tự phát ngoài ý muốn.
- `BossController` (Inspector):
  - `bossIntroCutsceneDirector`
  - `playBossIntroCutscene`
  - `lockBossAiDuringIntro`
- `BossHealth` (Inspector):
  - `bossOutroCutsceneDirector`
  - `playBossOutroCutsceneOnDeath`
  - `playAfterCreditsOnBossDeath`
  - `teamTitle`
  - `teamCreditsLines` (**editable** để thay đổi line credit team)
  - `bossOutroTimeout`
  - `afterCreditsTimeout`
- Flow khi boss chết:
  1. Chạy `boss_outro` narrative.
  2. Sau đó (nếu bật) chạy `after_credits` bằng các line trong `teamCreditsLines`.
  3. Kết thúc mới `Destroy` boss object.

### Timeline Mode (tuỳ chọn)
- `useTimelineIfAvailable`: Nếu bật, CutsceneDirector sẽ ưu tiên phát bằng `PlayableDirector` khi có sẵn.
- `playableDirector`: Có thể gán trực tiếp trong Inspector; nếu để trống hệ thống sẽ tự tìm trên cùng GameObject rồi trong scene.
- Nếu không có `TimelineAsset` trên `PlayableDirector`, hệ thống tự fallback về coroutine camera/subtitle hiện tại (an toàn, không crash).

### Setup Timeline nhanh bằng Tool mới
1. Chọn GameObject có `CutsceneDirector`.
2. Mở menu: **Tools > Cutscene > Create Timeline Setup On Selected Director**.
3. Tool sẽ tự:
   - Thêm `PlayableDirector` (nếu thiếu).
   - Tạo Timeline asset trong `Assets/Cutscenes/Timelines/`.
   - Thêm `CutsceneSignalReceiver` + `SignalReceiver`.
   - Tạo Signal track + 4 signal marker mẫu (ShowSubtitle, HideSubtitle, Skip, End).
   - Bind Signal track vào `SignalReceiver`.
4. Mở Timeline và chỉnh lại thời điểm marker theo cutscene thực tế.

### Timeline Signal với CutsceneSignalReceiver
- Component mới: `CutsceneSignalReceiver` (gắn cùng object với `CutsceneDirector`).
- Các method để gọi từ Signal:
  - `SignalShowSubtitleDefault()`
  - `SignalShowSubtitleById(string id)` (gọi qua helper nếu muốn trigger từ code/tooling)
  - `SignalShowSubtitleByIndex(int index)` (gọi qua helper nếu muốn trigger từ code/tooling)
  - `SignalHideSubtitle()`
  - `SignalSkipCutscene()`
  - `SignalEndCutscene()`
  - `SignalPlaySfx(string key)` (tuỳ chọn)

### Mapping subtitle cue theo id/index
Trong `CutsceneSignalReceiver`:
1. Khai báo `Subtitle Cues` (id, text, duration).
2. Chọn `defaultSubtitleCueId` nếu muốn marker mặc định gọi subtitle theo id.
3. Signal mặc định `SIG_ShowSubtitle_Default` sẽ gọi `SignalShowSubtitleDefault()`
   - Có `defaultSubtitleCueId` -> phát cue theo id.
   - Không có -> fallback cue index 0.
   - Nếu chưa cấu hình cue nào, hệ thống tự hiển thị fallback subtitle mặc định.
4. Với flow nâng cao, có thể tạo thêm signal asset riêng và bind sang method khác để điều khiển cue cụ thể.

### Runtime Timeline behavior
- Progress UI lấy từ `PlayableDirector.time / PlayableDirector.duration` (clamp 0..1).
- Hệ thống theo dõi cả `PlayState` lẫn thời gian timeline để nhận biết complete ổn định hơn.
- Skip bằng giữ SPACE/ESC hoặc nhấn 1 lần (tuỳ `useHoldToSkip`) vẫn hoạt động khi Timeline đang chạy.
- Khi end/skip, `PlayableDirector.Stop()` được gọi an toàn, player lock/unlock + neutralize animation vẫn giữ nguyên logic.
- Nếu Timeline thiếu motion track hoặc signal marker, hệ thống sẽ tự chạy fallback camera/subtitle coroutine để cutscene vẫn hiển thị đầy đủ.

### Player Animation Neutralization
- Khi lock/unlock input trong cutscene, hệ thống sẽ neutralize Animator parameter phổ biến (Speed/yVelocity/isGrounded/isWallSliding/isBlocking, reset Jump/Dash trigger nếu tồn tại).
- Mục tiêu là tránh hiện tượng player bị kẹt visual ở trạng thái jump/air trong lúc cutscene.

---

## TROUBLESHOOTING

### Cutscene không chạy
- Kiểm tra `Play On Start` đã tick trong CutsceneDirector
- Kiểm tra Console có lỗi compile không

### Player vẫn di chuyển được
- Đảm bảo Player có tag "Player" hoặc tên "Player"
- Kiểm tra Console có log "Found player: ..."

### Subtitles không hiện
- Kiểm tra SubtitleController có được add vào scene
- Kiểm tra reference subtitleText đã được gán

### Fade không hoạt động
- Kiểm tra FadePanel có CanvasGroup component
- Kiểm tra reference fadePanel đã được gán trong CutsceneDirector

---

## LƯU Ý QUAN TRỌNG

1. **Không sửa code Player cũ** - Cutscene chỉ disable/enable components
2. **Safe fallbacks** - Nếu thiếu UI, cutscene vẫn chạy với debug logs
3. **Reusable** - Có thể copy system sang scene khác
4. **Extensible** - Dễ thêm camera waypoints và subtitles mới
