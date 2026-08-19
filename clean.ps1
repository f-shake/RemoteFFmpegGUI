# 清理编译产物（v2：输出重定向到 Generation/bin、Generation/obj；Inkore.Extension 的 Release 输出在 Release/AnyCPU）
Remove-Item -r -Force */bin -ErrorAction SilentlyContinue
Remove-Item -r -Force */obj -ErrorAction SilentlyContinue
Remove-Item -r -Force Generation/bin -ErrorAction SilentlyContinue
Remove-Item -r -Force Generation/obj -ErrorAction SilentlyContinue
Remove-Item -r -Force Release -ErrorAction SilentlyContinue
