# 留声论坛 · Unity 客户端

这里是正式的 Unity 客户端工程。当前只放技术骨架，不放剧情、关卡、美术成品或线上服务密钥。

## 分层约定

- `Runtime/Domain`：纯游戏规则与数据模型；不能依赖 Unity 场景、网络或存档实现。
- `Runtime/Application`：用例编排，例如将“查看帖子 / 接取事件 / 结算”转换为领域命令。
- `Runtime/Infrastructure`：本地存档、将来的 Steam / 服务端 / 网络适配器。
- `Runtime/Presentation`：Unity 场景、UI、输入、音频和演出；只能调用 Application。
- `Content`：可配置内容；后续事件、道具、帖子模板会放这里。
- `Scenes`：场景文件；先预留，不创建具体玩法场景。

单机阶段使用本地实现；以后联机时替换 Infrastructure 的适配器和会话宿主，不让 UI 或剧情内容直接绑定某个网络方案。
