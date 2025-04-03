# BSODWTPC Anti-Stopping-Background

ClassIsland 反终止插件
安装此插件后可阻止ClassIsland 被 taskkill 或 任务管理器 结束进程。

**注意事项：**

- 此插件会禁用操作系统驱动签名校验，可能会被不良应用程式利用。
- 安装和下载此插件前，请先将此插件的目录添加进您的杀毒软件白名单，并禁用您的杀毒软件。
- 本插件涉及系统内核操作，可能会有一定风险，请自行斟酌后安装。
- 插件作者不会对使用或安装此插件带来的任何后果负责。

***

**如何停止驱动程序**
``` cmd
sc stop AntiStoppingBackground
```
* Tips： 当驱动程序被手动禁用后，也可以通过重启系统再次启动

**如何手动启动驱动程序**
``` cmd
sc start AntiStoppingBackground
```
 **如何删除驱动程序**
``` cmd
sc delete AntiStoppingBackground
```
**如何停止驱动程序自启动**
``` cmd
sc config AntiStoppingBackground start= demand
```