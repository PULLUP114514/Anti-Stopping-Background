using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ControlzEx.Standard;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Threading;

namespace AntiStoppingBackground
{
    [PluginEntrance]
    public class Plugin : PluginBase
    {
        private DispatcherTimer dtTimer;
        private const string sDriverPath10 = "Driver\\Win10\\Anti_Stopping_BackGround.sys";
        //private const string sDriverPath7 = "Driver\\Win7\\Anti_Stopping_BackGround.sys";
        private keepworking kKeepworkingWindow = new keepworking();
        private void Timer_Elapsed(object sender, EventArgs e)
        {
            if (kKeepworkingWindow == null || !kKeepworkingWindow.IsLoaded)
            {
                kKeepworkingWindow = new keepworking(); 
                kKeepworkingWindow.Show();
            }
        }

        private void vTestDialog(string sMessage)
        {
#if DEBUG
            ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(sMessage);
#endif
        }

        static string sRunCommand(string sCommand, string sArguments)
        {
            ProcessStartInfo psiCommandProcess = new ProcessStartInfo
            {
                FileName = sCommand,
                Arguments = sArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                Verb = "runas" // 以管理员权限运行
            };

            try
            {
                using (Process process = Process.Start(psiCommandProcess))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return string.IsNullOrEmpty(error) ? output : error;
                }
            }
            catch (Exception ex)
            {
                ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                    "Error: Failed to run command\n" + ex.Message);
                throw new Exception("Error: Failed to run command\n" + ex.Message);
            }
        }

        public static int iGetWindowsVersion()
        {
            Version verWinVersion = Environment.OSVersion.Version;
            if(verWinVersion.Major == 10)
            {
                return 10;
            }
            if(verWinVersion.Major == 6 && verWinVersion.Major == 1)
            {
                return 7;
            }
            return -1;
        }

        /// <summary>
        /// 检测管理员权限
        /// </summary>
        /// <returns></returns>
        public static bool bIsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool bGetTestsiginStatus(string sBcdeditResult)
        {
            string sPattern = @"(?<=testsigning\s+)\S+";
            Match sMatch = Regex.Match(sBcdeditResult, sPattern);
            return sMatch.Success;
        }

        public override void Initialize(HostBuilderContext context, IServiceCollection services)
        {
            Console.WriteLine("ANTI-SB-INSTALLER: Start Initalize Anti Stopping Background Driver");
            string sDLLDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine($"ANTI-SB-INSTALLER: DLL PATH: {sDLLDir}");
            //vTestDialog(sDLLDir);
            if (string.IsNullOrEmpty(sDLLDir))
            {
                throw new Exception("Error: Cannot get dll path");
            }
            sDLLDir += "\\";


            //if (File.Exists($"{sDLLDir}\\{sDriverPath7}"))
            //{
            //    ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
            //        $"错误！\n" +
            //        $"驱动程序不存在，驱动安装进程被迫退出。\n\n" +
            //        $"在以下位置没有找到 Anti-SB 驱动程序：\n" +
            //        $"{sDLLDir}\\{sDriverPath7}");
            //    throw new Exception($"Error: {sDLLDir}\\{sDriverPath7} not found");
            //}
            //获取启动状态

            string sStartStatus = "0";
            try
            {
                sStartStatus = File.ReadAllText(sDLLDir + "status.cfg");
            }
            catch
            {
                //ignore
            }
            Console.WriteLine($"ANTI-SB-INSTALLER: sStartStatus: {sStartStatus}");
            if (sStartStatus== "Unsupported Windows Version")
            {
                throw new Exception("Error: Unsupported Windows Version");
            }

            if(sStartStatus == "installed")
            {
                //ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                //    $"installed");
                dtTimer = new DispatcherTimer();
                dtTimer.Interval = TimeSpan.FromSeconds(0.1);
                dtTimer.Tick += Timer_Elapsed;
                dtTimer.Start();
                return;
            }


            //鉴权
            if (!bIsRunAsAdmin())
            {
                int iNoAdminExit = new CommonDialogBuilder()
                    .SetContent(
                    $"错误！\n" +
                    $"程序没有以管理员权限运行！\n" +
                    $"Anti-Stopping-Background 驱动安装无法继续！\n" +
                    $"点击“确定”来退出ClassIsland\n" +
                    $"点击“取消”以继续启动ClassIsland")
                    .AddCancelAction()
                    .AddAction("确定", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
                    .ShowDialog();
                if (iNoAdminExit == 0)
                {
                    throw new Exception($"Error: No Administrator permission");
                }
                if (iNoAdminExit == 1)
                {
                    Process.GetCurrentProcess().Kill();
                }
                return;
            }

            //由于可能会被杀软误杀，所以需要判断sys是否存在
            if (!File.Exists($"{sDLLDir}\\{sDriverPath10}"))
            {
                ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                    $"错误！\n" +
                    $"驱动程序不存在，驱动安装进程被迫退出。\n\n" +
                    $"在以下位置没有找到 Anti-SB 驱动程序：\n" +
                    $"{sDLLDir}\\{sDriverPath10}");
                throw new Exception($"Error: {sDLLDir}\\{sDriverPath10} not found");
            }

            //等待重启    还需要判断是否是已经重启的状态

            if (sStartStatus== "sign wait for reboot" && File.ReadAllText("c:\\Anti-sb-status.cfg") == "sign rebooted")
            {
                File.WriteAllText(sDLLDir + "status.cfg", "sign wait for reboot");
                string sCmdResult = sRunCommand("bcdedit", " /enum");
                bool sSignstatus = bGetTestsiginStatus(sCmdResult);
                if (sSignstatus)
                {
                    //注册服务
                    string sSCCreateCmdResult = sRunCommand("sc",
                        $"create AntiStoppingBackground type= kernel " +
                        $"binPath= {sDLLDir}{sDriverPath10}");
                    if((sSCCreateCmdResult.Contains("SUCCESS") == false &&
                        sSCCreateCmdResult.Contains("成功") == false))
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to create service \n{sSCCreateCmdResult}");
                        throw new Exception($"Error: Failed to create service \n{sSCCreateCmdResult}");
                    }

                    //启动服务
                    string sSCStartCmdResult = sRunCommand("sc",
                        $"start AntiStoppingBackground");
                    if (sSCStartCmdResult.Contains("RUNNING") == false)
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to start service \n{sSCStartCmdResult}");
                        throw new Exception($"Error: Failed to start service \n{sSCStartCmdResult}");
                    }

                    //服务持久化
                    string sSCConfigStatus = sRunCommand("sc",
                        $"config AntiStoppingBackground start= auto");
                    if ((sSCConfigStatus.Contains("SUCCESS") == false &&
                         sSCConfigStatus.Contains("成功") == false))
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to start service \n{sSCConfigStatus}");

                        throw new Exception($"Error: Failed to start service \n{sSCConfigStatus}");
                    }

                    //更新状态
                    using (RegistryKey rkRun = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        rkRun.DeleteValue("StateChangeDetector");
                    }
                    File.WriteAllText(sDLLDir + "status.cfg", "installed");

                    int iRestartComputer = new CommonDialogBuilder()
                        .SetContent(
                        $"Anti-SB 驱动程序安装成功！\n" +
                        $"点击“取消”以关闭ClassIsland并等候下一次重启" +
                        $"点击“取消”以重新启动计算机")
                        .AddCancelAction()
                        .AddAction("确定", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
                        .ShowDialog();
                    if (iRestartComputer == 1)
                    {
#if DEBUG
#else
                        Process.Start("shutdown", "/r /t 0");
#endif
                        return;
                    }
                    else
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                }
                return;
            }

            if (sStartStatus == "sign wait for reboot")
            {
                int iRestartComputer = new CommonDialogBuilder()
                    .SetContent(
                    $"Anti Stopping Background正在等待一次重启！\n" +
                    $"点击“取消”以关闭ClassIsland并等候下一次重启" +
                    $"点击“确定”以重新启动计算机")
                    .AddCancelAction()
                    .AddAction("确定", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
                    .ShowDialog();
                if (iRestartComputer == 0)
                {
                    Process.GetCurrentProcess().Kill();
                }
                if (iRestartComputer == 1)
                {
#if DEBUG
#else
                    Process.Start("shutdown", "/r /t 0");
#endif
                }
                return;
            }


            //首次启动
            //禁用驱动签名
            if (sStartStatus == "0")
            {
                int iAgreeInstallnationDialogResult = new CommonDialogBuilder()
                    .SetContent($"这是您首次运行 Anti-Stopping-Background ClassIsland 内核保护驱动插件！\n" +
                    $"即将开始 Anti-Stopping-Background 驱动安装，在安装前，请退出所有的安全软件。\n" +
                    $"此过程中，您的计算机将会重启若干次。\n" +
                    $"请记录并永久保存以下路径：\n" +
                    $"{sDLLDir}\\Driver\\Win10\\Anti_Stopping_Background.sys\n" +
                    $"如果在安装此内核保护驱动后，您的计算机无法启动或发生崩溃（如BSOD、黑屏等），\n" +
                    $"请进入PE恢复系统或WinRE系统来删除上述文件。\n\n" +
                    $"警告：\n" +
                    $"此插件会禁用操作系统驱动签名校验，可能会被不良应用程式利用；" +
                    $"由于本插件涉及系统内核操作，可能会有一定风险，请自行斟酌后安装；\n" +
                    $"插件作者不会对使用或安装此插件带来的任何后果负责。\n\n" +
                    $"点击“确定”表示“已经阅读且同意该警告”并继续安装此插件（您的计算机将重启若干次大约2~3次）\n" +
                    $"否则请点击“取消”")
                    .AddCancelAction()
                    .AddAction("确定", PackIconKind.WrenchCheckOutline, true)
                    .ShowDialog();
                if(iAgreeInstallnationDialogResult == 0)
                {
                    throw new Exception("Error: User has canceled the installation process.");
                }
                if(iAgreeInstallnationDialogResult == 1)
                {
                    string sCmdResult = "";
                    int iWinVersion = iGetWindowsVersion();
                    if( iWinVersion == -1 )
                    {
                        File.WriteAllText(sDLLDir + "status.cfg", "Unsupported Windows Version");
                        throw new Exception("Error: Unsupported Windows Version");
                    }

                    //开始禁用驱动签名
                    sRunCommand("bcdedit", "/set testsigning on");
                    sCmdResult = sRunCommand("bcdedit", " /enum");
                    Console.WriteLine(sCmdResult);
                    bool sSignstatus = bGetTestsiginStatus(sCmdResult);
                    //vTestDialog (sSignstatus.ToString());
                    if(sSignstatus)
                    {
                        vTestDialog("禁用驱动签名成功！");
                        File.WriteAllText(sDLLDir + "\\status.cfg", "sign wait for reboot");
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                            @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null)
                            {
                                key.SetValue(
                                    "StateChangeDetector", 
                                    $"{sDLLDir}StateChangeDetector.exe");
                            }
                            else
                            {
                                return;
                            }
                        }
                        File.WriteAllText($"c:\\ANTI-SB.cfg",$"{sDLLDir}status.cfg");
                        int iRestartComputer = new CommonDialogBuilder()
                            .SetContent(
                            $"已成功禁用驱动签名！\n" +
                            $"点击“取消”以关闭ClassIsland并等候下一次重启"+
                            $"点击“取消”以重新启动计算机")
                            .AddCancelAction()
                            .AddAction("确定", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
                            .ShowDialog();
                        if (iRestartComputer == 1)
                        {
#if DEBUG
#else
                            Process.Start("shutdown", "/r /t 0");
#endif
                            return;
                        }
                        else
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    else
                    {
                        vTestDialog("禁用驱动签名失败！");
                        throw new Exception($"Error: Disable driver signature failed.\n{sCmdResult}\n");
                    }
                    return;
                }
                throw new Exception("Error: Unknown Status");
            }
        }
    }
}
