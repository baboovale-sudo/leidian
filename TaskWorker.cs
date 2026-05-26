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

        public int RunState { get; private set; } = 0;
        public DateTime LastStartTime { get; private set; }

        // 防卡死：记录最后一次有效动作的时间
        public DateTime LastActionTime { get; set; } = DateTime.Now;

        public OLAPlugServer Ola => _ola!;
        public long CurrentBindHwnd { get; private set; } = 0;

        private OLAPlugServer? _ola = null;
        private CancellationTokenSource? _logicTokenSource;
        private CancellationToken _currentToken;

        private string _lastStatusMsg = "";
        private string _lastExceptionMsg = "";

        private readonly Random _rnd = new Random();

        public Action<string>? LogCallback;
        public Action<int, string, string>? StatusCallback;
        public Action<int, string>? ExceptionCallback;

        /// <summary>
        /// 创建一个模拟器任务执行器实例。
        /// </summary>
        /// <param name="row">当前任务所在表格行号。</param>
        /// <param name="name">模拟器窗口标题，例如："雷电模拟器-0"。</param>
        /// <param name="className">模拟器窗口类名。</param>
        /// <param name="path">模拟器安装目录。</param>
        /// <param name="packageName">游戏包名。为空时使用默认包名。</param>
        public TaskWorker(int row, string name, string className, string path, string packageName = "")
        {
            RowIndex = row;
            EmulatorName = name;
            EmulatorClass = className;
            EmulatorBasePath = path;

            if (!string.IsNullOrEmpty(packageName))
                PackageName = packageName;
        }

        #region 生命周期控制

        /// <summary>
        /// 启动当前任务线程。
        /// </summary>
        /// <remarks>
        /// 如果当前已经处于运行状态，则不会重复启动。
        /// 启动后会创建插件对象、启动或查找模拟器窗口、绑定窗口，并按 TaskList 顺序执行任务。
        /// </remarks>
        public void Start()
        {
            if (RunState == 1) return;

            RunState = 1;
            LastStartTime = DateTime.Now;
            LastActionTime = DateTime.Now;

            UpdateException("等待60秒监控介入...");

            _logicTokenSource = new CancellationTokenSource();
            var token = _logicTokenSource.Token;

            Task.Run(async () => await RunLogicThread(token), token);
        }

        /// <summary>
        /// 停止当前任务线程，并请求取消所有异步等待。
        /// </summary>
        public void Stop()
        {
            RunState = 4;
            _logicTokenSource?.Cancel();

            UpdateStatus("已停止", "0");
            UpdateException("");
        }

        /// <summary>
        /// 暂停当前正在运行的任务。
        /// </summary>
        public void Pause()
        {
            if (RunState == 1)
            {
                RunState = 2;
                UpdateStatus("已暂停", "");
            }
        }

        /// <summary>
        /// 恢复已经暂停的任务。
        /// </summary>
        public void Resume()
        {
            if (RunState == 2)
            {
                RunState = 3;
            }
        }

        /// <summary>
        /// 判断当前插件对象和模拟器窗口是否仍然有效。
        /// </summary>
        /// <returns>插件对象存在且能找到窗口返回 true；否则返回 false。</returns>
        public bool IsAlive()
        {
            return _ola != null && FindWindowWithPlugin() != 0;
        }

        /// <summary>
        /// 标记当前任务已进入监控状态。
        /// </summary>
        public void MarkAsMonitored()
        {
            if (_lastExceptionMsg.Contains("等待") || _lastExceptionMsg.Contains("监控"))
                UpdateException("监控中");
        }

        /// <summary>
        /// 执行模拟器重启流程。
        /// </summary>
        /// <remarks>
        /// 会先取消当前任务，关闭模拟器，等待 3 秒后重新调用 Start。
        /// </remarks>
        public void PerformRestart()
        {
            Task.Run(async () =>
            {
                UpdateStatus("掉线重连", "0");
                UpdateException("检测掉线，正在重启...");

                _logicTokenSource?.Cancel();
                RunState = 0;

                CloseEmulator();

                await Task.Delay(3000);

                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 执行重启...");

                Start();
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
                    catch
                    {
                        return;
                    }

                    UpdateException("等待60秒监控介入...");

                    int retry = 0;
                    while (parentHwnd == 0 && retry < 30)
                    {
                        if (token.IsCancellationRequested) return;

                        parentHwnd = FindWindowWithPlugin();

                        if (parentHwnd != 0)
                            break;

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

                while (RunState != 4 && childHwnd == 0)
                {
                    if (token.IsCancellationRequested) return;

                    childHwnd = _ola!.GetWindow(parentHwnd, 1);

                    if (childHwnd != 0)
                        break;

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
                    LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 成功绑定窗口: 0x{childHwnd:X}");

                    try
                    {
                        await DoGameLogic(token, childHwnd);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            LogError($"逻辑异常:{ex.Message}");
                    }

                    RunState = 4;
                }
                else
                {
                    LogError($"绑定失败:{ret}");
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    LogError($"异常:{ex.Message}");
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
                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 未分配任务");
                await Task.Delay(2000, token);
                return;
            }

            var gameTask = new GameTask(this);

            foreach (var taskName in TaskList)
            {
                await CheckPauseStateAsync();

                if (RunState == 4)
                    break;

                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ======= 开始执行: {taskName} =======");

                try
                {
                    await gameTask.Execute(taskName);
                }
                catch (Exception ex)
                {
                    LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 任务[{taskName}]出错: {ex.Message}");
                }

                if (RunState == 4)
                    break;

                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {taskName} 已完成");

                await Task.Delay(1000, token);
            }

            if (RunState != 4)
            {
                UpdateStatus("任务已全部完成", currentHwnd.ToString());
                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 所有任务已完成");
            }
        }

        #endregion

        // =======================================================================
        // OL_SDK 标准封装方法区
        // =======================================================================

        /// <summary>
        /// 在当前绑定窗口的指定区域内查找图片，找到后自动点击指定坐标。
        /// </summary>
        /// <param name="x1">查找区域左上角 X 坐标。</param>
        /// <param name="y1">查找区域左上角 Y 坐标。</param>
        /// <param name="x2">查找区域右下角 X 坐标。</param>
        /// <param name="y2">查找区域右下角 Y 坐标。</param>
        /// <param name="imgName">要查找的图片文件名，例如："开始游戏.bmp"。图片路径基于插件 SetPath 设置的目录。</param>
        /// <param name="targetX">找到图片后要点击的目标 X 坐标。</param>
        /// <param name="targetY">找到图片后要点击的目标 Y 坐标。</param>
        /// <param name="delay">点击完成后的等待时间，单位毫秒。</param>
        /// <param name="offset">点击随机偏移范围，默认 5。实际点击坐标会在 ±offset 范围内随机浮动。</param>
        /// <param name="sim">图片相似度，默认 0.85。数值越高匹配越严格。</param>
        /// <returns>找到图片并完成点击返回 true；未找到图片返回 false。</returns>
        /// <remarks>
        /// 该方法适合用于按钮识别、界面状态判断、弹窗关闭、任务入口识别等场景。
        /// 找图成功后会自动记录日志、执行随机偏移点击，并调用 SmartSleep 等待。
        /// </remarks>
        /// <example>
        /// <code>
        /// if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "开始游戏.bmp", 481, 485, 3000))
        /// {
        ///     continue;
        /// }
        /// </code>
        /// </example>
        public async Task<bool> OL_MatchWindowsFromPath(
            int x1,
            int y1,
            int x2,
            int y2,
            string imgName,
            int targetX,
            int targetY,
            int delay,
            int offset = 5,
            double sim = 0.85)
        {
            var res = _ola!.MatchWindowsFromPath(x1, y1, x2, y2, imgName, sim, 0, 0, 1.0);

            if (res != null && res.MatchState)
            {
                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 找图: {imgName} -> 找到 -> 执行点击");

                await OL_LeftClick(targetX, targetY, offset);
                await SmartSleep(delay);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断多个屏幕坐标点的颜色是否全部匹配，全部匹配后自动点击指定坐标。
        /// </summary>
        /// <param name="pointsStr">
        /// 多点颜色字符串，格式为："x,y,color|x,y,color|x,y,color"。
        /// 例如："514,116,a37b20|520,116,8d6d21|840,22,d7e1eb"。
        /// </param>
        /// <param name="targetX">颜色匹配成功后要点击的目标 X 坐标。</param>
        /// <param name="targetY">颜色匹配成功后要点击的目标 Y 坐标。</param>
        /// <param name="delay">点击完成后的等待时间，单位毫秒。</param>
        /// <param name="offset">点击随机偏移范围，默认 5。</param>
        /// <returns>所有颜色点都匹配并完成点击返回 true；任意一个点不匹配返回 false。</returns>
        /// <remarks>
        /// 该方法适合用于判断固定 UI 状态，例如按钮是否出现、弹窗是否存在、界面是否切换完成。
        /// 只要任意一个颜色点不匹配，就会直接返回 false。
        /// </remarks>
        /// <example>
        /// <code>
        /// if (await _worker.OL_CmpColor(
        ///     "514,116,a37b20|520,116,8d6d21|840,22,d7e1eb",
        ///     939,
        ///     22,
        ///     500))
        /// {
        ///     break;
        /// }
        /// </code>
        /// </example>
        public async Task<bool> OL_CmpColor(
            string pointsStr,
            int targetX,
            int targetY,
            int delay,
            int offset = 5)
        {
            if (string.IsNullOrEmpty(pointsStr))
                return false;

            string[] points = pointsStr.Split('|');

            foreach (string p in points)
            {
                string[] item = p.Split(',');

                if (item.Length < 3)
                    continue;

                int x = int.Parse(item[0]);
                int y = int.Parse(item[1]);
                string color = item[2];

                if (_ola!.CmpColor(x, y, color, color) == 0)
                {
                    return false;
                }
            }

            LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 找色: 多点特征 -> 匹配 -> 执行点击");

            await OL_LeftClick(targetX, targetY, offset);
            await SmartSleep(delay);

            return true;
        }

        /// <summary>
        /// 在指定区域内通过字库查找文字，找到后点击文字所在位置。
        /// </summary>
        /// <param name="x1">查找区域左上角 X 坐标。</param>
        /// <param name="y1">查找区域左上角 Y 坐标。</param>
        /// <param name="x2">查找区域右下角 X 坐标。</param>
        /// <param name="y2">查找区域右下角 Y 坐标。</param>
        /// <param name="text">要查找的文字内容。</param>
        /// <param name="color">文字颜色或偏色配置，例如："e3dbcb-303030"。</param>
        /// <param name="delay">点击完成后的等待时间，单位毫秒。</param>
        /// <returns>找到文字并点击成功返回 true；未找到文字返回 false。</returns>
        /// <remarks>
        /// 当前封装固定使用字库文件 "无尽黑暗.txt"，相似度为 0.8。
        /// 适合用于点击菜单文字、地图文字、任务文字、按钮文字等。
        /// </remarks>
        /// <example>
        /// <code>
        /// if (await _worker.OL_FindStr(87, 328, 117, 350, "幻术园", "e3dbcb-303030", 500))
        /// {
        ///     continue;
        /// }
        /// </code>
        /// </example>
        public async Task<bool> OL_FindStr(
            int x1,
            int y1,
            int x2,
            int y2,
            string text,
            string color,
            int delay)
        {
            int x, y;

            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 找字: {text} -> 找到 -> 执行点击");

                await OL_LeftClick(x, y);
                await SmartSleep(delay);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 在指定区域内通过字库查找文字，找到后点击指定坐标。
        /// </summary>
        /// <param name="x1">查找区域左上角 X 坐标。</param>
        /// <param name="y1">查找区域左上角 Y 坐标。</param>
        /// <param name="x2">查找区域右下角 X 坐标。</param>
        /// <param name="y2">查找区域右下角 Y 坐标。</param>
        /// <param name="text">要查找的文字内容。</param>
        /// <param name="color">文字颜色或偏色配置，例如："ada187-101010"。</param>
        /// <param name="clickX">找到文字后要点击的指定 X 坐标。</param>
        /// <param name="clickY">找到文字后要点击的指定 Y 坐标。</param>
        /// <param name="delay">点击完成后的等待时间，单位毫秒。</param>
        /// <returns>找到文字并完成指定坐标点击返回 true；未找到文字返回 false。</returns>
        /// <remarks>
        /// 该重载适合“文字只作为判断条件，但实际点击固定按钮”的场景。
        /// 例如识别到任务文本后，点击右上角关闭按钮或固定确认按钮。
        /// </remarks>
        /// <example>
        /// <code>
        /// if (await _worker.OL_FindStr(
        ///     78, 36, 91, 49,
        ///     "等级达到30",
        ///     "ada187-101010",
        ///     871,
        ///     79,
        ///     500))
        /// {
        ///     break;
        /// }
        /// </code>
        /// </example>
        public async Task<bool> OL_FindStr(
            int x1,
            int y1,
            int x2,
            int y2,
            string text,
            string color,
            int clickX,
            int clickY,
            int delay)
        {
            int x, y;

            if (_ola!.FindStr(x1, y1, x2, y2, text, color, "无尽黑暗.txt", 0.8, out x, out y) != -1)
            {
                LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 找字: {text} -> 找到 -> 点击指定位置({clickX},{clickY})");

                await OL_LeftClick(clickX, clickY);
                await SmartSleep(delay);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 使用内置字库识别指定区域内的文字。
        /// </summary>
        /// <param name="x1">识别区域左上角 X 坐标。</param>
        /// <param name="y1">识别区域左上角 Y 坐标。</param>
        /// <param name="x2">识别区域右下角 X 坐标。</param>
        /// <param name="y2">识别区域右下角 Y 坐标。</param>
        /// <param name="color">文字颜色或偏色配置，例如："e3dbcb-303030"。</param>
        /// <returns>返回识别到的字符串；如果没有识别到内容，则返回空字符串。</returns>
        /// <remarks>
        /// 当前封装固定使用字库文件 "无尽黑暗.txt"，相似度为 0.8。
        /// 适合读取等级、地图名、任务文本、按钮文字等区域内容。
        /// </remarks>
        /// <example>
        /// <code>
        /// string text = _worker.OL_OcrFromDict(80, 30, 200, 60, "e3dbcb-303030");
        ///
        /// if (text.Contains("等级"))
        /// {
        ///     // 执行等级相关逻辑
        /// }
        /// </code>
        /// </example>
        public string OL_OcrFromDict(
            int x1,
            int y1,
            int x2,
            int y2,
            string color)
        {
            string text = _ola!.OcrFromDict(x1, y1, x2, y2, color, "无尽黑暗.txt", 0.8);
            return text ?? "";
        }

        /// <summary>
        /// 在指定坐标附近执行一次左键点击，并自动加入随机偏移。
        /// </summary>
        /// <param name="x">目标 X 坐标。</param>
        /// <param name="y">目标 Y 坐标。</param>
        /// <param name="range">
        /// 随机偏移范围，默认 5。
        /// 实际点击坐标会在 x±range、y±range 范围内随机生成。
        /// </param>
        /// <returns>异步点击任务。</returns>
        /// <remarks>
        /// 每次调用都会更新 LastActionTime，用于防卡死检测。
        /// 点击过程包含：MoveTo → LeftDown → 随机短延迟 → LeftUp。
        /// </remarks>
        /// <example>
        /// <code>
        /// await _worker.OL_LeftClick(481, 485);
        ///
        /// await _worker.OL_LeftClick(810, 478, 10);
        /// </code>
        /// </example>
        public async Task OL_LeftClick(int x, int y, int range = 5)
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

        /// <summary>
        /// 智能延迟等待，在等待前后自动检查暂停、恢复、停止和取消状态。
        /// </summary>
        /// <param name="ms">等待时间，单位毫秒。</param>
        /// <returns>正常等待完成返回 true；如果任务被停止、取消或中断，返回 false。</returns>
        /// <remarks>
        /// 推荐在任务循环中使用 SmartSleep，而不是直接使用 Task.Delay。
        /// 这样可以保证暂停、停止按钮能够及时生效。
        /// </remarks>
        /// <example>
        /// <code>
        /// if (!await _worker.SmartSleep(1000))
        /// {
        ///     return;
        /// }
        /// </code>
        /// </example>
        public async Task<bool> SmartSleep(int ms)
        {
            try
            {
                if (await CheckLoopStateAsync())
                    return false;

                await Task.Delay(ms, _currentToken);

                if (await CheckLoopStateAsync())
                    return false;

                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        // =======================================================================
        // 内部辅助方法
        // =======================================================================

        #region 内部辅助方法

        /// <summary>
        /// 确保游戏进程处于启动状态。
        /// </summary>
        /// <remarks>
        /// 当前主要支持雷电模拟器。
        /// 方法会根据 EmulatorName 解析模拟器索引，
        /// 然后调用 ldconsole.exe 执行 launchex 命令，启动指定 PackageName。
        /// </remarks>
        /// <example>
        /// <code>
        /// _worker.EnsureGameRunning();
        ///
        /// if (!await _worker.SmartSleep(5000))
        /// {
        ///     return;
        /// }
        /// </code>
        /// </example>
        public void EnsureGameRunning()
        {
            if (EmulatorName.Contains("雷电"))
            {
                try
                {
                    string indexStr = "0";

                    if (EmulatorName.Contains("-"))
                        indexStr = EmulatorName.Split('-')[1];

                    string cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");

                    if (!File.Exists(cmdExe))
                    {
                        LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 未找到 ldconsole.exe");
                        return;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = cmdExe,
                        Arguments = $"launchex --index {indexStr} --packagename {PackageName}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 正在拉起游戏: {PackageName}");
                }
                catch (Exception ex)
                {
                    LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 启动指令失败: {ex.Message}");
                }
            }
        }

        private async Task<bool> CheckLoopStateAsync()
        {
            if (_currentToken.IsCancellationRequested)
                return true;

            await CheckPauseStateAsync();

            return RunState == 4;
        }

        private async Task CheckPauseStateAsync()
        {
            bool wasPaused = false;

            while (RunState == 2)
            {
                wasPaused = true;

                _currentToken.ThrowIfCancellationRequested();

                await Task.Delay(500, _currentToken);
            }

            if (RunState == 3)
                RunState = 1;

            if (wasPaused)
            {
                UpdateStatus("运行中", CurrentBindHwnd.ToString());
                LastActionTime = DateTime.Now;
            }

            _currentToken.ThrowIfCancellationRequested();
        }

        private long FindWindowWithPlugin()
        {
            if (_ola is null)
                return 0;

            long hwnd = _ola.FindWindow(EmulatorClass, EmulatorName);

            if (hwnd == 0)
                hwnd = _ola.FindWindow(EmulatorClass, EmulatorName + "(64)");

            if (hwnd == 0 && EmulatorName.EndsWith("-0"))
            {
                string altName = EmulatorName.Replace("-0", "");

                hwnd = _ola.FindWindow(EmulatorClass, altName);

                if (hwnd == 0)
                    hwnd = _ola.FindWindow(EmulatorClass, altName + "(64)");
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

                if (EmulatorName.Contains("-"))
                    indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    args = $"launchex --index {indexStr} --packagename {PackageName}";
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    string shellPath = Path.Combine(Directory.GetParent(EmulatorBasePath)?.FullName ?? "", "shell");

                    cmdExe = Path.Combine(shellPath, "MuMuManager.exe");

                    if (!File.Exists(cmdExe))
                        cmdExe = Path.Combine(EmulatorBasePath, "MuMuManager.exe");

                    args = $"player launch {indexStr}";
                }

                if (!File.Exists(cmdExe))
                    return false;

                Process.Start(new ProcessStartInfo
                {
                    FileName = cmdExe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                return true;
            }
            catch
            {
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

                if (EmulatorName.Contains("-"))
                    indexStr = EmulatorName.Split('-')[^1];

                if (EmulatorName.Contains("雷电"))
                {
                    cmdExe = Path.Combine(EmulatorBasePath, "ldconsole.exe");
                    args = $"quit --index {indexStr}";
                }
                else if (EmulatorName.Contains("MuMu"))
                {
                    // MuMu 关闭逻辑可按需补充
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
            }
            catch
            {
            }
        }

        private void LogError(string msg)
        {
            LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");

            UpdateStatus("错误", "0");
            UpdateException(msg);
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
            if (_ola != null)
            {
                _ola.UnBindWindow();
                _ola.ReleaseObj();
                _ola = null;
            }

            if (RunState == 4)
            {
                UpdateStatus("已停止", "0");
                UpdateException("");
            }
        }

        #endregion
    }
}
