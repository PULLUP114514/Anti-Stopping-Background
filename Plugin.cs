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
                Verb = "runas" // �Թ���ԱȨ������
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
        /// ������ԱȨ��
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
            //        $"����\n" +
            //        $"�������򲻴��ڣ�������װ���̱����˳���\n\n" +
            //        $"������λ��û���ҵ� Anti-SB ��������\n" +
            //        $"{sDLLDir}\\{sDriverPath7}");
            //    throw new Exception($"Error: {sDLLDir}\\{sDriverPath7} not found");
            //}
            //��ȡ����״̬

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


            //��Ȩ
            if (!bIsRunAsAdmin())
            {
                int iNoAdminExit = new CommonDialogBuilder()
                    .SetContent(
                    $"����\n" +
                    $"����û���Թ���ԱȨ�����У�\n" +
                    $"Anti-Stopping-Background ������װ�޷�������\n" +
                    $"�����ȷ�������˳�ClassIsland\n" +
                    $"�����ȡ�����Լ�������ClassIsland")
                    .AddCancelAction()
                    .AddAction("ȷ��", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
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

            //���ڿ��ܻᱻɱ����ɱ��������Ҫ�ж�sys�Ƿ����
            if (!File.Exists($"{sDLLDir}\\{sDriverPath10}"))
            {
                ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                    $"����\n" +
                    $"�������򲻴��ڣ�������װ���̱����˳���\n\n" +
                    $"������λ��û���ҵ� Anti-SB ��������\n" +
                    $"{sDLLDir}\\{sDriverPath10}");
                throw new Exception($"Error: {sDLLDir}\\{sDriverPath10} not found");
            }

            //�ȴ�����    ����Ҫ�ж��Ƿ����Ѿ�������״̬

            if (sStartStatus== "sign wait for reboot" && File.ReadAllText("c:\\Anti-sb-status.cfg") == "sign rebooted")
            {
                File.WriteAllText(sDLLDir + "status.cfg", "sign wait for reboot");
                string sCmdResult = sRunCommand("bcdedit", " /enum");
                bool sSignstatus = bGetTestsiginStatus(sCmdResult);
                if (sSignstatus)
                {
                    //ע�����
                    string sSCCreateCmdResult = sRunCommand("sc",
                        $"create AntiStoppingBackground type= kernel " +
                        $"binPath= {sDLLDir}{sDriverPath10}");
                    if((sSCCreateCmdResult.Contains("SUCCESS") == false &&
                        sSCCreateCmdResult.Contains("�ɹ�") == false))
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to create service \n{sSCCreateCmdResult}");
                        throw new Exception($"Error: Failed to create service \n{sSCCreateCmdResult}");
                    }

                    //��������
                    string sSCStartCmdResult = sRunCommand("sc",
                        $"start AntiStoppingBackground");
                    if (sSCStartCmdResult.Contains("RUNNING") == false)
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to start service \n{sSCStartCmdResult}");
                        throw new Exception($"Error: Failed to start service \n{sSCStartCmdResult}");
                    }

                    //����־û�
                    string sSCConfigStatus = sRunCommand("sc",
                        $"config AntiStoppingBackground start= auto");
                    if ((sSCConfigStatus.Contains("SUCCESS") == false &&
                         sSCConfigStatus.Contains("�ɹ�") == false))
                    {
                        ClassIsland.Core.Controls.CommonDialog.CommonDialog.ShowInfo(
                            $"Error: Failed to start service \n{sSCConfigStatus}");

                        throw new Exception($"Error: Failed to start service \n{sSCConfigStatus}");
                    }

                    //����״̬
                    using (RegistryKey rkRun = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        rkRun.DeleteValue("StateChangeDetector");
                    }
                    File.WriteAllText(sDLLDir + "status.cfg", "installed");

                    int iRestartComputer = new CommonDialogBuilder()
                        .SetContent(
                        $"Anti-SB ��������װ�ɹ���\n" +
                        $"�����ȡ�����Թر�ClassIsland���Ⱥ���һ������" +
                        $"�����ȡ�������������������")
                        .AddCancelAction()
                        .AddAction("ȷ��", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
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
                    $"Anti Stopping Background���ڵȴ�һ��������\n" +
                    $"�����ȡ�����Թر�ClassIsland���Ⱥ���һ������" +
                    $"�����ȷ�������������������")
                    .AddCancelAction()
                    .AddAction("ȷ��", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
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


            //�״�����
            //��������ǩ��
            if (sStartStatus == "0")
            {
                int iAgreeInstallnationDialogResult = new CommonDialogBuilder()
                    .SetContent($"�������״����� Anti-Stopping-Background ClassIsland �ں˱������������\n" +
                    $"������ʼ Anti-Stopping-Background ������װ���ڰ�װǰ�����˳����еİ�ȫ�����\n" +
                    $"�˹����У����ļ���������������ɴΡ�\n" +
                    $"���¼�����ñ�������·����\n" +
                    $"{sDLLDir}\\Driver\\Win10\\Anti_Stopping_Background.sys\n" +
                    $"����ڰ�װ���ں˱������������ļ�����޷�����������������BSOD�������ȣ���\n" +
                    $"�����PE�ָ�ϵͳ��WinREϵͳ��ɾ�������ļ���\n\n" +
                    $"���棺\n" +
                    $"�˲������ò���ϵͳ����ǩ��У�飬���ܻᱻ����Ӧ�ó�ʽ���ã�" +
                    $"���ڱ�����漰ϵͳ�ں˲��������ܻ���һ�����գ����������ú�װ��\n" +
                    $"������߲����ʹ�û�װ�˲���������κκ������\n\n" +
                    $"�����ȷ������ʾ���Ѿ��Ķ���ͬ��þ��桱��������װ�˲�������ļ�������������ɴδ�Լ2~3�Σ�\n" +
                    $"����������ȡ����")
                    .AddCancelAction()
                    .AddAction("ȷ��", PackIconKind.WrenchCheckOutline, true)
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

                    //��ʼ��������ǩ��
                    sRunCommand("bcdedit", "/set testsigning on");
                    sCmdResult = sRunCommand("bcdedit", " /enum");
                    Console.WriteLine(sCmdResult);
                    bool sSignstatus = bGetTestsiginStatus(sCmdResult);
                    //vTestDialog (sSignstatus.ToString());
                    if(sSignstatus)
                    {
                        vTestDialog("��������ǩ���ɹ���");
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
                            $"�ѳɹ���������ǩ����\n" +
                            $"�����ȡ�����Թر�ClassIsland���Ⱥ���һ������"+
                            $"�����ȡ�������������������")
                            .AddCancelAction()
                            .AddAction("ȷ��", MaterialDesignThemes.Wpf.PackIconKind.Error, true)
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
                        vTestDialog("��������ǩ��ʧ�ܣ�");
                        throw new Exception($"Error: Disable driver signature failed.\n{sCmdResult}\n");
                    }
                    return;
                }
                throw new Exception("Error: Unknown Status");
            }
        }
    }
}
