# 成品案例与 GitHub 参考库

> 目的：从已经发售、运营或被大量使用的产品中提炼“什么被验证过”，从公开源码中学习架构和测试方法。不是复制任何游戏、视觉、文本、剧情、资源或 GPL 代码。

## 1. 如何使用这份参考

| 标记 | 含义 |
| --- | --- |
| 体验参考 | 只研究玩家行为、信息结构、节奏与范围控制，不能复制内容或 UI。 |
| 架构参考 | 阅读数据模型、模块边界、测试和部署；默认不复制源码。 |
| 可选依赖 | 未来可作为正式 package/service 评估，但必须固定版本、审计许可证和做最小样机。 |
| 不直接采用 | 说明它有价值，但不适合本项目当前阶段。 |

所有第三方代码接入前都要记录：仓库 URL、固定 commit/tag、许可证、维护状态、升级负责人、移除方案。即使开源，也不能因为“GitHub 上能下载”就直接复制。

## 2. 市面已验证的相关成品

### 2.1 Hypnospace Outlaw：虚构网络必须是“可用系统”

这款已发售作品将虚构网络、邮箱、应用、网页浏览和任务调查放在同一套操作系统体验中；官方描述它为 90 年代互联网模拟器，玩家要在大量网页、收件箱、病毒和应用中寻找违规线索。 [Nintendo 官方页](https://www.nintendo.com/us/store/products/hypnospace-outlaw-switch/)、[PlayStation 官方介绍](https://blog.playstation.com/2020/03/27/explore-the-internet-of-1999-in-hypnospace-outlaw-out-now-on-ps4/)

可以借鉴：

- 论坛不能只是“任务列表皮肤”；帖子、个人页、通知、历史内容、附件和工具都要形成相互印证的使用痕迹。
- 信息发现要有多条途径：板块、搜索、订阅、引用、私信/通知、已读痕迹，而不是一个唯一高亮按钮。
- 可探索内容必须设置范围和状态，否则玩家会在真假线索里迷失。

不借鉴：复古操作系统外观、其网页/角色/音乐、将整款游戏锁死在电脑屏幕中。本项目的核心仍是“论坛后进入实地调查，再回论坛”。

### 2.2 Welcome to the Game：电脑界面可以承载悬念，但不能代替调查

该已发售恐怖解谜游戏以电脑屏幕和虚构深网浏览为主要操作，玩家通过查找隐藏信息和应对压力源推进。 [Steam 商店页](https://store.steampowered.com/app/485380/Welcome_to_the_Game/)

可以借鉴：

- 搜索、整理、判断信息可靠性本身可以制造紧张感。
- UI 的反馈、等待、异常提示和信息不完整会改变氛围。

必须避开：它的“深网/黑客”题材、玩法结构和具体恐怖表达。更重要的是，本项目不能只让玩家坐在论坛里点网页；每次事件必须落到可操作的调查场景与道具规则上。

### 2.3 The Black Watchmen：社区参与要有现实边界

它自称为持续运行的 ARG，以现实世界信息和玩家协作作为玩法的一部分。 [The Black Watchmen 官方站](https://irc.blackwatchmen.com/)

可以借鉴：

- 长期社区可以通过官方任务、定时更新和玩家协作保持“世界仍在发生”的感觉。
- 官方账号、活动节点与普通用户讨论应在同一社区结构内出现，但权限不同。

必须避免：要求玩家接触真实地点、真实个人、真实危险信息，或把游戏谜题伪装成现实求助。本项目的一切任务地点、帖子对象和调查结果都应清晰处于虚构世界内。

### 2.4 Ink / Yarn Spinner：成熟内容工具的正确位置

Ink 是用于互动叙事的开源脚本语言并有 Unity 集成；Yarn Spinner 是面向编剧式对话的工具，已用于多款已发售游戏。 [Ink](https://github.com/inkle/ink)、[Ink Unity 集成](https://github.com/inkle/ink-unity-integration)、[Yarn Spinner](https://github.com/YarnSpinnerTool/YarnSpinner)

它们将来适合承担：场景内对话、演出分支、局部可选文本。

它们**不**承担：论坛数据库、全局任务状态机、玩家帖审核、多人同步。任务核心仍使用项目自己的 `CaseSession + Command/Event + 内容包 Schema`。这能避免内容工具变成游戏所有逻辑的单点。

## 3. GitHub 参考仓库清单

### A. 论坛与社区（架构参考，不直接嵌入）

| 仓库 | 观察重点 | 采用方式 | 禁区 |
| --- | --- | --- | --- |
| [NodeBB/NodeBB](https://github.com/NodeBB/NodeBB) | Node.js、WebSocket 实时讨论、REST API、分类论坛、插件边界，支持 PostgreSQL/Redis 等数据层 | 研究主题、楼层、通知、权限、实时扇出的划分 | GPL-3.0；不拷贝源码进闭源 Steam 项目，不把整个 NodeBB 套进 Unity。 |
| [discourse/discourse](https://github.com/discourse/discourse) | 已长期运营的论坛，使用 PostgreSQL + Redis；话题、审核、信任、备份和插件的系统化做法 | 研究治理、审核队列、修订历史、运维测试思路 | GPL-2.0；不复制代码，不继承其 Web UI/技术栈。 |
| [flarum/framework](https://github.com/flarum/framework) | 极简核心、扩展 API、基于资源的授权 | 研究权限字符串、扩展点和“核心保持小”的原则 | 不把 PHP/Laravel 引入当前 Node 服务，仅看设计。 |

为什么不直接选一个：NodeBB、Discourse、Flarum 都是成熟论坛，但本项目需要 Unity 内嵌氛围 UI、离线论坛快照、任务状态投影和可控 AI。直接改造会比自建有严格范围的 Forum API 更难维护。NodeBB 的实时/REST/插件方向、Discourse 的 PostgreSQL + Redis 生产组合，反而是很好的验证样本。 [NodeBB README](https://github.com/nodebb/nodebb)、[Discourse README](https://github.com/discourse/discourse)

### B. 未来合作联机（先读，后做样机）

| 仓库 | 观察重点 | 采用决定 |
| --- | --- | --- |
| [Unity Multiplayer Coop Sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) | 小规模合作游戏中的游戏流程状态机、场景加载与进度共享 | **最优先读**；用于 P5 的 2 人灰盒样机，不复制其具体玩法。 |
| [Unity Matchplay Sample](https://github.com/Unity-Technologies/com.unity.services.samples.matchplay) | 匹配与托管服务的端到端示例 | 当我们确认要“公开匹配”时再研究；现在不接云服务。 |
| [heroiclabs/nakama](https://github.com/heroiclabs/nakama) | 身份、好友、群组、聊天、通知、匹配、权威/中继多人、后台运行时 | 作为 NGO 样机不足后的**备选服务**；不在单机阶段部署。 |
| [Heroic Labs Unity 示例](https://heroiclabs.com/docs/nakama/client-libraries/unity/) | Unity 侧 socket、会话、匹配接入方式 | 只用于评估 Nakama 的实际接入成本。 |

Unity 的官方合作样例明确包含游戏流程状态机以及场景加载/进度共享，这正是我们将来需要验证、但绝不能一开始塞进正式内容的部分。 [Unity Coop Sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop)

### C. 内容包、资源与工具链

| 仓库/工具 | 观察重点 | 采用决定 |
| --- | --- | --- |
| [Unity Addressables Sample](https://github.com/Unity-Technologies/Addressables-Sample) | 基础/高级资源分组与远程内容样例 | 作为 Addressables 落地参考；正式版本由自己的内容 manifest 管理。 |
| [inkle/ink](https://github.com/inkle/ink) | 分支对话、变量、编译 JSON、Unity 运行时 | 将来仅用于关键演出或对话局部，先不安装。 |
| [YarnSpinnerTool/YarnSpinner](https://github.com/YarnSpinnerTool/YarnSpinner) | 编剧式对话脚本、运行时把文本/选项/场景命令交给游戏 | 与 Ink 二选一做小样机，不能同时接入。 |

Addressables 的官方样例是资源加载的起点；Addressables 内容更新有版本状态文件要求，发布后的内容更新与代码更新也必须分开管理。 [Addressables Sample](https://github.com/Unity-Technologies/Addressables-Sample)、[内容更新流程](https://docs.unity.cn/Packages/com.unity.addressables%401.21/manual/ContentUpdateWorkflow.html)

### D. 服务端基础设施（作为技术参考）

| 项目/标准 | 观察重点 | 落地方式 |
| --- | --- | --- |
| [PostgreSQL](https://www.postgresql.org/docs/17/textsearch.html) | 全文检索、排序、索引和事务 | 首版论坛主库与基础搜索。 |
| [Redis Streams](https://redis.io/docs/latest/develop/data-types/streams/) | 可确认、可重试的消费者组 | 通知、AI、附件扫描、索引更新的作业总线。 |
| [OpenTelemetry](https://opentelemetry.io/docs/concepts/signals/) | traces / metrics / logs | API、worker、未来联机会话的统一可观测性。 |
| [OWASP File Upload](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html) | 附件安全边界 | 附件上线前的验收清单，而不是依赖。 |

## 4. “拿来用”与“拿来学”的边界

```text
可以直接采用（经版本/许可证审计）
  Unity 官方 Package、Steamworks SDK、PostgreSQL、Redis、OpenTelemetry、Docker

可以做隔离样机后采用
  Unity NGO、Nakama、Ink 或 Yarn Spinner（只选一个内容工具）

只研究架构，不复制源码
  NodeBB、Discourse、Flarum、Unity 官方 Sample 的项目实现

绝不直接复用
  其他游戏的资源、文本、世界观、UI、音频、任务结构、玩家数据或 GPL 代码片段
```

特别提醒：即便某个仓库使用了看似相同的技术栈，也不能复制其业务代码。论坛的攻击面、隐私政策、Steam 身份、AI 内容治理都必须按本项目重新设计与测试。

## 5. 推荐的源码阅读顺序

不是现在全部 clone 下来，而是到相应阶段只读需要的部分：

1. **P0/P1：**Unity Addressables Sample，Unity Coop Sample 的状态机/进度同步结构；建立自己的 `Domain` 和内容包边界。
2. **P2：**NodeBB 的主题/实时通知理念，Discourse 的审核与运营思路，Flarum 的资源授权方式；实现自己的 Forum API。
3. **P3：**PostgreSQL/Redis 官方文档；做迁移、Outbox、搜索和作业可靠性测试。
4. **P4：**OWASP 文件上传和 LLM 提示词注入防护；再接附件和 AI。
5. **P5：**Unity Coop Sample 做 2 人灰盒；只有出现明确的匹配/权威会话需求才读 Nakama 实现。
6. **演出开始前：**用一个小场景比较 Ink 与 Yarn Spinner，胜者进入项目，另一个不再安装。

## 6. 这批案例带来的范围警报

- 虚构互联网游戏最容易失控在“每一页都要写、每一个功能都要做”。先把论坛作为任务闭环和社区功能，不做无限网页宇宙。
- 真实论坛最难的不是帖子列表，而是审核、垃圾内容、附件、通知、搜索、隐私和长期运维。
- 组队最难的不是“看见另一个玩家”，而是权威状态、掉线重连、奖励幂等和内容不分歧。
- 叙事工具可以提高内容生产效率，但不能替代全局任务规则和存档迁移。

因此当前最正确的开始仍是：单机灰盒闭环 + 可扩展的领域层；不是先接十个 GitHub 项目。
