export class TaskType {
  public static Types = [
    new TaskType(0, 'Transcode', 'Transcode', '转码'),
    new TaskType(1, 'Mux', 'Mux', '合并音视频'),
    new TaskType(2, 'QualityCheck', 'QualityCheck', '视频对比'),
    new TaskType(4, 'Concat', 'Concat', '拼接'),
    new TaskType(99, 'Custom', 'Custom', '自定义'),
  ]
  /** 仅含前端有路由的类型，用于导航栏 */
  public static NavTypes = TaskType.Types
  public static GetByID(id: number): TaskType {
    const types = this.Types.filter(p => p.Id === id)
    if (types.length === 0) {
      throw new Error('未知类型：' + id)
    }
    return types[0]
  }
  constructor(
    public Id: number,
    public Route: string,
    public Name: string,
    public Description: string
  ) {}
}

export function getTaskTypeDescription(type: number): string {
  return TaskType.GetByID(type).Description
}
