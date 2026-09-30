# Honor, Not Luxury 发布准备

当前状态：**公开 GitHub 仓库已创建，尚未上传 Steam 创意工坊。** 仓库：https://github.com/lazy-red-panda-qw/Honor-Not-Luxury 。对外署名：叮咚 / DingDong；项目原创源码和文档使用 MIT 许可证。需求、功能取舍和适用范围由叮咚提出并决定；Mod 代码由 AI 编写，叮咚审阅并实机测试。

## 已准备

- `HonorNotLuxury/README.md` 与 `README.en.md`：用途、七项范围、依赖、实测范围、中途加入和移除、已知边界、维护与反馈方式。
- 根目录与 `HonorNotLuxury/` 都有 MIT 许可证，方便 GitHub 识别并随模组安装；`About/About.xml` 已改为公开署名并添加仓库链接。
- 根目录 `.github/ISSUE_TEMPLATE/compatibility.md`：兼容问题模板，可被 GitHub Issues 识别。
- `WORKSHOP_DESCRIPTION.md`：中英文工坊介绍草稿。
- `package.ps1`：只收录指定运行文件、原创源码和文档，校验包内 SHA-256；不把源码构建缓存、旧原型、个人存档和日志放入发布包。
- `install.ps1`：更新已有工坊关联安装时保留 `About/PublishedFileId.txt`，避免后续上传指向一个新物品。

## 公开前还需完成

1. 核对公开仓库只包含本 Mod 的源码、运行文件、说明、许可证、Issue 模板和打包脚本。**不要直接发布整个 `Rimworld Modding Trial` 工作区**；其中 `_qa` 包含私人存档和日志副本。
2. 检查工坊封面/预览图的版权和显示效果。`About/Preview.png` 目前不存在；若制作，需将它加入打包清单并检查压缩包。
3. 如果希望公开声称“完整停用、重启、重读档已实测”，在存档副本上实际完成这一步并记录结果。当前 README 对此保持明确限定。
4. 若将来遇到原版/OA 授勋或非殖民者范围的兼容问题，先复现并修订说明或代码。140+ Mod 环境中观察到的行为不等于所有组合均兼容。
5. 发布前重新运行构建、受控测试、安装脚本隔离测试与打包校验。确认包内 Mod DLL 与当前经游戏验证的 DLL 一致；代码改动后需要重新进游戏验证。
6. 上传工坊时使用发布包中的 `HonorNotLuxury` 目录，核对依赖 Harmony、Royalty 和加载顺序。首次上传后保存工坊物品 ID；后续更新确认 `About/PublishedFileId.txt` 没有丢失。不要同时启用本地与工坊两个同包 ID 的副本。

## 本地打包

在工作区根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

输出为 `Releases/HonorNotLuxury-2.0.0.zip`。其中的 `HonorNotLuxury` 文件夹可供手工安装或工坊上传；内含可供 Fork 的源码，但不含 `Source/bin` 或 `Source/obj`。项目根部的脚本和资料不属于游戏运行内容。`MANIFEST.json` 保存每个打包文件的 SHA-256 和人类实测范围。打包脚本会在成功写入并校验新压缩包后替换同名旧包，并把旧包保留为 `.previous.zip`。

## 版本维护原则

保持 `About/packageId` 为 `local.honornotluxury`，避免现有用户的设置与启用记录断裂。需要变更它时先提供迁移说明。每次游戏更新先编译并检查 Harmony 目标，再对有实际需求的高阶头衔做实机回归；有代码变化时更新版本号、说明、发布包文件名和 manifest。Issues 按可复现程度处理，修复顺序以本人游玩环境为主。接受 Fork / Continued，遵守 MIT 许可证并保留版权声明。
