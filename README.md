Unity + Mirror 多人足球游戏 MVP 项目总结（当前项目结构修正版）

用途：给新对话、Codex、毕业设计说明或项目交接使用。本文根据 Assets/Scripts/MultiplePlayers/ 下当前脚本整理，说明项目结构、脚本职责、网络架构、比赛规则、队伍系统、AI 系统和 UI 流程。


一、项目基本信息


项目类型：Unity 多人在线足球游戏 MVP。
网络框架：Mirror。
核心目标：实现一个可以通过 Host / Client / Server 联机运行的在线足球游戏，支持玩家进入房间、选择队伍和位置、Ready、Host 开始比赛、玩家移动跳跃、运球踢球、进球出界、定位球、比分计时、AI 补齐到 5v5，以及主菜单、网络启动、选队、Lobby、比赛 HUD 等 UI 流程。

核心原则：服务器权威。
- 客户端主要负责输入、摄像机、UI 显示和向服务器提交请求。
- 服务器负责比赛阶段、Ready 判断、开始比赛、比分、倒计时、进球、出界、定位球、球状态、球重置、AI 生成和 AI 决策。
- 客户端不能直接决定比分、比赛状态、规则归属、球重置、AI 行为或比赛结束。

当前项目已经从“玩法 MVP”进入“玩法 + UI 流程整合完成”的状态。当前主要主菜单 UI 由 Assets/Scripts/MultiplePlayers/UI/MPSimpleMainMenuUI.cs 负责；旧版 OnGUI、MPMainMenuUI/MPNetworkMenuUI 等脚本仍保留为备份、兼容或后续网络启动界面参考。


二、目录结构概览


主要代码目录：Assets/Scripts/MultiplePlayers/

1. 根目录脚本
- MPNetworkManager.cs
- MPNetworkLauncherUI.cs
- MPNetworkBall.cs
- MPNetworkPlayerController.cs
- MPPlayerBallController.cs
- MPPlayerReadyState.cs
- MPSetPieceController.cs

2. MatchManageUi/
- MPGameSession.cs
- MPMatchHUD.cs
- MPFootballTypes.cs
- MPBoundaryReporter.cs

3. Team/
- MPPlayerTeamState.cs
- MPTeamRoster.cs
- MPTeamSelectHUD.cs
- MPTeamTypes.cs

4. AI/
- MPAIManager.cs
- MPAIPlayerController.cs
- MPAIFormationPoint.cs
- MPTeamUtility.cs

5. PlayerMoveAndCamera/
- MPThirdPersonCameraController.cs
- MPPlayerAnimationController.cs

6. UI/
- MPSimpleMainMenuUI.cs
- MPMainMenuUI.cs
- MPNetworkMenuUI.cs
- MPGameUIRoot.cs
- MPTeamSelectPanel.cs
- MPLobbyPanel.cs
- MPRuntimeUIFactory.cs
- MPUIVisibilityUtility.cs


三、运行模式与场景架构


当前支持三种 Mirror 运行模式：

1. Host
- 同一进程同时运行 Server 和 Client。
- Host 玩家可以参与比赛。
- Host 可以在满足 Ready 条件后点击 Start Game。
- 适合本机多开测试。

2. Client
- 连接 Host 或专用 Server。
- 通过 UI 输入 IP 和端口后启动客户端。
- Client 选择队伍和位置，手动 Ready，等待 Host 开始比赛。

3. Server
- 只运行服务器，不作为玩家。
- 不显示玩家 UI，不显示选队面板，不启用场景主摄像机和 AudioListener。
- 负责服务器权威逻辑、规则判断、AI 生成和网络同步。

当前主要两场景结构：

1. MainMenuScene
- 主菜单。
- 设置界面。
- 由 MPSimpleMainMenuUI 负责主菜单按钮、设置面板和进入比赛场景。
- MPNetworkManager 所在场景。

2. MultiplePlayers
- 足球场地。
- 玩家和球。
- TeamSystem / MPGameSession / MPTeamRoster / MPAIManager / MPTeamUtility。
- 选队面板、Lobby 面板、比赛 HUD、GameOver 显示。

MPNetworkManager 默认场景配置：
- defaultOfflineScene  MainMenuScene。
- defaultOnlineScene  MultiplePlayers。
- Awake 中如果 NetworkManager 的 offlineScene / onlineScene 未设置，会自动使用默认值。
- 不应该在多个场景中创建多个 NetworkManager。


四、比赛状态 MPMatchState


MPMatchState 定义在 MPFootballTypes.cs 中。

1. Lobby
- 玩家连接、选队、选位置、Ready。
- 只有 Lobby 阶段允许选队。
- Ready 需要玩家已经完成有效队伍和位置选择。
- Host 在 Ready 条件满足后可以开始比赛。

2. Playing
- 正常比赛阶段。
- 玩家可以移动、跳跃、运球、地滚球射门、挑球。
- AI 可以回位、追球、传球、解围或射门。
- 服务器处理倒计时、进球、出界、比分和规则事件。

3. RulePause
- 规则暂停阶段。
- 进球或出界后进入短暂停顿。
- 比赛计时通过 accumulatedPauseSeconds 暂停扣除。
- 玩家和 AI 不应继续常规控制。

4. SetPiece
- 定位球阶段。
- 用于界外球、角球、球门球等重新开始比赛。
- 服务器指定执行者、摆放球、传送执行者到发球点附近。
- 只有指定执行者可以按住鼠标左键蓄力并释放发球。
- 摄像机只允许定位球执行者控制。

5. TimeUp
- 比赛时间结束。
- 显示 GAME OVER。
- 等待 shutdownDelaySeconds 后进入关闭流程。

6. Closing
- 收尾阶段。
- 服务器通过 RPC 通知客户端退出。
- Host / Server 停止网络运行。


五、核心数据类型


1. MPTeam
- 比赛规则层队伍枚举。
- None、RedLeft、BlueRight。
- 用于比分、进球归属、最后触球、规则判定和球门方向。

2. MPTeamId
- 选队 UI 和队伍系统使用的队伍枚举。
- None、Red、Blue。
- 通过 MPPlayerTeamState.ToMatchTeam 映射到 MPTeam。

3. MPPlayerPosition
- 球员位置枚举。
- None、Goalkeeper、Defender、Midfielder、Forward。
- 用于真人选位、AI 补齐、阵型点分配。

4. MPControlType
- 控制类型枚举。
- Human、AI。
- 用于区分真人玩家和 AI 球员。

5. MPBoundaryType
- 边界触发器类型。
- 包括上下边线、左右球门进球区、左右底线出界区。

6. MPRuleEventType
- 规则事件类型。
- Goal、ThrowIn、CornerKick、GoalKick、PenaltyKick、UnknownOut。

7. MPSetPieceMode
- 定位球模式。
- GroundKick、ElevatedThrow。
- 界外球使用 ElevatedThrow，角球和球门球使用 GroundKick。

8. MPRestartLocation
- 重新开始比赛的位置。
- Center、TouchLineTop、TouchLineBottom、LeftTopCorner、LeftBottomCorner、RightTopCorner、RightBottomCorner、LeftGoalKick、RightGoalKick。

9. MPRuleDecision
- 服务器规则判定结果结构体。
- 包含事件类型、获利队伍、重启位置和中心提示文本。

10. MPBallState
- 球状态枚举。
- Free、Dribbling、Kicked。
- 用于区分自由球、被玩家控制、被踢出后的状态。


六、主要场景对象建议结构


1. NetworkManager 对象
- NetworkIdentity 不需要单独挂在 UI 上。
- 挂 MPNetworkManager。
- 挂 Mirror Transport。
- 配置 Player Prefab。
- 配置 Spawnable Prefabs，包括 NetworkBall 和 AI Player Prefab。
- Offline Scene 使用 MainMenuScene。
- Online Scene 使用 MultiplePlayers。

2. MainMenuScene UI
- 当前主流程挂 MPSimpleMainMenuUI。
- 可手动绑定 mainPanel、settingsPanel、Start Game、Settings、Quit、Back、音量、全屏和画质控件。
- Start Game 默认加载 MultiplePlayers 场景。
- MPMainMenuUI 和 MPNetworkMenuUI 仍在项目中，可作为旧版/备用网络启动 UI，不是当前主菜单的主要入口。

3. MultiplePlayers 场景 TeamSystem
- NetworkIdentity。
- MPGameSession。
- MPTeamRoster。
- MPAIManager。
- MPTeamUtility。
- 定位球点 Transform 引用。
- AI Prefab 引用。
- 球门坐标配置。

4. MultiplePlayers 场景 UI Root
- MPGameUIRoot。
- MPTeamSelectPanel。
- MPLobbyPanel。
- MPMatchHUD。
- 可选 GameOverPanel。
- MPGameUIRoot 负责等待 NetworkClient.localPlayer，初始化本地玩家 UI，隐藏旧选队 HUD，控制不同阶段面板显示。

5. Player Prefab
- NetworkIdentity。
- Rigidbody。
- Collider。
- NetworkTransform / NetworkRigidbody 相关同步组件按项目实际配置。
- MPNetworkPlayerController。
- MPPlayerBallController。
- MPPlayerReadyState。
- MPPlayerTeamState。
- MPSetPieceController。
- MPThirdPersonCameraController。
- MPPlayerAnimationController。
- 人形模型和 Animator。
- 本地玩家读取输入和控制摄像机，非本地玩家不读取输入。

6. AI Player Prefab
- NetworkIdentity。
- Rigidbody。
- Collider。
- MPPlayerTeamState。
- MPAIPlayerController。
- MPPlayerAnimationController。
- 人形模型和 Animator。
- 必须加入 NetworkManager 的 Spawnable Prefabs。
- AI 决策只在服务器执行。

7. NetworkBall Prefab
- NetworkIdentity。
- Rigidbody。
- Collider。
- NetworkRigidbodyReliable 推荐使用 ServerToClient。
- MPNetworkBall。
- 服务器驱动物理和规则状态，客户端接收同步。

8. 边界对象
- 球门 Trigger。
- 边线 Trigger。
- 底线 Trigger。
- 挂 MPBoundaryReporter。
- boundaryType 配置为对应 MPBoundaryType。
- 只在服务器上向 MPGameSession 报告球进入边界。

9. AI 阵型点
- 挂 MPAIFormationPoint。
- 配置 teamId、position、index。
- MPAIManager 用这些点为 AI 分配出生点和回位目标。


七、已实现功能总览


1. 网络连接
- Host / Client / Server 启动。
- IP 输入，默认 127.0.0.1。
- 端口输入，默认 7777。
- 支持 Mirror 场景切换。
- 服务器进入 MultiplePlayers 场景后自动生成比赛用球。

2. 主菜单 UI
- Start Game。
- Settings。
- Quit。
- 当前主要脚本是 MPSimpleMainMenuUI。
- Settings 可同步和调整主音量、全屏、画质等设置控件。
- Start Game 默认直接加载 MultiplePlayers 场景。
- Back 可从设置界面返回主菜单。
- MPMainMenuUI/MPNetworkMenuUI 保留旧版主菜单和网络启动面板逻辑，可按需要重新接入。

3. 选队和 Lobby UI
- 进入 MultiplePlayers 场景后等待本地玩家生成。
- 本地玩家生成后显示 TeamSelectPanel。
- 提供 Red GK、Red DF、Red MF、Red FW、Blue GK、Blue DF、Blue MF、Blue FW 组合按钮。
- 点击后通过 Command 请求服务器设置队伍和位置。
- 服务器确认后锁定选择、关闭选队面板、显示 LobbyPanel。
- 选队完成后不会自动 Ready。
- 玩家在 LobbyPanel 中手动 Ready / Cancel Ready。
- Host 显示 Start Game 按钮。
- Start Game 按钮只有 Ready 条件满足时可点击。

4. 比赛 HUD
- 显示 Lobby Ready 状态。
- 显示比赛时间。
- 显示剩余时间。
- 显示比分。
- 显示中心提示。
- 显示 GAME OVER 和关闭倒计时。
- MPGameUIRoot 接管阶段面板显示，MPMatchHUD 只负责比赛内信息刷新。

5. 玩家控制
- WASD / Horizontal / Vertical 移动。
- 移动方向基于第三人称摄像机朝向。
- Space 跳跃。
- SphereCast 地面检测。
- Rigidbody 移动和转向。
- 非本地玩家禁用本地输入和本地物理控制。
- 非可控制比赛阶段会冻结水平移动。

6. 第三人称摄像机
- 只由本地玩家控制。
- 鼠标控制 yaw / pitch。
- 平滑跟随玩家。
- Playing 阶段可控制。
- SetPiece 阶段只有定位球执行者可控制。
- ServerOnly 模式禁用场景摄像机和 AudioListener。

7. 玩家球交互
- 服务器通过前方 OverlapBox 检测可控制球。
- 玩家接近并面向球时可开始 Dribbling。
- 运球时球被锁定到玩家前方目标位置。
- 球距离目标过远会自动解除控球，减少绕人转圈问题。
- 鼠标左键蓄力地滚球。
- 鼠标右键蓄力挑球。
- 蓄力时长由服务器用 NetworkTime 计算。
- 踢球后设置短暂 reControlCooldown，避免马上重新控球。

8. 网络球
- 服务器维护 Free / Dribbling / Kicked 状态。
- 服务器维护 controllerNetId、canBeControlledTime。
- 服务器限制最大速度。
- 球掉落到 resetHeight 以下会重置到出生点。
- 记录最后触球队伍和最后触球玩家。
- 支持玩家控球、玩家踢球、AI 踢散球、定位球发球、球重置和定位球摆放。

9. 比赛规则
- 120 分钟倒计时，按真实秒数配置为 120 * 60。
- 记录 RedLeft 和 BlueRight 比分。
- 球进入左侧球门判定 BlueRight 进球。
- 球进入右侧球门判定 RedLeft 进球。
- 上下边线出界判定界外球。
- 左右底线出界根据最后触球队伍判定角球或球门球。
- 规则事件有 lockout，避免短时间重复触发。
- 进球或出界后进入 RulePause。
- 可使用定位球的事件进入 SetPiece。
- 非定位球事件可重置球后恢复 Playing。

10. 定位球
- 界外球使用 ElevatedThrow。
- 角球、球门球使用 GroundKick。
- 服务器寻找获利队伍中离重启点最近的真人玩家作为执行者。
- 服务器将球摆放到重启点。
- 服务器将执行者传送到球附近并朝向球。
- 执行者按住鼠标左键蓄力，松开后发球。
- 发球力根据事件类型和蓄力百分比计算。
- 发球后清空定位球状态并恢复 Playing。

11. Ready / Start
- MPPlayerReadyState 保存玩家 Ready SyncVar。
- Ready 前必须完成队伍和位置选择。
- 只有 Lobby 阶段可以改变 Ready。
- MPGameSession 统计 requiredReadyPlayers、readyPlayers、allClientsReady。
- includeHostInReadyCheck 默认 false，意味着只要求真正连接进来的 Client Ready，Host 不一定需要 Ready。
- Host / Server 调用 ServerTryStartGame 进入 Playing。

12. AI
- 比赛开始时服务器调用 MPAIManager.ServerSpawnAIsForMatch。
- 每队补齐到 targetPlayersPerTeam，默认 5 人。
- 根据真人玩家已选位置，优先补齐缺失的 GK / DF / MF / FW。
- 人数不足时按 Defender / Midfielder / Forward 等顺序重复补位。
- AI 使用阵型点或 fallback 位置出生。
- AI 只在 Playing 阶段行动。
- AI 状态包括 Idle、ReturnToFormation、ChaseBall、KickOrPass。
- 每队选出距离球合适的最佳追球 AI。
- 非最佳追球者回到阵型点。
- 接近球后尝试向前方队友传球，否则向对方球门射门或向前解围。
- 守门员有追球范围限制，避免离开球门过远。

13. 动画
- MPPlayerAnimationController 控制 Speed、IsGrounded、Jump。
- 本地玩家根据位移速度计算动画速度。
- 通过 Command、SyncVar、ClientRpc 同步远端动画。
- AI 可由服务器调用 ServerSetMoveSpeed 和 ServerSetGrounded 更新动画状态。

14. 运行时 UI 自动生成
- 如果场景没有手动搭好旧版 UI 引用，MPMainMenuUI、MPNetworkMenuUI、MPTeamSelectPanel、MPLobbyPanel 会通过 MPRuntimeUIFactory 创建基础 UI。
- 当前主菜单主流程使用 MPSimpleMainMenuUI 手动绑定的场景 UI，不依赖运行时自动生成主菜单。
- 自动创建 Canvas、CanvasScaler、GraphicRaycaster、EventSystem。
- MPUIVisibilityUtility 统一使用 SetActive + CanvasGroup alpha/interactable/blocksRaycasts 显示隐藏面板。


八、脚本职责详解


1. MPNetworkManager
- 继承 Mirror NetworkManager。
- 设置默认 Offline Scene 和 Online Scene。
- 启动服务器时重置队伍分配索引。
- 客户端启动时注册 ballPrefab。
- 服务器切到 MultiplePlayers 场景后生成比赛用球。
- 玩家连接时生成 Player Prefab。
- 给玩家设置显示名 Player 1、Player 2 等。
- 给玩家临时分配 RedLeft / BlueRight 交替队伍，正式队伍选择后由 MPPlayerTeamState 覆盖。
- 玩家断开时通知 MPGameSession 刷新 Ready 统计。

2. MPNetworkMenuUI
- 旧版/备用网络启动 UI。
- 提供 IP 输入框、端口输入框、Start Host、Start Client、Start Server、Back。
- 还提供 Local Game，可直接加载 MultiplePlayers 场景。
- 默认 IP 为 127.0.0.1，默认端口为 7777。
- 启动前把 IP 写入 networkManager.networkAddress。
- 如果当前 Transport 支持 PortTransport，则尝试运行时设置端口。
- 如果没有手动绑定 UI，会自动生成 NetworkLaunchPanel。

3. MPSimpleMainMenuUI
- 当前主要主菜单 UI。
- 管理 mainPanel 和 settingsPanel。
- Start Game 默认加载 MultiplePlayers 场景。
- Settings 显示设置面板并同步音量、全屏和画质控件。
- Quit 在 Editor 中停止播放，在 Build 中退出程序。
- Back 从设置面板返回主菜单。

4. MPMainMenuUI
- 旧版/备用主菜单 UI。
- 管理 MainMenuPanel、NetworkLaunchPanel、SettingsPanel。
- Start Game 显示网络启动界面。
- Settings 显示设置空壳。
- Quit 在 Editor 中停止播放，在 Build 中退出程序。
- 如果没有手动绑定 UI，会自动生成 MainMenuPanel 和 SettingsPanel。

5. MPGameUIRoot
- MultiplePlayers 场景 UI 总控。
- 等待 NetworkClient.localPlayer 生成。
- 获取本地玩家的 MPPlayerTeamState 和 MPPlayerReadyState。
- 初始化 MPTeamSelectPanel 和 MPLobbyPanel。
- 禁用旧 MPTeamSelectHUD。
- ServerOnly 模式隐藏全部玩家 UI，并关闭场景主摄像机和 AudioListener。
- Lobby 阶段未选队时显示 TeamSelectPanel。
- 选队成功后锁定选择，显示 LobbyPanel。
- 非 Lobby 阶段隐藏选队和 Lobby，显示比赛 HUD。

6. MPTeamSelectPanel
- 正式选队弹窗。
- 提供八个组合按钮：Red GK、Red DF、Red MF、Red FW、Blue GK、Blue DF、Blue MF、Blue FW。
- 点击按钮后调用 MPPlayerTeamState.CmdRequestSelectTeam。
- 请求期间禁用按钮。
- 通过 MPPlayerTeamState.SelectionRequestResolved 接收服务器结果。
- 成功后锁定选择并触发 SelectionConfirmed。
- 成功后不允许重新打开并修改选择。

7. MPLobbyPanel
- 正式 Lobby 面板。
- 显示本地玩家队伍 / 位置。
- 显示 Ready 计数。
- 提供 Ready / Cancel Ready 按钮。
- Host 显示 Start Game 按钮。
- Start Game 只有 session.AllClientsReady 为 true 时可用。

8. MPMatchHUD
- 比赛 HUD。
- 显示 Ready 信息、计时、剩余时间、比分、中心提示、GameOver、关闭倒计时。
- 可由 MPGameUIRoot 设置为外部管理模式，避免自己和新 UI Root 同时控制阶段面板。
- Ready 和 Start Game 功能仍保留，但正式流程主要由 MPLobbyPanel 负责 Lobby。

9. MPRuntimeUIFactory
- 运行时 UI 工厂。
- 创建 Canvas、EventSystem、Panel、Text、Button、InputField、VerticalLayoutGroup、GridLayoutGroup。
- 用于在没有手工搭建 UI 的情况下保证项目能跑通 UI 流程。

10. MPUIVisibilityUtility
- UI 显示隐藏工具。
- Show：SetActive(true)，CanvasGroup alpha1、interactabletrue、blocksRaycaststrue。
- Hide：CanvasGroup alpha0、interactablefalse、blocksRaycastsfalse，然后 SetActive(false)。
- 避免透明 UI 拦截点击。

11. MPNetworkLauncherUI
- 旧版 OnGUI 网络启动 UI。
- 提供 Host / Client / Server / Stop。
- 当前作为备份或调试用，正式场景中应避免和其他网络启动 UI 同时启用。

12. MPGameSession
- 比赛规则核心。
- 维护 MPMatchState。
- 维护比赛时间、暂停时间、结束关闭时间。
- 维护 Ready 统计。
- 维护比分。
- 维护中心提示。
- 处理 Ready / Start。
- 处理进球、边线出界、底线出界。
- 判定界外球、角球、球门球。
- 进入 RulePause 和 SetPiece。
- 指定定位球执行者。
- 摆放球和传送执行者。
- 执行定位球后恢复比赛。
- 比赛结束后通知客户端退出并停止网络。

13. MPFootballTypes
- 存放核心枚举和规则判定结构体。
- 包含 MPTeam、MPBoundaryType、MPRuleEventType、MPMatchState、MPSetPieceMode、MPRestartLocation、MPRuleDecision、MPTeamId、MPPlayerPosition、MPControlType。

14. MPBoundaryReporter
- 挂在边界 Trigger 上。
- 只在 NetworkServer.active 时处理触发。
- 检测进入 Trigger 的对象是否是 MPNetworkBall。
- 把 boundaryType 和球位置报告给 MPGameSession。

15. MPNetworkBall
- 网络球和球规则状态。
- 服务器权威维护球状态。
- 支持被玩家开始控球、停止控球、锁定运球位置、踢球。
- 支持 AI 踢散球。
- 支持定位球摆放和定位球发球。
- 记录最后触球队伍和玩家 netId。
- 支持掉出场地后重置。
- 检查 NetworkRigidbodyReliable 是否是 ServerToClient 同步方向。

16. MPPlayerBallController
- 真人玩家球交互。
- 本地读取鼠标输入，服务器处理实际控球和踢球。
- 服务器通过前方盒子检测寻找可控球。
- 运球时服务器持续把球移动到玩家前方。
- 左键地滚球蓄力，右键挑球蓄力。
- 服务器计算蓄力比例并调用 MPNetworkBall.ServerKick。

17. MPSetPieceController
- 挂在 Player Prefab 上。
- 只处理定位球阶段本地执行者输入。
- 如果当前玩家不是 restartExecutorNetId，则不响应。
- 鼠标左键按下开始服务器蓄力。
- 鼠标左键释放时根据相机朝向或玩家朝向提交发球方向。
- 服务器验证执行者合法后调用 MPGameSession.ServerExecuteSetPiece。

18. MPNetworkPlayerController
- 真人玩家移动、跳跃、朝向和重启传送。
- 本地玩家读取键盘输入。
- 移动方向基于 MPThirdPersonCameraController。
- Rigidbody 控制水平速度和转向。
- Space 跳跃。
- SphereCast 更新地面状态。
- 非本地玩家在客户端设为 kinematic，避免被本地物理驱动。
- ServerTeleportForRestart 用于定位球阶段传送执行者。
- ServerSetDisplayName 和 ServerSetTeam 供服务器设置玩家信息。

19. MPThirdPersonCameraController
- 本地玩家第三人称摄像机。
- 使用 Camera.main。
- 鼠标控制 yaw / pitch。
- LateUpdate 平滑跟随。
- 为玩家移动提供相机相对方向。
- 根据 MPGameSession.CanControlCamera 判断是否能控制视角。

20. MPPlayerAnimationController
- 玩家和 AI 动画同步。
- 控制 Animator 参数 Speed、IsGrounded、Jump。
- 本地玩家计算速度并通过 Command 同步。
- Jump 通过 ClientRpc 播放给其他客户端。
- AI 可由服务器直接设置动画速度和地面状态。

21. MPPlayerReadyState
- 玩家 Ready 状态。
- SyncVar 保存 isReady。
- CmdSetReady 只能在 Lobby 阶段生效。
- 未完成选队和位置选择时不能 Ready。
- Ready 改变后通知 MPGameSession 刷新统计。

22. MPPlayerTeamState
- 玩家队伍、位置、控制类型状态。
- SyncVar 保存 TeamId、Position、ControlType。
- CmdRequestSelectTeam 向服务器请求选队。
- 服务器验证是否 Lobby、参数是否有效、MPTeamRoster 是否允许。
- 成功后设置 TeamId / Position，并同步到 MPNetworkPlayerController 的 MPTeam。
- TargetNotifySelectionResult 通知本地 UI 请求结果。

23. MPTeamRoster
- 队伍选择验证。
- 限制每队真人玩家数量，默认 maxHumanPlayersPerTeam5。
- allowSamePosition 默认 true，允许多个真人选择同一位置。
- 可检查所有真人玩家是否都有有效队伍选择。

24. MPTeamSelectHUD
- 旧版选队 UI。
- 常驻式选队面板，显示当前选择。
- 当前正式流程中由 MPGameUIRoot 自动禁用。
- 可作为旧版参考，不建议和 MPTeamSelectPanel 同时启用。

25. MPTeamTypes
- 当前是空 MonoBehaviour 模板脚本。
- 没有实际功能，核心队伍枚举已经在 MPFootballTypes.cs 中。

26. MPAIManager
- 服务器 AI 管理器。
- 比赛开始时每队补齐到 targetPlayersPerTeam。
- 统计队伍已有真人和 AI。
- 根据已占用位置生成 AI 位置列表。
- 查找阵型点。
- 实例化 AI Prefab，设置 MPPlayerTeamState 为 AI。
- 初始化 MPAIPlayerController。
- NetworkServer.Spawn AI。
- 提供 ServerGetBestChaser 给 AI 状态机选择最佳追球者。

27. MPAIPlayerController
- AI 状态机和行为执行。
- 只在服务器 FixedUpdate 行动。
- 非服务器客户端上 AI Rigidbody 设为 kinematic。
- Playing 之外进入 Idle 并停止移动。
- ReturnToFormation：回到阵型点。
- ChaseBall：追向球。
- KickOrPass：接近球后传球、射门或解围。
- 守门员只有球接近本方球门且自己没有离门太远时才追球。
- 调用 MPNetworkBall.ServerKickLooseBall 踢球。

28. MPAIFormationPoint
- AI 阵型点标记。
- 配置 teamId、position、index。
- MPAIManager 根据这些标记分配 AI 位置。

29. MPTeamUtility
- 队伍工具类。
- 提供红蓝球门位置。
- 提供进攻方向。
- 提供本方球门和对方球门坐标。
- 提供 MPTeamId 到 MPTeam 的转换。


九、核心数据流


1. 网络启动流
当前主菜单本地进入流程：MainMenuScene -> MPSimpleMainMenuUI 点击 Start Game -> 加载 MultiplePlayers 场景 -> 客户端等待本地玩家生成。
备用 Mirror 网络启动流程：MainMenuScene -> MPMainMenuUI/MPNetworkMenuUI -> 输入 IP / Port -> Start Host / Client / Server -> MPNetworkManager 启动 Mirror -> 切换到 MultiplePlayers 场景 -> 服务器生成球 -> 客户端等待本地玩家生成。

2. 选队和 Ready 流
进入 MultiplePlayers 场景 -> MPGameUIRoot 等待 NetworkClient.localPlayer -> 显示 MPTeamSelectPanel -> 点击 Red GK / Blue FW 等组合按钮 -> MPPlayerTeamState.CmdRequestSelectTeam -> MPTeamRoster 服务器验证 -> 服务器设置 TeamId / Position -> TargetRpc 通知客户端 -> TeamSelectPanel 锁定并关闭 -> MPLobbyPanel 显示 -> 玩家手动 Ready -> MPPlayerReadyState.CmdSetReady -> MPGameSession 刷新 Ready 统计 -> Host 点击 Start Game -> MPGameSession 进入 Playing -> MPAIManager 补齐 AI。

3. 玩家输入流
本地玩家读取键盘和鼠标 -> MPNetworkPlayerController 处理移动和跳跃 -> MPPlayerBallController / MPSetPieceController 通过 Command 请求服务器踢球或发球 -> 服务器验证比赛阶段和控制权 -> 修改权威球状态 -> NetworkRigidbody / SyncVar / RPC 同步到客户端。

4. 比赛规则流
球进入边界 Trigger -> MPBoundaryReporter 只在服务器报告 -> MPGameSession.ServerHandleBoundaryTriggered -> 根据 MPBoundaryType 和最后触球队伍生成 MPRuleDecision -> 进球则加分 -> 进入 RulePause -> 显示中心提示 -> 需要定位球则进入 SetPiece -> 指定执行者发球 -> 恢复 Playing。

5. AI 流
比赛开始 -> MPAIManager 检查每队人数 -> 生成 AI 补齐 5v5 -> AI 初始化队伍、位置、阵型点 -> Playing 阶段 MPAIPlayerController 执行状态机 -> 最佳追球 AI 追球 -> 其他 AI 回位 -> 接近球后传球 / 射门 / 解围 -> 非 Playing 阶段停止。

6. UI 显示流
MainMenuScene 当前主要使用 MPSimpleMainMenuUI。
MultiplePlayers 场景使用 MPGameUIRoot 管理 TeamSelectPanel、LobbyPanel、MatchHUD。
UI 只提交请求和显示结果，不直接修改比赛权威状态。


十、服务器权威设计总结


服务器权威内容：
- 玩家加入和玩家对象生成。
- 比赛阶段。
- Ready 统计和开始比赛判断。
- 比分。
- 比赛计时和暂停计时。
- 中心提示权威内容。
- 进球和出界判定。
- 界外球、角球、球门球归属。
- 定位球执行者、位置和发球。
- 球状态、球速度、控球者、最后触球。
- AI 生成、AI 状态机、AI 移动和踢球。

客户端负责内容：
- 主菜单和网络启动 UI。
- 选队、Ready、Start Game 按钮点击。
- 本地键盘鼠标输入。
- 本地第三人称摄像机。
- HUD 显示。
- 接收 SyncVar / RPC / NetworkTransform / NetworkRigidbody 同步后的表现。


十一、当前实现特点和注意事项


1. 正式 UI 和旧 UI 并存
- 当前主要主菜单 UI 是 MPSimpleMainMenuUI。
- MPNetworkMenuUI 是旧版/备用网络启动 UI。
- MPMainMenuUI 是旧版/备用主菜单 UI。
- MultiplePlayers 场景 UI 总控是 MPGameUIRoot。
- 正式选队面板是 MPTeamSelectPanel。
- 正式 Lobby 面板是 MPLobbyPanel。
- 旧 MPNetworkLauncherUI 和 MPTeamSelectHUD 仍存在，但正式场景中应避免启用。

2. UI 不需要 NetworkIdentity
- UI 脚本不应该挂 NetworkIdentity。
- UI 通过本地 NetworkClient.localPlayer 找到玩家组件，再调用 Command。

3. 本地玩家生成是异步的
- 不能在进入 MultiplePlayers 场景后立即假设 NetworkClient.localPlayer 存在。
- MPGameUIRoot 已经使用协程等待本地玩家生成。

4. ServerOnly 不显示玩家 UI
- NetworkServer.active 且 NetworkClient.isConnected 为 false 时认为是纯服务器。
- MPGameUIRoot 会隐藏 UI，并关闭 Camera.main 和 AudioListener。

5. Ready 条件
- 玩家必须先有 TeamId 和 Position。
- Ready 只在 Lobby 阶段有效。
- includeHostInReadyCheck 默认 false，Host 自己不一定计入 requiredReadyPlayers。

6. AI 生成时机
- AI 在 ServerTryStartGame 成功进入 Playing 时生成。
- MPAIManager 使用 spawnedForCurrentMatch 防止重复生成。

7. 球同步要求
- MPNetworkBall 会检查 NetworkRigidbodyReliable。
- 推荐 NetworkRigidbodyReliable.syncDirection  ServerToClient。
- 服务器驱动球物理，客户端只看同步结果。

8. 定位球执行者限制
- 当前定位球执行者从真人连接玩家中按距离选择。
- AI 不作为定位球执行者。
- 如果没有找到执行者，会提示 No restart player found 并重置球恢复比赛。

9. MPTeamTypes 当前无实际作用
- 这是空模板脚本。
- 项目实际队伍类型在 MPFootballTypes.cs 中。


十二、可以向别人介绍的一句话总结


这是一个 Unity + Mirror 实现的服务器权威多人在线足球游戏 MVP。项目当前主要使用 MainMenuScene 和 MultiplePlayers 两场景流程，包含主菜单、设置界面、选队弹窗、Lobby Ready、比赛 HUD、比分计时、GameOver；玩法上实现了玩家移动跳跃、第三人称相机、运球、地滚球和挑球、进球出界、界外球、角球、球门球、定位球发球，以及服务器生成 AI 自动补齐到 5v5 并进行简单追球、回位、传球、射门或解围。MPNetworkMenuUI 等网络启动脚本仍保留，可作为后续联机入口或备用流程。


十三、毕业设计/项目说明可用概括


本项目基于 Unity 和 Mirror 网络框架开发多人在线足球游戏原型，采用服务器权威架构。服务器负责比赛状态、规则判定、比分计时、球状态、定位球、AI 生成和 AI 决策，客户端负责输入、摄像机和 UI 表现。项目实现了 Host、Client、Server 三种运行方式，玩家可通过主菜单输入 IP 和端口启动网络连接，进入比赛场景后选择红蓝队与球员位置，在 Lobby 中 Ready，并由 Host 开始比赛。比赛中玩家可以进行第三人称移动、跳跃、运球和蓄力踢球；系统可以检测进球、边线出界、底线出界，并根据最后触球队伍判定界外球、角球或球门球。服务器会在比赛开始时为双方自动生成 AI 球员补齐到 5v5，AI 具有回到阵型、追球、传球、射门和解围等基础行为。UI 部分包含主菜单、网络启动、选队面板、Lobby 面板、比赛 HUD、比分时间显示、中心规则提示和比赛结束提示，使项目形成完整的在线足球游戏 MVP 流程。
