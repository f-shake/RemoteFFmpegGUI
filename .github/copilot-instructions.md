# Copilot Instructions

## 项目指南
- 本次 WPF 改造要求：所有原有页面均迁移为独立窗口；新建任务、媒体信息、所有任务、预设、日志、FFmpeg 输出、性能测试使用非模态窗口，设置 Owner 且同一功能同时只能打开一个；设置页使用模态窗口；主窗口命令栏保留在左侧任务列表下方；右侧保留现有多任务状态显示；所有 Pages 命名全部改为 Views。
- 用户确认 WPF 改造细节：Pages 目录、命名空间和页面类型统一改为 Views/*View，但对应 ViewModel 暂时保留原命名；TestWindow 也纳入 Views 和单实例非模态窗口管理；设置页沿用 RequestToClose 机制由宿主 Close；子窗口不持久化尺寸位置，每次按默认尺寸居中；由 MainWindow 增加子窗口管理类统一管理窗口实例。
- 子窗口首次弹出时保证尺寸不大于主窗口，不需要随主窗口缩放动态限制或同步收缩。后续应采用首次显示时的 Width/Height 截断，不设置持续性的 MaxWidth/MaxHeight 和 SizeChanged 监听。