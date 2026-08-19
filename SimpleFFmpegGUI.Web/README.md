# SimpleFFmpegGUI.Web

远程 FFmpeg 工具箱的前端（Vue 3 + TypeScript + Element Plus + Vite）。

## 开发

```bash
npm install
npm run dev      # 开发服务器（API 直连 http://localhost:5001）
npm run build    # 生产构建，输出到 dist/
```

## 说明

- API 请求封装在 `src/api.ts`；开发环境通过 CORS 直连 `http://localhost:5001`，生产构建请求相对路径 `api/{controller}`。
- 新增任务相关视图在 `src/views/Add/`。
