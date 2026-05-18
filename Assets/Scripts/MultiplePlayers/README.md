# MultiplePlayers + Mirror

这个目录保存 `MultiplePlayers` 场景后续要用到的联机脚本。

当前工程里还没有安装 Mirror，所以这批脚本采用了一个安全做法：

- 默认以占位模式编译，不会破坏现有工程。
- 等你把 Mirror 装进项目后，再加一个脚本宏 `USE_MIRROR`，这些脚本就会切换到正式联机实现。

## 当前已提供的脚本

- `MPNetworkManager.cs`
  - 自定义 `NetworkManager`
  - 负责玩家加入时生成 player prefab
- `MPNetworkPlayerController.cs`
  - 本地玩家输入控制
  - 使用 `Horizontal` / `Vertical` 轴移动
  - 只允许 `isLocalPlayer` 控制自己
- `MPNetworkLauncherUI.cs`
  - 一个简单的运行时 Host / Client / Server 启动面板
  - 支持输入地址并连接

## Mirror 接线步骤

1. 安装 Mirror。
2. 在 `Project Settings > Player > Scripting Define Symbols` 添加 `USE_MIRROR`。
3. 打开 `MultiplePlayers` 场景。
4. 创建一个空物体，例如 `NetworkRoot`。
5. 给 `NetworkRoot` 挂：
   - `Telepathy Transport` 或你选用的 Mirror Transport
   - `MPNetworkManager`
   - `MPNetworkLauncherUI`
6. 给玩家 prefab 挂：
   - `NetworkIdentity`
   - `NetworkTransform`
   - `MPNetworkPlayerController`
7. 在 `MPNetworkManager` 上把 `playerPrefab` 指向你的网络玩家 prefab。
8. 在场景里放若干 `NetworkStartPosition` 作为出生点。

## 位置同步建议

这个方案默认让本地拥有者读取输入并移动自身。

推荐按 Mirror 官方文档给玩家 prefab 添加 `NetworkTransform`，并把同步方向设为：

- `Client To Server`

这样主机和其他客户端都能看到该玩家的位置变化。

## 联机测试方式

- 同机测试：
  - 一个进程点 `Start Host`
  - 另一个进程点 `Start Client`
  - 地址填 `localhost` 或 `127.0.0.1`
- 局域网测试：
  - 主机进程点 `Start Host`
  - 其他客户端填主机局域网 IP 后点 `Start Client`

## 后续你大概率还会补的内容

- 玩家名、队伍、出生位分配
- 动画同步
- 足球、比赛状态、比分同步
- 服务器权威的球权与碰撞判定
- 断线重连与房间流程

## 参考

- Mirror docs: https://mirror-networking.gitbook.io/docs/manual/components/network-manager
- Mirror docs: https://mirror-networking.gitbook.io/docs/manual/components/network-transform
- Mirror docs: https://mirror-networking.gitbook.io/docs/manual/guides/gameobjects/player-gameobjects
- Mirror docs: https://mirror-networking.gitbook.io/docs/manual/guides/communications/remote-actions
- Mirror docs: https://mirror-networking.gitbook.io/docs/manual/components/network-manager-hud
