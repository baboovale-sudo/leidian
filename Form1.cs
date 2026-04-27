using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using OLAPlug;

namespace OLA
{
    public partial class Form1 : Form
    {
        // ==========================================
        // 🌍 全局配置中心
        // ==========================================
        public static class OLAConfig
        {
            public const string UserCode = "d841c28403974a56b31a74856542b6b7";
            public const string SoftCode = "3392920261284bcca265a3f451ed9709";
            public const string Key = "OLA";

            public const string Bind_Display = "gdi";
            public const string Bind_Mouse = "windows";
            public const string Bind_Keypad = "windows";
            public const int Bind_Mode = 0;
        }

        // ==========================================
        // 🔥 API 定义
        // ==========================================
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint GA_ROOT = 2;

        // 任务管理器
        Dictionary<int, TaskWorker> workers = new Dictionary<int, TaskWorker>();
        private CancellationTokenSource? _monitorToken;
        private bool _isMonitorActive = false;
        private bool _isRowAlreadySelected = false;
        private bool _isScriptRunning = false;
        private DateTime _scriptStartTime;
        private const string INI_SECTION = "Settings";

        public Form1()
        {
            InitializeComponent();
            InitializeSettings();
            ApplyDesignMdStyle();
            this.moniqi_liebiao.ClearSelection();

            // 注册插件
            Register_OLA();

            this.moniqi_liebiao.ClearSelection();
            this.moniqi_liebiao.CurrentCell = null;

            this.renwu_liebiao.DoubleClick += new EventHandler(this.renwu_liebiao_DoubleClick);
            this.yixuan_renwu.DoubleClick += new EventHandler(this.yixuan_renwu_DoubleClick);
        }

        private void InitializeSettings()
        {
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);

            // 初始化模拟器下拉框 (只保留雷电和MuMu)
            if (this.moniqi_xuanze.Items.Count == 0)
            {
                this.moniqi_xuanze.Items.Add("雷电模拟器");
                this.moniqi_xuanze.Items.Add("MuMu模拟器");
            }

            this.moniqi_xuanze.SelectedIndexChanged += new EventHandler(this.moniqi_xuanze_SelectedIndexChanged);
            this.moniqi_xuanze.DropDown += new EventHandler(this.Control_Intercept_DropDown);
            this.moniqi_xuanze.SelectedIndex = 0;

            this.duokai_shuliang.Text = "2";
            this.duokai_shuliang.Enter += new EventHandler(this.Control_Intercept_Enter);
            this.duokai_shuliang.KeyPress += new KeyPressEventHandler(this.Control_Intercept_KeyPress);

            if (this.pailie_fangshi.Items.Count == 0)
            {
                this.pailie_fangshi.Items.Add("平铺排序");
                this.pailie_fangshi.Items.Add("隐藏窗口");
            }
            this.pailie_fangshi.SelectedIndex = 0;

            this.lujing_shuru.Enter += new EventHandler(this.Control_Intercept_Enter);
            this.lujing_shuru.KeyPress += new KeyPressEventHandler(this.Control_Intercept_KeyPress);

            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                cb.DropDown += new EventHandler(this.Control_Intercept_DropDown);
            }

            this.moniqi_liebiao.CellMouseDown += new DataGridViewCellMouseEventHandler(this.moniqi_liebiao_CellMouseDown);
            this.moniqi_liebiao.CellMouseUp += new DataGridViewCellMouseEventHandler(this.moniqi_liebiao_CellMouseUp);
            this.tingzhi_xuanzhong.Click += new EventHandler(this.tingzhi_xuanzhong_Click);
            this.zanting_suoyou.Click += new EventHandler(this.zanting_suoyou_Click);
            this.huifu_suoyou.Click += new EventHandler(this.huifu_suoyou_Click);

            this.queding_shezhi.Click += new EventHandler(this.queding_shezhi_Click);

            this.Load += new EventHandler(this.Form1_Load);

            if (this.renwu_liebiao.Items.Count == 0)
            {
                this.renwu_liebiao.Items.Add("主线任务");
                this.renwu_liebiao.Items.Add("每日活跃");
                this.renwu_liebiao.Items.Add("自动签到");
                this.renwu_liebiao.Items.Add("支线任务");
                this.renwu_liebiao.Items.Add("挂机任务");
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            _scriptStartTime = DateTime.Now;
            timer_runtime.Start();

            LoadSettings();
            if (Directory.Exists(this.lujing_shuru.Text))
            {
                shuaxin_liebiao_Click(null, EventArgs.Empty);
            }
            this.moniqi_liebiao.ClearSelection();
            this.moniqi_liebiao.CurrentCell = null;
        }

        private void timer_runtime_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now - _scriptStartTime;
            yunxingshijian.Text = string.Format("脚本运行时间: {0:D2}:{1:D2}:{2:D2}",
                (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        private void quanbu_qidong_Click(object? sender, EventArgs e)
        {
            if (!timer_runtime.Enabled)
            {
                _scriptStartTime = DateTime.Now;
                timer_runtime.Start();
            }

            if (!int.TryParse(this.duokai_shuliang.Text, out int maxCount))
            {
                MessageBox.Show("多开数量必须是数字！");
                return;
            }

            string basePath = this.lujing_shuru.Text.Trim();
            if (!Directory.Exists(basePath))
            {
                MessageBox.Show("模拟器路径不存在！");
                return;
            }

            if (this.moniqi_liebiao.Rows.Count == 0)
            {
                MessageBox.Show("列表为空！");
                return;
            }

            _isScriptRunning = true;

            List<string> selectedTasks = new List<string>();
            foreach (var item in this.yixuan_renwu.Items)
            {
                selectedTasks.Add(item.ToString() ?? "");
            }

            int runningCount = 0;
            lock (workers)
            {
                foreach (var kvp in workers)
                {
                    if (kvp.Value.RunState != 4)
                    {
                        runningCount++;
                    }
                }
            }

            if (runningCount >= maxCount)
            {
                MessageBox.Show($"当前运行数量({runningCount})已达到设定上限({maxCount})，无法继续启动！");
                return;
            }

            int currentCount = runningCount;
            var rowsToStart = new List<(int index, string name, string className, string basePath)>();

            // 先添加选中的
            for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
            {
                if (currentCount >= maxCount) break;
                if (this.moniqi_liebiao.Rows[i].Selected)
                {
                    if (IsRowValidForStart(i))
                    {
                        AddToStartList(rowsToStart, i, basePath);
                        currentCount++;
                    }
                }
            }

            // 再添加剩下的
            if (currentCount < maxCount)
            {
                for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
                {
                    if (currentCount >= maxCount) break;
                    if (!this.moniqi_liebiao.Rows[i].Selected)
                    {
                        if (IsRowValidForStart(i))
                        {
                            AddToStartList(rowsToStart, i, basePath);
                            currentCount++;
                        }
                    }
                }
            }

            if (rowsToStart.Count == 0) return;

            Task.Run(() =>
            {
                foreach (var item in rowsToStart)
                {
                    TaskWorker worker;
                    lock (workers)
                    {
                        if (workers.ContainsKey(item.index))
                        {
                            worker = workers[item.index];
                        }
                        else
                        {
                            worker = new TaskWorker(item.index, item.name, item.className, item.basePath)
                            {
                                StatusCallback = (r, status, hwnd) => UpdateRowStatus(r, status, hwnd),
                                ExceptionCallback = (r, msg) => UpdateRowException(r, msg),
                                LogCallback = (msg) =>
                                {
                                    string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    System.Diagnostics.Debug.WriteLine($"[{timeStr}] [{item.name}] {msg}");
                                }
                            };
                            workers[item.index] = worker;
                        }

                        worker.TaskList = new List<string>(selectedTasks);
                    }

                    // ... 找到大约 234 行 ...
                    long checkHwnd = 0;
                    try
                    {
                        // ✅ 修改这里，加上 "OLA.dll"
                        OLAPlugServer tempOla = new OLAPlugServer("OLA.dll");

                        if (tempOla.OLAObject != 0)
                        {
                            checkHwnd = tempOla.FindWindow(item.className, item.name);
                            tempOla.ReleaseObj(); // 探测完记得释放，新版SDK非常看重这个
                        }
                    }
                    catch { checkHwnd = 0; }

                    worker.Start();
                    this.Invoke(new Action(() => { StartGlobalMonitor(); }));

                    if (checkHwnd > 0) System.Threading.Thread.Sleep(500);
                    else System.Threading.Thread.Sleep(3000);
                }
            });
        }

        private void quanbu_tingzhi_Click(object? sender, EventArgs e)
        {
            timer_runtime.Stop();
            StopGlobalMonitor();
            lock (workers)
            {
                foreach (var kvp in workers)
                {
                    kvp.Value.Stop();
                }
                workers.Clear();
            }
            _isScriptRunning = false;
        }

        private void queding_shezhi_Click(object? sender, EventArgs e)
        {
            SaveSettings();

            string selectedMode = this.pailie_fangshi.Text;
            string emuType = this.moniqi_xuanze.Text;
            string basePath = this.lujing_shuru.Text.Trim();

            if (selectedMode.Contains("平铺"))
            {
                // ==========================
                // ⚡ 雷电模拟器
                // ==========================
                if (emuType.Contains("雷电"))
                {
                    if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                    {
                        MessageBox.Show("模拟器路径无效，无法执行排序！");
                        return;
                    }
                    string cmdExe = Path.Combine(basePath, "ldconsole.exe");
                    if (File.Exists(cmdExe))
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = cmdExe;
                            psi.Arguments = "sortWnd";
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("执行排序出错: " + ex.Message);
                        }
                    }
                }
                // ==========================
                // 🟡 MuMu模拟器
                // ==========================
                else if (emuType.Contains("MuMu"))
                {
                    // 智能查找 MuMuManager.exe
                    string[] possiblePaths = new string[]
                    {
                        Path.Combine(basePath, "shell", "MuMuManager.exe"),
                        Path.Combine(basePath, "MuMuManager.exe"),
                        Path.Combine(basePath, "nx_main", "MuMuManager.exe")
                    };

                    string managerPath = "";
                    foreach (var p in possiblePaths)
                    {
                        if (File.Exists(p))
                        {
                            managerPath = p;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(managerPath))
                    {
                        MessageBox.Show("未找到 MuMuManager.exe，无法执行排序。");
                        return;
                    }

                    // MuMu 官方排序命令
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = managerPath, Arguments = "sort", UseShellExecute = false, CreateNoWindow = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("MuMu排序出错: " + ex.Message);
                    }
                }
            }
            else if (selectedMode.Contains("隐藏"))
            {
                int count = 0;
                for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
                {
                    var cellVal = this.moniqi_liebiao.Rows[i].Cells["jubing"].Value;
                    string hwndStr = cellVal != null ? cellVal.ToString() : "0";

                    if (long.TryParse(hwndStr, out long childHwndVal) && childHwndVal > 0)
                    {
                        IntPtr childHwnd = (IntPtr)childHwndVal;
                        IntPtr parentHwnd = GetAncestor(childHwnd, GA_ROOT);

                        if (parentHwnd != IntPtr.Zero)
                        {
                            SetWindowPos(parentHwnd, IntPtr.Zero, -3000, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                            count++;
                        }
                    }
                }
                if (count == 0) MessageBox.Show("未检测到运行中的窗口，无法隐藏。");
            }
        }

        private void LoadSettings()
        {
            string defaultEmuName = this.moniqi_xuanze.Items.Count > 0 ? (this.moniqi_xuanze.Items[0]?.ToString() ?? string.Empty) : string.Empty;
            string emuName = IniHelper.Read(INI_SECTION, "EmulatorType", defaultEmuName);
            int idx = this.moniqi_xuanze.Items.IndexOf(emuName);
            if (idx != -1) this.moniqi_xuanze.SelectedIndex = idx;
            else if (this.moniqi_xuanze.Items.Count > 0) this.moniqi_xuanze.SelectedIndex = 0;

            string maxCount = IniHelper.Read(INI_SECTION, "MaxCount", "2");
            this.duokai_shuliang.Text = maxCount;

            string savedPath = IniHelper.Read(INI_SECTION, "BasePath", "");
            if (string.IsNullOrEmpty(savedPath))
            {
                savedPath = Auto_Find_Path(this.moniqi_xuanze.Text);
            }
            this.lujing_shuru.Text = savedPath;

            string defaultSortType = this.pailie_fangshi.Items.Count > 0 ? (this.pailie_fangshi.Items[0]?.ToString() ?? string.Empty) : string.Empty;
            string sortType = IniHelper.Read(INI_SECTION, "SortType", defaultSortType);
            idx = this.pailie_fangshi.Items.IndexOf(sortType);
            if (idx != -1) this.pailie_fangshi.SelectedIndex = idx;
            else if (this.pailie_fangshi.Items.Count > 0) this.pailie_fangshi.SelectedIndex = 0;

            this.zidong_denglu.Checked = IniHelper.Read(INI_SECTION, "AutoLogin", "True") == "True";
            this.shifou_zhiding.Checked = IniHelper.Read(INI_SECTION, "PinToTop", "False") == "True";
            this.shifou_huanhao.Checked = IniHelper.Read(INI_SECTION, "SwitchAccount", "False") == "True";
            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                string defaultServer = cb.Items.Count > 0 ? (cb.Items[0]?.ToString() ?? string.Empty) : string.Empty;
                string serverName = IniHelper.Read(INI_SECTION, "ServerRegion", defaultServer);
                cb.SelectedIndex = cb.Items.IndexOf(serverName);
                if (cb.SelectedIndex == -1 && cb.Items.Count > 0) cb.SelectedIndex = 0;
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isScriptRunning)
            {
                if (MessageBox.Show("有脚本正在运行，确定要关闭程序吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                quanbu_tingzhi_Click(null, EventArgs.Empty);
            }
            SaveSettings();
        }

        private void SaveSettings()
        {
            IniHelper.Write(INI_SECTION, "EmulatorType", this.moniqi_xuanze.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "MaxCount", this.duokai_shuliang.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "BasePath", this.lujing_shuru.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "SortType", this.pailie_fangshi.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "AutoLogin", this.zidong_denglu.Checked.ToString());
            IniHelper.Write(INI_SECTION, "PinToTop", this.shifou_zhiding.Checked.ToString());
            IniHelper.Write(INI_SECTION, "SwitchAccount", this.shifou_huanhao.Checked.ToString());
            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                IniHelper.Write(INI_SECTION, "ServerRegion", cb.Text ?? string.Empty);
            }
        }

        private void StartGlobalMonitor()
        {
            if (_isMonitorActive) return;
            _isMonitorActive = true;
            _monitorToken = new CancellationTokenSource();
            var token = _monitorToken.Token;
            Task.Run(() =>
            {
                Thread.Sleep(500);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        TaskWorker[] currentWorkers;
                        lock (workers) { currentWorkers = new List<TaskWorker>(workers.Values).ToArray(); }

                        int activeCount = 0;

                        if (currentWorkers.Length > 0)
                        {
                            foreach (var worker in currentWorkers)
                            {
                                if (worker.RunState == 1 || worker.RunState == 2)
                                {
                                    activeCount++;
                                    double seconds = (DateTime.Now - worker.LastStartTime).TotalSeconds;

                                    if (seconds < 60)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        worker.MarkAsMonitored();
                                    }

                                    if (!worker.IsAlive())
                                    {
                                        worker.PerformRestart();
                                    }
                                }
                            }
                        }

                        if (activeCount == 0)
                        {
                            _isScriptRunning = false;
                            break;
                        }
                    }
                    catch { }

                    try { Task.Delay(10000, token).Wait(); } catch { break; }
                }
                _isMonitorActive = false;
            }, token);
        }

        private void StopGlobalMonitor()
        {
            _monitorToken?.Cancel();
            _isMonitorActive = false;
        }

        private void renwu_liebiao_DoubleClick(object? sender, EventArgs e)
        {
            if (_isScriptRunning) return;
            if (this.renwu_liebiao.SelectedItem != null)
            {
                object item = this.renwu_liebiao.SelectedItem;
                this.yixuan_renwu.Items.Add(item);
                this.renwu_liebiao.Items.Remove(item);
            }
        }

        private void yixuan_renwu_DoubleClick(object? sender, EventArgs e)
        {
            if (_isScriptRunning) return;
            if (this.yixuan_renwu.SelectedItem != null)
            {
                object item = this.yixuan_renwu.SelectedItem;
                this.renwu_liebiao.Items.Add(item);
                this.yixuan_renwu.Items.Remove(item);
            }
        }

        private bool IsRowValidForStart(int i)
        {
            if (workers.ContainsKey(i) && workers[i].RunState != 4) return false;
            string? name = this.moniqi_liebiao.Rows[i].Cells["moniqi"].Value?.ToString();
            if (string.IsNullOrEmpty(name)) return false;
            return true;
        }

        // ==========================================
        // 🔥 核心修改：设置 MuMu 类名
        // ==========================================
        private void AddToStartList(List<(int, string, string, string)> list, int i, string basePath)
        {
            string name = this.moniqi_liebiao.Rows[i].Cells["moniqi"].Value?.ToString()!;
            string className = "";

            if (name.Contains("雷电"))
            {
                className = "LDPlayerMainFrame";
            }
            else if (name.Contains("MuMu"))
            {
                // 使用你指定的 MuMu 类名
                className = "Qt5156QWindowIcon";
            }

            list.Add((i, name, className, basePath));
        }

        private void tingzhi_xuanzhong_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选中要停止的模拟器！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index))
                {
                    workers[index].Stop();
                }
            }
        }

        private void zanting_suoyou_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0) { MessageBox.Show("请先选中！"); return; }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index)) workers[index].Pause();
            }
        }

        private void huifu_suoyou_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0) { MessageBox.Show("请先选中！"); return; }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index)) workers[index].Resume();
            }
        }

        // ==========================================
        // 🔥 核心修改：关闭所有 (MuMu 循环关闭)
        // ==========================================
        private void guanbi_suoyou_Click(object? sender, EventArgs e)
        {
            quanbu_tingzhi_Click(null, EventArgs.Empty);

            string basePath = this.lujing_shuru.Text.Trim();
            if (string.IsNullOrEmpty(basePath)) return;

            string emuType = this.moniqi_xuanze.Text;

            // 雷电逻辑
            if (emuType.Contains("雷电"))
            {
                string cmdExe = Path.Combine(basePath, "ldconsole.exe");
                if (File.Exists(cmdExe))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = "quitall", UseShellExecute = false, CreateNoWindow = true });
                        MessageBox.Show("已发送雷电关闭指令");
                    }
                    catch (Exception ex) { MessageBox.Show("错误: " + ex.Message); }
                }
            }
            // MuMu 逻辑 (循环关闭)
            else if (emuType.Contains("MuMu"))
            {
                string[] possiblePaths = new string[]
                {
                    Path.Combine(basePath, "shell", "MuMuManager.exe"),
                    Path.Combine(basePath, "MuMuManager.exe"),
                    Path.Combine(basePath, "nx_main", "MuMuManager.exe")
                };

                string managerPath = "";
                foreach (var p in possiblePaths)
                {
                    if (File.Exists(p))
                    {
                        managerPath = p;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(managerPath))
                {
                    MessageBox.Show($"未找到 MuMuManager.exe\n请检查路径: {basePath}");
                    return;
                }

                int count = 0;
                foreach (DataGridViewRow row in this.moniqi_liebiao.Rows)
                {
                    if (row.IsNewRow) continue;
                    string name = row.Cells["moniqi"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    string indexStr = "0";
                    if (name.Contains("-")) indexStr = name.Split('-')[^1];

                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = managerPath, Arguments = $"api -v {indexStr} shutdown_player", UseShellExecute = false, CreateNoWindow = true });
                        count++;
                    }
                    catch { }

                    Thread.Sleep(20);
                }
                MessageBox.Show($"已发送关闭指令给 {count} 个模拟器 (MuMu)");
            }
        }

        public void UpdateRowStatus(int rowIndex, string status, string hwnd)
        {
            if (this.moniqi_liebiao.InvokeRequired)
            {
                this.moniqi_liebiao.Invoke(new Action<int, string, string>(UpdateRowStatus), rowIndex, status, hwnd);
                return;
            }
            if (rowIndex >= 0 && rowIndex < this.moniqi_liebiao.Rows.Count)
            {
                this.moniqi_liebiao.Rows[rowIndex].Cells["zhuangtai"].Value = status;
                this.moniqi_liebiao.Rows[rowIndex].Cells["jubing"].Value = hwnd;
            }
        }

        public void UpdateRowException(int rowIndex, string msg)
        {
            if (this.moniqi_liebiao.InvokeRequired)
            {
                this.moniqi_liebiao.Invoke(new Action<int, string>(UpdateRowException), rowIndex, msg);
                return;
            }
            if (rowIndex >= 0 && rowIndex < this.moniqi_liebiao.Rows.Count)
            {
                this.moniqi_liebiao.Rows[rowIndex].Cells["yichang"].Value = msg;
            }
        }

        private void Control_Intercept_DropDown(object? sender, EventArgs e)
        {
            if (_isScriptRunning)
            {
                (sender as ComboBox).DroppedDown = false;
                this.moniqi_liebiao.Focus();
                MessageBox.Show("运行中禁止修改配置！");
            }
        }

        private void Control_Intercept_Enter(object? sender, EventArgs e)
        {
            if (_isScriptRunning)
            {
                this.moniqi_liebiao.Focus();
                MessageBox.Show("运行中禁止修改配置！");
            }
        }

        private void Control_Intercept_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (_isScriptRunning)
            {
                e.Handled = true;
                this.moniqi_liebiao.Focus();
            }
        }

        private void moniqi_liebiao_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.RowIndex >= 0)
            {
                _isRowAlreadySelected = this.moniqi_liebiao.Rows[e.RowIndex].Selected;
            }
        }

        private void moniqi_liebiao_CellMouseUp(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.RowIndex >= 0)
            {
                if (_isRowAlreadySelected)
                {
                    this.moniqi_liebiao.Rows[e.RowIndex].Selected = false;
                    this.moniqi_liebiao.CurrentCell = null;
                }
                _isRowAlreadySelected = false;
            }
        }

        private void Register_OLA()
        {
            try
            {
                // 主要是改这一行，加上 "OLA.dll"
                OLAPlugServer ola = new OLAPlugServer("OLA.dll");

                int ret = ola.Reg(OLAConfig.UserCode, OLAConfig.SoftCode, OLAConfig.Key);

                if (ret != 1) MessageBox.Show($"注册失败:{ret}");
                else ola.ReleaseObj();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DLL调用失败或丢失: {ex.Message}");
            }
        }

        private void moniqi_xuanze_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_isScriptRunning)
            {
                string name = this.moniqi_xuanze.Text;
                string foundPath = Auto_Find_Path(name);
                this.lujing_shuru.Text = string.IsNullOrEmpty(foundPath) ? @"D:\未找到，请手动输入" : foundPath;
            }
        }

        private string Auto_Find_Path(string emulatorName)
        {
            string nameLower = emulatorName.ToLower();

            if (nameLower.Contains("mumu"))
            {
                string defaultPath = @"D:\Program Files\Netease\MuMu";
                if (Directory.Exists(defaultPath)) return defaultPath;

                return Full_Search_MuMu(@"D:\");
            }

            if (nameLower.Contains("雷电"))
            {
                string res = Deep_Search_D_Drive("LDPlayer9");
                if (!string.IsNullOrEmpty(res)) return res;
                return Deep_Search_D_Drive("LDPlayer");
            }

            return "";
        }

        private string Full_Search_MuMu(string rootPath)
        {
            try
            {
                string[] dirs = Directory.GetDirectories(rootPath);
                foreach (string dir in dirs)
                {
                    if (Path.GetFileName(dir).Equals("MuMu", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }

                    if (dir.Contains("$RECYCLE.BIN") || dir.Contains("System Volume Information"))
                    {
                        continue;
                    }

                    string found = Full_Search_MuMu(dir);
                    if (!string.IsNullOrEmpty(found))
                    {
                        return found;
                    }
                }
            }
            catch
            {
                // 忽略权限受限的目录
            }
            return "";
        }

        private string Deep_Search_D_Drive(string targetName)
        {
            string root = @"D:\";
            string level0 = Path.Combine(root, targetName);
            if (Directory.Exists(level0)) return level0;
            try
            {
                string[] dirs = Directory.GetDirectories(root);
                foreach (string dir in dirs)
                {
                    string tryPath = Path.Combine(dir, targetName);
                    if (Directory.Exists(tryPath)) return tryPath;

                    if (Path.GetFileName(dir).Equals(targetName, StringComparison.OrdinalIgnoreCase)) return dir;
                }
            }
            catch { }
            return "";
        }

        private void shuaxin_liebiao_Click(object? sender, EventArgs e)
        {
            if (_isScriptRunning)
            {
                MessageBox.Show("运行中无法刷新");
                return;
            }

            moniqi_liebiao.Rows.Clear();
            string path = lujing_shuru.Text.Trim();

            if (!Directory.Exists(path))
            {
                MessageBox.Show("路径不存在！");
                return;
            }

            string vms = Path.Combine(path, "vms");
            if (!Directory.Exists(vms))
            {
                vms = Path.Combine(Directory.GetParent(path)?.FullName ?? "", "vms");
            }

            if (Directory.Exists(vms))
            {
                if (moniqi_xuanze.Text.Contains("雷电"))
                {
                    Parse_Leidian_Vms(vms);
                }
                else if (moniqi_xuanze.Text.Contains("MuMu"))
                {
                    Parse_Mumu_Vms(vms);
                }
            }

            moniqi_liebiao.ClearSelection();
            moniqi_liebiao.CurrentCell = null;
        }

        private void Parse_Leidian_Vms(string p)
        {
            foreach (var d in Directory.GetDirectories(p))
            {
                string n = new DirectoryInfo(d).Name;
                string id = n.StartsWith("leidian") ? n.Substring(7) : n;
                if (int.TryParse(id, out _) && id != "0")
                {
                    Tianjia_Hang((moniqi_liebiao.Rows.Count + 1).ToString(), $"雷电模拟器-{id}", "未运行");
                }
            }
        }

        private void Parse_Mumu_Vms(string p)
        {
            foreach (var d in Directory.GetDirectories(p))
            {
                string n = new DirectoryInfo(d).Name;
                string[] s = n.Split('-');
                if (s.Length > 0)
                {
                    string id = s[s.Length - 1];
                    if (int.TryParse(id, out _) && id != "0")
                    {
                        Tianjia_Hang((moniqi_liebiao.Rows.Count + 1).ToString(), $"MuMu模拟器-{id}", "未运行");
                    }
                }
            }
        }

        private void Tianjia_Hang(string a, string b, string c)
        {
            int i = moniqi_liebiao.Rows.Add();
            var r = moniqi_liebiao.Rows[i];
            r.Cells["xuhao"].Value = a;
            r.Cells["moniqi"].Value = b;
            r.Cells["zhuangtai"].Value = c;
            r.Cells["zhanghao"].Value = "";
            r.Cells["mima"].Value = "";
            r.Cells["jubing"].Value = "0";
            r.Cells["yichang"].Value = "无异常";
        }

        private void quanbu_tingzhi_Click_1(object? sender, EventArgs e)
        {
            quanbu_tingzhi_Click(sender, EventArgs.Empty);
        }

        private void timer_runtime_Tick_1(object sender, EventArgs e)
        {
            timer_runtime_Tick(sender, e);
        }
    }
}