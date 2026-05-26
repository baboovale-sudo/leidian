using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OLAPlug;

namespace OLA
{
    public class TaskWorker
    {
        public int RowIndex { get; set; }
        public string EmulatorName { get; set; }
        public string EmulatorClass { get; set; }
        public string EmulatorBasePath { get; set; }
        public string PackageName { get; set; } = "com.xy.sh.wjsy5774";
        public List<string> TaskList { get; set; } = new List<string>();

        public WorkerState RunState { get; private set; } = WorkerState.Idle;
        public DateTime LastStartTime { get; private set; }
        public DateTime LastActionTime { get; set; } = DateTime.Now;
        public OLAPlugServer Ola => _ola!;
        public long CurrentBindHwnd { get; private set; } = 0;

        private OLAPlugServer? _ola = null;
        private CancellationTokenSource? _logicTokenSource;
        private CancellationToken _currentToken;
        private string _lastStatusMsg = "";
        private string _lastExceptionMsg = "";
        private readonly Random _rnd = new Random();

        private const int DefaultClickDelay = 1000;
        private const int DefaultClickOffset = 5;
        private const double DefaultImageSimilarity = 0.85;

        public Action<string>? LogCallback;
        public Action<int, string, string>? StatusCallback;
        public Action<int, string>? ExceptionCallback;

        public TaskWorker(int row, string name, string className, string path, string packageName = "")
        {
            RowIndex = row;
            EmulatorName = name;
            EmulatorClass = className;
            EmulatorBasePath = path;
            if (!string.IsNullOrEmpty(packageName)) PackageName = packageName;
        }

        #region 生命周期控制

        public void Start()
        {
            if (RunState == WorkerState.Running) return;

            RunState = WorkerState.Running;
            LastStartTime = DateTime.Now;
            LastActionTime = DateTime.Now;
            UpdateException("等待60秒监控介入...");

            _logicTokenSource?.Dispose();
            _logicTokenSource = new CancellationTokenSource();
            var token = _logicTokenSource.Token;

            Task.Run(async () => await RunLogicThread(token), token);
        }

        public void Stop()
        {
            RunState = WorkerState.Stopped;
            _logicTokenSource?.Cancel();
            UpdateStatus("已停止", "0");
            UpdateException("");
        }

        public void Pause()
        {
            if (RunState == WorkerState.Running)
            {
                RunState = WorkerState.Paused;
                UpdateStatus("已暂停", "");
            }
        }

        public void Resume()
        {
            if (RunState == WorkerState.Paused)
            {
                RunState = WorkerState.Resuming;
            }
        }

        public bool IsAlive()
        {
            try
            {
                return _ola != null && FindWindowWithPlugin() != 0;
            }
            catch (Exception ex)
            {
                WriteLog($"存活检测异常: {ex.Message}");
                return false;
            }
        }

        public void MarkAsMonitored()
        {
            if (_lastExceptionMsg.Contains("等待") || _lastExceptionMsg.Contains("监控"))
            {
                UpdateException("监控中");
            }
        }

        public void PerformRestart()
        {
            if (RunState == WorkerState.Stopped) return;

            Task.Run(async () =>
            {
                try
                {
                    UpdateStatus("掉线重连", "0");
                    UpdateException("检测掉线，正在重启...");
                    _logicTokenSource?.Cancel();
                    RunState = WorkerState.Idle;
                    CloseEmulator();
                    await Task.Delay(3000);
                    WriteLog("执行重启...");
                    Start();
                }
                catch (Exception ex)
                {
                    LogError($"重启异常: {ex.Message}");
                }
            });
        }

        #endregion

        #region 逻辑线程核心

        private async Task RunLogicThread(CancellationToken token)
        {
            try
            {
                _ola = new OLAPlugServer("OLA.dll");
                if (_ola.OLAObject == 0)
                {
                    LogError("插件接口创建失败");
                    return;
                }

                string imageBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                _ola.SetPath(imageBasePath);

                long parentHwnd = FindWindowWithPlugin();
                if (parentHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;

                    UpdateStatus("启动中...", "0");
                    if (!LaunchEmulator())
                    {
                        LogError("启动失败");
                        return;
                    }

                    UpdateStatus("等待画面10s", "0");
                    try
                    {
                        await Task.Delay(10000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    UpdateException("等待60秒监控介入...");
                    int retry = 0;
                    while (parentHwnd == 0 && retry < 30)
                    {
                        if (token.IsCancellationRequested) return;
                        parentHwnd = FindWindowWithPlugin();
                        if (parentHwnd != 0) break;
                        await Task.Delay(1000, token);
                        retry++;
                    }
                }

                if (parentHwnd == 0)
                {
                    LogError("启动超时");
                    return;
                }

                UpdateStatus("等待画面", parentHwnd.ToString());

                long childHwnd = 0;
                while (RunState != WorkerState.Stopped && childHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;
                    childHwnd = _ola!.GetWindow(parentHwnd, 1);
                    if (childHwnd != 0) break;
                    await Task.Delay(1000, token);
                }

                int ret = _ola!.BindWindowEx(
                    childHwnd,
                    Form1.OLAConfig.Bind_Display,
                    Form1.OLAConfig.Bind_Mouse,
                    Form1.OLAConfig.Bind_Keypad,
                    "",
                    Form1.OLAConfig.Bind_Mode
                );

                if (ret == 1)
                {
                    UpdateStatus("运行中", childHwnd.ToString());
                    WriteLog($"成功绑定窗口: 0x{childHwnd:X}");

                    try
                    {
                        await DoGameLogic(token, childHwnd);
                    }
                    catch (OperationCanceledException)
                    {
                        WriteLog("任务已取消");
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested) LogError($"逻辑异常: {ex.Message}");
                    }

                    RunState = WorkerState.Stopped;
                }
                else
                {
                    LogError($"绑定失败: {ret}");
                }
            }
            catch (OperationCanceledException)
            {
                WriteLog("逻辑线程已取消");
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) LogError($"异常: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        private async Task DoGameLogic(CancellationToken token, long currentHwnd)
        {
            _currentToken = token;
            CurrentBindHwnd = currentHwnd;

            if (TaskList == null || TaskList.Count == 0)
            {
                WriteLog("未分配任务");
                await Task.Delay(2000, token);
                return;
            }

            var gameTask = new GameTask(this);

            foreach (var taskName in TaskList)
            {
                await CheckPauseStateAsync();
                if (RunState == WorkerState.Stopped) break;

                WriteLog($"======= 开始执行: {taskName} =======");

                try
                {
                    await gameTask.Execute(taskName);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteLog($"任务[{taskName}]出错: {ex.Message}");
                }

                if (RunState == WorkerState.Stopped) break;

                WriteLog($"{taskName} 已完成");
                await Task.Delay(1000, token);
            }

            if (RunState != WorkerState.Stopped)
            {
                UpdateStatus("任务已全部完成", currentHwnd.ToString());
                WriteLog("所有任务已完成");
            }
        }

        #endregion

        #region OL_SDK 封装方法

        public async Task<bool> OL_MatchWindowsFromPath(
            int x1, int y1, int x2, int y2,
            string imgName,
            int targetX, int targetY,
            int delay = DefaultClickDelay,
            int offset = DefaultClickOffset,
            double sim = DefaultImageSimilarity)
        {
            var res = _ola!.MatchWindowsFromPath(x1, y1, x2, y2, imgName, sim, 0, 0, 1.0);
            if (res != null && res.MatchState)
            {
                WriteLog($"找图: {imgName} -> 找到 -> 执行点击");
                await OL_LeftClick(targetX, targetY, offset);
                await SmartSleep(delay);
                return true;
            }

            return false;
        }

        public async Task<bool> OL_CmpColor(
            string pointsStr,
            int targetX,
            int targetY,
            int delay = DefaultClickDelay,
            int offset = DefaultClickOffset)
        {
            if (string.IsNullOrEmpty(pointsStr)) return false;

            string[] points = pointsStr.Split('|');
            foreach (string p in points)
            {
                string[] item = p.Split(',');
                if (item.Length < 3) continue;

                int x = int.Parse(item[0]);
                int y = int.Parse(item[1]);
                string color = item[2];

                if (_ola!.CmpColor(x, y, color, color) == 0)
                {
                    return false;
                }
            }

            WriteLog("找色: 多点特征 -> 匹配 -> 执行点击");
            await OL_LeftClick(targetX, targetY, offset);
            await SmartSleep(delay);
            return true;
        }

        public async Task<bool> OL_FindStr(
            int x1, int y1, int x2, int y2,
            string text,
            string color,
            int delay = DefaultClickDelay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                WriteLog($"找字: {text} -> 找到 -> 执行点击");
                await OL_LeftClick(x, y);
                await SmartSleep(delay);
                return true;
            }

            return false;
        }

        public async Task<bool> OL_FindStr(
            int x1, int y1, int x2, int y2,
            string text,
            string color,
            int clickX,
            int clickY,
            int delay = DefaultClickDelay)
        {
            int x, y;
            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                WriteLog($"找字: {text} -> 找到 -> 点击指定位置({clickX},{clickY})");
                await OL_LeftClick(clickX, clickY);
                await SmartSleep(delay);
                return true;
            }

            return false;
        }

        public string OL_OcrFromDict(int x1, int y1, int x2, int y2, string color)
        {
            string text = _ola!.OcrFromDict(x1, y1, x2, y2, color, "无尽黑暗.txt", 0.8);
            return text ?? "";
        }

        public async Task OL_LeftClick(int x, int y, int range = DefaultClickOffset)
        {
            LastActionTime = DateTime.Now;

            int rndX = x + _rnd.Next(-range, range + 1);
            int rndY = y + _rnd.Next(-range, range + 1);

            _ola!.MoveTo(rndX, rndY);
            await Task.Delay(_rnd.Next(30, 100), _currentToken);
            _ola.LeftDown();
            await Task.Delay(_rnd.Next(50, 200), _currentToken);
            _ola.LeftUp();
        }

        public async Task<bool> SmartSleep(int ms)
        {
            try
            {
                if (await CheckLoopStateAsync()) return false;
                await Task.Delay(ms, _currentToken);
                if (await CheckLoopStateAsync()) return false;
                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        #endregion

        #region 内部辅助方法

        public void EnsureGameRunning()
        {
            if (!EmulatorName.Contains("雷电")) return;

            try
            {
                string indexStr = "0";
                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[1];

                string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                if (!File.Exists(cmdExe))
                {
                    WriteLog("未找到 ldconsole.exe");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = cmdExe,
                    Arguments = $"launchex --index {indexStr} --packagename {PackageName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                WriteLog($"正在拉起游戏: {PackageName}");
            }
            catch (Exception ex)
            {
                WriteLog($"启动指令失败: {ex.Message}");
            }
        }

        private async Task<bool> CheckLoopStateAsync()
        {
            if (_currentToken.IsCancellationRequested) return true;
            await CheckPauseStateAsync();
            return RunState == WorkerState.Stopped;
        }

        private async Task CheckPauseStateAsync()
        {
            bool wasPaused = false;

            while (RunState == WorkerState.Paused)
            {
                wasPaused = true;
                _currentToken.ThrowIfCancellationRequested();
                await Task.Delay(500, _currentToken);
            }

            if (RunState == WorkerState.Resuming)
            {
                RunState = WorkerState.Running;
            }

            if (wasPaused)
            {
                UpdateStatus("运行中", CurrentBindHwnd.ToString());
                LastActionTime = DateTime.Now;
            }

            _currentToken.ThrowIfCancellationRequested();
        }

        private long FindWindowWithPlugin()
        {
            if (_ola is null) return 0;

            long hwnd = _ola.FindWindow(EmulatorClass, EmulatorName);
            if (hwnd == 0) hwnd = _ola.FindWindow(EmulatorClass, EmulatorName + "(64)");

            if (hwnd == 0 && EmulatorName.EndsWith("-0"))
            {
                string altName = EmulatorName.Replace("-0", "");
                hwnd = _ola.FindWindow(EmulatorClass, altName);
                if (hwnd == 0) hwnd = _ola.FindWindow(EmulatorClass, altName + "(64)");
            }

            return hwnd;
        }

        private bool LaunchEmulator()
        {
            try
            {
                string cmdExe = "";
                string args = "";
                string indexStr = "0";

                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    args = $"launchex --index {indexStr} --packagename {PackageName}";
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string shellPath = Path.Combine(Directory.GetParent(EmulatorBasePath)?.FullName ?? "", "shell");
                    cmdExe = Path.Combine(shellPath, "MuMuManager.exe");
                    if (!File.Exists(cmdExe)) cmdExe = Path.Combine(EmulatorBasePath, "MuMuManager.exe");
                    args = $"player launch {indexStr}";
                }

                if (!File.Exists(cmdExe))
                {
                    WriteLog($"启动文件不存在: {cmdExe}");
                    return false;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = cmdExe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                return true;
            }
            catch (Exception ex)
            {
                WriteLog($"启动模拟器异常: {ex.Message}");
                return false;
            }
        }

        private void CloseEmulator()
        {
            try
            {
                string cmdExe = "";
                string args = "";
                string indexStr = "0";

                if (EmulatorName.Contains("-")) indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    args = $"quit --index {indexStr}";
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string[] possiblePaths =
                    {
                        Path.Combine(EmulatorBasePath, "shell", "MuMuManager.exe"),
                        Path.Combine(EmulatorBasePath, "MuMuManager.exe"),
                        Path.Combine(EmulatorBasePath, "nx_main", "MuMuManager.exe")
                    };

                    foreach (var p in possiblePaths)
                    {
                        if (File.Exists(p))
                        {
                            cmdExe = p;
                            args = $"api -v {indexStr} shutdown_player";
                            break;
                        }
                    }
                }

                if (File.Exists(cmdExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = cmdExe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    WriteLog($"关闭文件不存在: {cmdExe}");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"关闭模拟器异常: {ex.Message}");
            }
        }

        private void LogError(string msg)
        {
            WriteLog(msg);
            UpdateStatus("错误", "0");
            UpdateException(msg);
        }

        private void WriteLog(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            LogCallback?.Invoke(line);
            Debug.WriteLine(line);
        }

        private void UpdateStatus(string status, string hwnd)
        {
            if (_lastStatusMsg != status)
            {
                _lastStatusMsg = status;
                StatusCallback?.Invoke(RowIndex, status, hwnd);
            }
        }

        private void UpdateException(string msg)
        {
            if (_lastExceptionMsg != msg)
            {
                _lastExceptionMsg = msg;
                ExceptionCallback?.Invoke(RowIndex, msg);
            }
        }

        private void Cleanup()
        {
            try
            {
                if (_ola != null)
                {
                    _ola.UnBindWindow();
                    _ola.ReleaseObj();
                    _ola = null;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"释放插件异常: {ex.Message}");
            }

            if (RunState == WorkerState.Stopped)
            {
                UpdateStatus("已停止", "0");
                UpdateException("");
            }
        }

        #endregion
    }
}
