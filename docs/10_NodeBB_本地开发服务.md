# NodeBB 本地开发服务

服务目录：`E:\留声论坛\local-services\nodebb`。该目录被根仓库的 `.gitignore` 忽略。使用官方 NodeBB `v4.16.0` 与其 Compose 配置；本机配置把 HTTP 端口绑定到 `127.0.0.1:4567`，数据库不发布到宿主机。

## 启动

先启动 Docker Desktop。在服务目录运行：

```powershell
docker compose --profile redis up -d
```

## 健康检查

```powershell
docker compose --profile redis ps
curl.exe --noproxy '*' http://127.0.0.1:4567/api/config
```

浏览器可打开 `http://127.0.0.1:4567`。首次从 Windows 克隆官方仓库时，需确保 `nodebb` 和 `install/docker/entrypoint.sh` 使用 LF 换行。

## 停止

```powershell
docker compose --profile redis down
```

该命令保留数据库与配置数据。

## 清理

仅在明确要删除本机测试数据时，在服务目录执行 `docker compose --profile redis down -v`，再自行删除整个 `local-services/nodebb` 目录。此操作不可恢复，不影响 Unity 项目 Git 主线或游戏本地档案。
