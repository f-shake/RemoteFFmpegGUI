# WPF 主界面布局与独立窗口改造计划

## 目标

将 WPF 主窗口调整为固定的双栏工作区：

- 左侧只显示任务列表。
- 右侧只显示当前转码任务状态区域，并保留现有多任务状态显示能力。
- 新建任务、媒体信息、设置，以及原先通过 Tab 或顶层页面显示的其他功能，改为独立 Window。
- 所有原 `Pages` 目录下的页面统一迁移并重命名为 `Views`。
- 保留现有任务队列、拖放、状态刷新和页面业务逻辑，尽量只调整宿主方式与布局。

## 当前实现盘点

- `MainWindow.xaml` 当前包含动态 `TabControl`、`topTab`、命令栏，以及根据 Tab 状态在左右区域移动 `TaskList` 和 `StatusPanel` 的布局切换逻辑。
- `MainWindow.xaml.cs` 通过 `AddNewTab`、`ShowTopTabAsync`、`ResetUI` 和 `Tab_SelectionChanged` 管理页面及压缩布局。
- `MainWindowViewModel` 通过 `AddNewTabMessage` 触发页面打开；设置页使用 `Top` 标记，测试窗口使用 `ShowWindow` 标记。
- `AddTaskPage`、`MediaInfoPage`、`TasksPage`、`PresetsPage`、`SettingPage`、`LogsPage` 和 `FFmpegOutputPage` 当前主要是 `UserControl` 页面；`TestWindow` 已经是独立窗口示例。迁移后这些类型和文件统一改为 `Views` 命名，`TestWindow` 也纳入统一窗口管理。
- `App.xaml.cs` 已通过依赖注入注册页面及对应 ViewModel，可在此基础上增加通用 Window 宿主注册。
- 文件拖放逻辑直接调用 `AddNewTab<AddTaskPage>()` 或 `AddNewTab<MediaInfoPage>()`，迁移时必须保留 `SetFiles` 和 `SetFile` 的初始化能力。

## 实施方案

### 1. 主窗口固定双栏布局

修改 `SimpleFFmpegGUI.WPF/MainWindow.xaml`：

- 删除动态 `TabControl`、`topTab` 以及与页面切换相关的布局承载控件。
- 左栏直接承载 `TaskList`，右栏直接承载 `StatusPanel`。
- 移除根据 Tab 选择状态改变左右布局的 `GridSplitter`、行高和压缩模式依赖；如仍需调整宽度，只保留左右栏之间的垂直分隔条。
- 将命令入口保留在左侧任务列表下方，但不再把页面内容显示在主窗口内部。
- 确保窗口缩放时任务列表和状态区域均可正常滚动，右侧状态面板继续显示所有正在处理的任务。

### 2. 统一页面 Window 宿主

新增一个可复用的页面 Window 宿主，例如 `ViewWindow` 或泛型/配置化的 `ContentWindow`：

- 宿主负责设置标题、Owner、启动位置、最小尺寸和关闭行为。
- 宿主内部承载现有 `UserControl` 页面，尽量不复制页面 XAML 和业务 ViewModel。
- 对 `ICloseablePage` 的 `RequestToClose` 事件进行适配，使页面请求关闭时调用 Window.Close。
- 对需要初始化参数的页面提供明确的初始化接口或回调，而不是依赖 Tab 内容查找。
- 保证窗口关闭后解除事件订阅，避免重复打开造成引用或状态泄漏。
- 新建任务、媒体信息、所有任务、预设、日志、FFmpeg 输出和性能测试使用非模态 `Show()`，设置 `Owner = MainWindow`，且同一功能同时只能存在一个窗口；再次触发时激活已有窗口。
- 设置使用带 Owner 的模态 `ShowDialog()`，关闭后再返回主窗口操作。

### 3. 页面入口迁移

调整 `MainWindowViewModel`、`MainWindow.xaml.cs` 和消息协议：

- 将 `ShowAddTaskCommand` 改为打开新建任务 Window。
- 将 `ShowMediaInfoCommand` 改为打开媒体信息 Window。
- 将 `ShowSettingsCommand` 从全屏顶层 Tab 改为带 Owner 的模态设置 Window。
- 将所有任务、预设、日志、FFmpeg 输出和性能测试等页面入口统一改为独立 Window，避免保留部分 Tab 语义。
- 将原 `Pages` 目录、命名空间、页面类型名和相关引用统一迁移为 `Views`；页面类型不保留 `Page` 后缀，相关 ViewModel 暂时保持现有命名。
- 将 `AddNewTabMessage` 重命名或替换为通用的窗口打开消息，删除 `Top`、Tab 关闭等不再需要的字段。
- 所有独立窗口均设置 `Owner = MainWindow`、居中显示和必要的窗口激活行为；由主窗口持有的子窗口管理类维护非模态窗口单实例引用。

### 4. 拖放和页面初始化迁移

修改 `MainWindow.xaml.cs` 的拖放处理：

- 拖放任务文件后打开新建任务 Window，并调用 `SetFiles` 初始化输入文件和任务类型。
- 拖放媒体文件查询信息后打开媒体信息 Window，并调用 `SetFile` 初始化文件路径。
- 确保文件对话框消息仍然可以将主窗口作为 Owner 或句柄来源。
- 验证窗口打开后不会改变主窗口左侧任务列表和右侧状态面板的布局。

### 5. DI 和页面适配

修改 `App.xaml.cs` 及必要的页面代码：

- 注册通用 Window 宿主及所需 View 实例/工厂。
- 增加主窗口子窗口管理类，统一负责创建、缓存、激活、关闭清理和 Owner 设置；设置窗口使用独立的模态打开流程。
- 检查页面构造函数、ViewModel 生命周期和共享 ViewModel（尤其任务列表、当前任务、FFmpeg 输出）是否适合独立窗口。
- 将设置页现有 `ICloseablePage` 关闭机制适配到 Window.Close。
- 如现有页面 XAML 对 Tab 容器、`Margin` 或高度存在隐式依赖，迁移到 Window 后进行局部调整。
- 清理废弃的 `TabControlVisibility`、`TopTabVisibility`、`SetTabVisiable`、`AddNewTab`、`ShowTopTabAsync` 和 `ResetUI` 代码；仅在仍有实际调用时保留兼容逻辑。

## 验证清单

### 编译与静态检查

- 构建 `SimpleFFmpegGUI.WPF` 项目。
- 确认没有 XAML 名称、DI 注册、消息类型或事件处理器的编译错误。
- 检查是否存在未使用且应删除的 Tab 相关成员。

### 主窗口布局

- 启动后左侧只显示任务列表，右侧只显示当前转码任务状态区域，并保留多任务卡片显示。
- 调整窗口大小时两栏均能正常伸缩，任务列表和状态内容不会互相覆盖。
- 选中任务后，右侧状态区域显示对应的转码状态；任务开始、暂停、取消和完成时状态可刷新。

### 独立窗口

- 新建任务、媒体信息、设置、所有任务、预设、日志、FFmpeg 输出和性能测试均可从主窗口打开独立 Window。
- 除设置外的窗口均为非模态、设置为模态；所有窗口均拥有正确的 Owner、标题、启动位置和关闭行为。
- 重复点击非模态入口不会创建第二个同功能窗口，而是激活已有窗口。
- 设置页的保存及关闭流程正常。
- 关闭后重新打开窗口不会导致初始化参数错乱或事件重复触发。
- 所有原 `Pages` 路径、命名空间、类型名和引用均已迁移到 `Views`。

### 拖放

- 拖放文件创建任务时，新建任务窗口能正确接收文件列表和任务类型。
- 拖放媒体文件查询信息时，媒体信息窗口能正确加载文件。
- 拖放操作不会重新引入 Tab 或改变主窗口双栏布局。

## 风险与待决策项

- 现有页面均为 `UserControl`，计划采用通用 `ViewWindow` 宿主以减少重复代码；页面内容、命名空间和类型名统一迁移到 `Views`，ViewModel 暂时不改名。
- 若页面通过 Messenger 间接依赖主窗口，需要逐一确认其 Owner 和文件对话框行为。
- 非模态窗口采用主窗口子窗口管理类按功能单实例复用；设置采用带 Owner 的模态调用，不纳入非模态窗口缓存。
- 子窗口不持久化尺寸和位置；每次创建时使用功能对应的默认尺寸并居中于主窗口。
- 本计划只描述布局和宿主迁移，不改变任务模型、队列执行逻辑或媒体处理逻辑。
