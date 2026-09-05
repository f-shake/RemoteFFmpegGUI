# 清理编译产物（v2：所有项目（含 Inkore.Extension）输出统一重定向到 Generation/bin、Generation/obj；Release 为历史遗留目录，一并清理）
Remove-Item -r -Force */bin -ErrorAction SilentlyContinue
Remove-Item -r -Force */obj -ErrorAction SilentlyContinue
Remove-Item -r -Force Generation/bin -ErrorAction SilentlyContinue
Remove-Item -r -Force Generation/obj -ErrorAction SilentlyContinue
Remove-Item -r -Force Release -ErrorAction SilentlyContinue
