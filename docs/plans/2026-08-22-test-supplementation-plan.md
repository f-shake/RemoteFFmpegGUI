# 测试补充方案：新建 UnitTests + 扩充 WebTest 集成测试

- **制定时间**：2026-08-22
- **分支**：`master_v2`
- **目标**：全面补齐单元测试与集成测试，一次交付。

## Context

`SimpleFFmpegGUI.WebTest`（唯一测试项目）仅 34 个集成测试 + 6 个参数生成器单测，覆盖不足。经盘点（3 个 Explore agent）与决策，形成本方案。单元测试与集成测试拆分到两个项目，职责更清晰：单测毫秒级、不依赖 ffmpeg/ASP.NET；集成测试仍需 `bin/ffmpeg/ffmpeg.exe`。

### 已核实事实
- `Core/Helpers/SystemTextJsonExtensions.cs` 的 `extension(string){...}` 是 **C# 14 extension blocks 特性**，非语法错误（Core.dll 在其后编译成功；`SimpleFFmpegApiTestsBase.cs:112` 正按扩展调用）。可正常单测。
- **无-token 工厂可行**：给 `SimpleFFmpegWebApplicationFactory` 加可写属性 `Token`（默认 `"Test_Token_123"`），第 131 行改 `[nameof(AppSettings.Token)] = Token`；用例 `new SimpleFFmpegWebApplicationFactory { Token = null }`。未配置 Token 时 `/Token/Need`→false、`/Token/Check`→true（`TokenController.cs:36-40`、`14-25`），两接口被 `AppActionFilter.cs:47-50` 排除。
- **UnitTests 依赖**：Core 的 FzLib 是普通 `PackageReference`（`Directory.Packages.props:7`，中央版本 `3.4.0-rc.2`），传递可用；但 `FilePathHelper` 抛 `FzLib.Web.HttpStatusCodeException`，测试要断言其类型/状态码需**显式加 FzLib 包**。
- **DoubleConverter.Write 真 bug**：`Core/Converters/DoubleConverter.cs:21-32`，`IsNaN` 写 `WriteNullValue()` 后缺终止，继续写 `WriteNumberValue(NaN)` → 双 token（NaN 分支还会抛）。

## 结构变更

1. **新建 `SimpleFFmpegGUI.UnitTests` 项目**：`net10.0`、`IsPackable=false`、`ImplicitUsings=enable`、`<Using Include="Xunit"/>`。
   - `ProjectReference → SimpleFFmpegGUI.Core`；`PackageReference`：`FluentAssertions`、`Microsoft.NET.Test.Sdk`、`xunit`、`xunit.runner.visualstudio`、**`FzLib`**（用于断言 `HttpStatusCodeException`）。
   - 加入 `SimpleFFmpegGUI.sln`。
   - **把 `SimpleFFmpegGUI.WebTest/ArgumentsGeneratorTests.cs` 迁入**并扩充。
2. **`SimpleFFmpegGUI.WebTest`**：只扩充集成测试；factory 加 `Token` 属性（2 行）。

## 单元测试（SimpleFFmpegGUI.UnitTests，全纯逻辑）

沿用 FluentAssertions，注释中文。

### A. FFmpeg 参数生成器
- `ArgumentsGeneratorTests`（迁入扩充）：`GetArguments` output==null 回退 RealOutput、多输入；`GetInputArguments` Extra 拼接；`GetOutputArguments` 的 video/audio disable、codec、average/max bitrate、aspect、pix_fmt、fps、scale、speed、pass、stream maps、Mux.Shortest；`CheckOutputArguments` 两抛异常分支（双 Disable、TwoPass+空白 Format）。
- `ArgumentsGeneratorBaseTests`：Parent/Separator/Other 扁平化、null 过滤、GroupBy(Parent) 与无 parent 的 `-k v`。
- `VideoArgumentsGeneratorTests`：`Aspect` 合法/非法抛、`BufferRatio` 未设 maxBitrate 抛 + SVTAV1 跳过、`Codec` 自动/auto/未知落 GeneralVideoCodec、CRF/Speed<=0 忽略、Speed>Max 抛、MaxBitrate/FrameRate NaN 忽略、Pass(0/null)、Scale `vf` 逗号、Disable、ExtraArguments 空 codec。
- `AudioArgumentsGeneratorTests`：Bitrate、SamplingRate、Codec 未知/自动。
- `InputArgumentsGeneratorTests`：Duration/Seek/To 的 `0.000` 秒、Format、Framerate NaN、Input(null)。
- `StreamArgumentsGeneratorTests`：Map v/a/s 通道 + index 追加 + default 抛 NotImplementedException。

### B. Codec
- `VideoCodecTests`：GetCodec(null/unknown)、Average/MaxBitrate/BufferSize/FrameRate<0 抛、PixelFormat 空白抛、Pass(4) 抛、Speed>Max 抛。
- `AudioCodecTests`：Bitrate<0 抛、SamplingRate<9600 抛。
- `Software/HardwareVideoCodecTests`：CRF 范围抛、CQ label。
- 具体 codec：X264、X265（Pass→parent `x265-params` `:`）、XVP9（cpu-used+row-mt）、SVTAV1（svtav1-params Other 链、BufferSize/Pass 抛）、GeneralVideoCodec（Speed 范围）、GeneralAudioCodec、AAC、OPUS（Name/Lib）。
- `FFmpegEnumsTests`：Presets/PixelFormats 数据。`VideoFormatTests`：ctor null 抛、AudioOnly/ImageOnly、Formats 数量。

### C. Helpers
- `FilePathHelperTests`：ctor InputDir/OutputDir 为 null 抛 500；GetFullPath 绝对路径 + allowAbsolute=false 抛 400、true 归一化、相对拼接、`..\` 逃逸抛 400、RootDirType 选根。用 `Options.Create(appSettings)` 注入 `IOptionsSnapshot`。
- `FileSystemHelperTests`：GetSequence 临时目录验证 `%d`/`%0Nd`/非序列 null；GenerateOutputPath 空输出回退输入、空输出+无输入抛、非法字符清理、按 Format 改扩展名、唯一命名。
- `SystemTextJsonExtensionsTests`：Default/Web/Friendly 往返、WebOptions 含 TimeSpan/Double 转换器、string 扩展 `DeserializeWithWebSettings`。

### D. Converters
- `DoubleConverterTests`：Read "NaN"→NaN/number；Write NaN→仅一 null、Infinity→null、普通数（**先暴露 bug，随后修复**）。
- `TimeSpanConverterTests`：Read number>0、string TryParse、0/负→Zero、非法→Zero；Write TotalSeconds。
- `MediaInfoJsonTests`：MediaInfoGeneral/Video/Audio/Text/Image 的映射 + 计算 Duration（AllowReadingFromString 解析字符串数字）。

### E. Compatibility / Services
- `PresetConverterTests`：ConvertJson 含 Arguments→null、null/空→null；ConvertFromV1_1；ConvertVideo/Audio 的 transcode/disable/copy + `MaxBitrateBuffer ?? 2.0`；ConvertToNew type 3→99。
- `PowerServiceTests`：仅测公共入口，`GetCpuUsageAsync` 两参都默认抛 ArgumentException（反射测私有 `CalculateCpuUsage` 不纳入，避免脆耦合）。

## 集成测试（WebTest 扩充，复用 SimpleFFmpegApiTestsBase + 内存 SQLite factory）

- `ConfigApiTests` +：SnapshotSize 空值/非法格式（`abc`/`1920`/多冒号）→400。
- `FileApiTests` +：Download 不存在→404、Upload null/0字节→400、Dirs 内容断言、List 目录不存在→空、Download Content-Type。
- `MediaInfoApiTests` +：MediaInfo 空名 400/不存在 404；Snapshot 空 videoPath 400 / seconds 负/NaN/+∞ 400 / 不存在 404 / image/jpeg。
- `PresetApiTests` +：null body/Parameters/Name 400、Update/Delete 不存在 id、Import null/非法 JSON、GET 按其他 TaskType 过滤。
- `TaskAndQueueApiTests` +：GET /Task/{id} 不存在 404、POST /Task/{type} null body/非法 type、Cancel/Delete/Reset 不存在 id 404、Batch null ids 400、PreviewArguments null 400、终态任务再 Cancel、Queue/Schedule 过去时间 400/null body 400、Queue/Start 重复、Queue/Pause|Resume 有任务成功、Queue/Cancel 未运行。
- `LogApiTests` +：无参默认分页、空日志 TotalCount=0、非法分页参数 400、按等级/类型过滤。
- `TokenApiTests` +：受保护端点错误 token（`Bearer wrong_token`）→401；用**无-token 工厂**测 `/Token/Need`→false、`/Token/Check`→true。
- `HealthApiTests`（新）：`GET /health`→200、`GET /`→200。

## 修复（与测试一同一次性交付）

- `DoubleConverter.Write`：`if (double.IsNaN(value) || double.IsInfinity(value)) { writer.WriteNullValue(); } else { writer.WriteNumberValue(value); }`。
- ① `FFmpegArgumentItem.Seprator` → `Separator`（`FFmpegArgumentItem.cs:40` + `ArgumentsGeneratorBase.cs:58`）。
- ② `N_*` codec `Name` 的 `"(Nvdia)"` → `"Nvidia"`（`N_H264.cs:6`、`N_H265.cs:6`、`N_AV1.cs:9`）。
- ④ `ArgumentsGenerator.cs:166-169`：仅当 `Format` 非空才写 `-f`，避免裸 `-f `。

### 保持现状（不改，仅用测试锁住）
- ③ `Video/AudioArgumentsGenerator.Codec` 的 null `.ToLower()` NRE（DTO 默认空串兜底，不补防御）。
- ⑤ `VideoArgumentsGenerator.MaxBitrate` 不跳过负数（视为有意非法输入防护）。
- 其余无行为影响的命名/拼写项不动。

## 关键文件
- 新增：`SimpleFFmpegGUI.UnitTests/SimpleFFmpegGUI.UnitTests.csproj`、`Directory.Packages.props` 补 `FzLib`、`SimpleFFmpegGUI.sln` 增项、`SimpleFFmpegGUI.UnitTests/**`（A–E 各测试类）。
- 迁移：`SimpleFFmpegGUI.WebTest/ArgumentsGeneratorTests.cs` → `SimpleFFmpegGUI.UnitTests/`。
- 扩充：`SimpleFFmpegGUI.WebTest/*Tests.cs`（Config/File/MediaInfo/Preset/TaskAndQueue/Log/Token + 新 `HealthApiTests`）。
- 修复：`Core/Converters/DoubleConverter.cs`、`Core/FFmpegArgument/FFmpegArgumentItem.cs`、`ArgumentsGeneratorBase.cs`、`ArgumentsGenerator.cs`、`Core/FFmpegLib/N_H264.cs`、`N_H265.cs`、`N_AV1.cs`。

## 验证
1. `dotnet build SimpleFFmpegGUI.sln -c Debug` → 0 错误。
2. `dotnet test SimpleFFmpegGUI.UnitTests -c Debug` → 单测全绿（不依赖 ffmpeg）。
3. `dotnet test SimpleFFmpegGUI.WebTest -c Debug` → 集成测试全绿（需仓库根 `bin/ffmpeg/ffmpeg.exe`）。
4. Debug 与 Release 各跑一遍。
5. DoubleConverter 单测：修复前观察到失败，修复后通过，验证 bug 修复。
