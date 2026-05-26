using System;
using System.Threading.Tasks;
using OLAPlug;

namespace OLA
{
    public class GameTask
    {
        private readonly TaskWorker _worker;

        public bool LastTaskCompleted { get; private set; } = true;

        public GameTask(TaskWorker worker)
        {
            _worker = worker;
        }

        private void MarkTaskFailed(string message)
        {
            LastTaskCompleted = false;
            _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            _worker.MarkCurrentTaskUnfinished();
        }

        public async Task Execute(string taskName)
        {
            LastTaskCompleted = true;
            _worker.LastActionTime = DateTime.Now;

            switch (taskName)
            {
                case "主线任务":
                    await MainQuest();
                    break;

                case "每日活跃":
                    await DailyActive();
                    break;

                case "自动签到":
                    await AutoSign();
                    break;

                case "支线任务":
                    await SideQuest();
                    break;

                case "挂机任务":
                    await AfkTask();
                    break;

                default:
                    MarkTaskFailed($"未知任务: {taskName}");
                    await _worker.SmartSleep(1000);
                    break;
            }
        }

        private async Task MainQuest()
        {
            _worker.StatusCallback?.Invoke(_worker.RowIndex, "启动/检查游戏", _worker.CurrentBindHwnd.ToString());
            _worker.EnsureGameRunning();

            if (!await _worker.SmartSleep(5000)) return;

            _worker.StatusCallback?.Invoke(_worker.RowIndex, "执行主线中...", _worker.CurrentBindHwnd.ToString());

            while (true)
            {
                if (!await _worker.SmartSleep(1000)) return;

                if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                {
                    MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                    return;
                }

                var im = _worker.Ola.MatchWindowsFromPath(0, 0, 960, 540, "等级不足.bmp", 0.85, 0, 0, 1.0);
                if (im != null && im.MatchState)
                {
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ⛔ 发现等级不足，退出主线循环");
                    await _worker.SmartSleep(1000);
                    break;
                }

                if (await _worker.OL_MatchWindowsFromPath(641, 442, 694, 457, "立即加点.bmp", 666, 449, 500))
                {
                    await _worker.SmartSleep(1000);

                    while (true)
                    {
                        if (!await _worker.SmartSleep(100)) return;

                        if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                        {
                            MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                            return;
                        }

                        if (await _worker.OL_CmpColor("514,116,a37b20|520,116,8d6d21|520,117,916f21|840,22,d7e1eb", 939, 22, 500)) break;
                        if (await _worker.OL_CmpColor("410,209,1c1d23|412,245,1d1f26|410,282,1f2228|418,319,20232a|427,181,9bc925|432,179,a1cd26", 940, 22, 500)) continue;
                        if (await _worker.OL_CmpColor("410,282,917e52|417,282,85724a|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        if (await _worker.OL_CmpColor("412,245,978156|418,245,8f7a4e|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        if (await _worker.OL_CmpColor("410,209,957f54|418,209,8f7a4e|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        if (await _worker.OL_CmpColor("419,319,8f784a|410,319,937f52|427,180,9fcd26|432,180,95c125|497,147,9ba5ab", 477, 397, 500)) continue;
                        if (await _worker.OL_CmpColor("544,209,b7a372|547,280,9f875a|568,178,e9e9e3|491,133,d1dde7|490,399,efefe7", 577, 179, 500)) continue;
                        if (await _worker.OL_CmpColor("250,203,f17c01|273,355,ef7b01|579,90,c5a55c|379,90,c3a35a|739,190,e1dfd7", 760, 189, 500)) continue;
                        if (await _worker.OL_CmpColor("554,116,a78f60|563,22,d1dbe3|840,23,d1dde7|469,146,a7a7a7", 554, 117, 500)) continue;

                        await _worker.SmartSleep(1000);
                    }
                }

                if (await _worker.OL_CmpColor("554,116,a78f60|563,22,d1dbe3|840,23,d1dde7|469,146,a7a7a7", 554, 117, 500))
                {
                    await _worker.SmartSleep(1000);

                    while (true)
                    {
                        if (!await _worker.SmartSleep(100)) return;

                        if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                        {
                            MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                            return;
                        }

                        if (await _worker.OL_FindStr(78, 36, 91, 49, "等级达到30", "ada187-101010", 871, 79, 500)) break;
                        if (await _worker.OL_FindStr(87, 328, 117, 350, "幻术园", "e3dbcb-303030", 148, 337, 500)) continue;
                        if (await _worker.OL_CmpColor("69,340,e1d7a7|141,299,fbf1bf|235,336,dfd7a3|277,384,b3a393|728,475,f3ebd7", 785, 477, 500)) continue;
                        if (await _worker.OL_FindStr(87, 328, 117, 350, "当前地图幻术园", "e3dbcb-303030", 871, 79, 500)) continue;
                    }
                }

                if (await _worker.OL_CmpColor("776,21,32435c|794,12,efe1d3|793,30,e1d9cf|819,12,f3e7db|783,475,e9ebeb|783,484,edefef|837,485,efefef|934,18,919187", 810, 478, 500)) continue;
                if (await _worker.OL_CmpColor("807,475,ffffff|805,482,f7f7f7|794,13,f1e7db|819,12,f3e7db|933,16,959587", 807, 477, 500)) continue;
                if (await _worker.OL_CmpColor("783,430,d90505|841,430,0000e3|759,72,fbfbfb|786,70,f3f3f3|934,16,959587", 810, 477, 500)) continue;
                if (await _worker.OL_CmpColor("546,159,c3b76a|547,161,811702|534,155,561711|560,191,859db3|387,470,b3b5b7", 942, 18, 500)) continue;
                if (await _worker.OL_CmpColor("762,117,d5b97f|764,124,dbbf83|764,120,f7d58f|784,120,f9d78f|743,80,ededeb", 878, 113, 500)) continue;
                if (await _worker.OL_CmpColor("841,430,0000e3|780,428,f10000|802,70,fdfdfb|801,22,ede7db|828,20,ede7db", 810, 478, 500)) continue;
                if (await _worker.OL_CmpColor("424,403,62e303|424,436,6cf903|466,459,f7b164|775,511,b92e2c|905,520,edefef", 910, 519, 500)) continue;
                if (await _worker.OL_CmpColor("819,13,ede7db|827,476,efefef|790,480,f1f3f3|824,437,dbdbdb", 807, 478, 500)) continue;
                if (await _worker.OL_MatchWindowsFromPath(557, 166, 669, 204, "新手启程礼.bmp", 575, 359, 500)) continue;
                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "立即启动.bmp", 478, 395, 3000)) continue;
                if (await _worker.OL_MatchWindowsFromPath(445, 476, 516, 498, "开始游戏.bmp", 481, 485, 3000)) continue;
                if (await _worker.OL_CmpColor("41,115,bd972c|41,113,bd972c|41,110,bf972c", 100, 111, 2000)) continue;
                if (await _worker.OL_CmpColor("235,174,dfd5a3|156,207,fff3bf|73,126,f1e7b7|759,184,b5afa3", 782, 476, 2000)) continue;
            }

            _worker.StatusCallback?.Invoke(_worker.RowIndex, "主线任务结束", _worker.CurrentBindHwnd.ToString());
        }

        private async Task DailyActive()
        {
            _worker.StatusCallback?.Invoke(_worker.RowIndex, "准备日常...", _worker.CurrentBindHwnd.ToString());
            _worker.EnsureGameRunning();

            if (!await _worker.SmartSleep(3000)) return;

            while (true)
            {
                if (!await _worker.SmartSleep(1000)) return;

                if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                {
                    MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                    return;
                }

                if (await _worker.OL_MatchWindowsFromPath(0, 0, 1280, 720, "一键领取.bmp", 600, 600, 1000)) continue;
                if (await _worker.OL_CmpColor("1100,200,FF0000", 1100, 200, 1000)) continue;

                var rewardRes = _worker.Ola.MatchWindowsFromPath(0, 0, 1280, 720, @"daily\get_reward.bmp", 0.9, 0, 0, 1.0);
                if (rewardRes.MatchState)
                {
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 领取日常奖励");
                    await _worker.OL_LeftClick(rewardRes.X, rewardRes.Y);
                    await _worker.SmartSleep(1500);
                }
            }
        }

        private async Task AutoSign()
        {
            _worker.StatusCallback?.Invoke(_worker.RowIndex, "自动签到中...", _worker.CurrentBindHwnd.ToString());
            _worker.EnsureGameRunning();

            if (!await _worker.SmartSleep(3000)) return;

            await _worker.OL_MatchWindowsFromPath(0, 0, 1280, 720, "关闭弹窗.bmp", 1200, 50, 1000);
            await _worker.OL_CmpColor("640,360,FFFFFF", 640, 360, 2000);

            var iconRes = _worker.Ola.MatchWindowsFromPath(0, 0, 1280, 720, @"sign\icon.bmp", 0.9, 0, 0, 1.0);
            if (iconRes.MatchState)
            {
                await _worker.OL_LeftClick(iconRes.X, iconRes.Y);
                await _worker.SmartSleep(2000);

                _worker.StatusCallback?.Invoke(_worker.RowIndex, "点击签到按钮", _worker.CurrentBindHwnd.ToString());

                int cx, cy;
                if (_worker.Ola.FindStr(0, 0, 1280, 720, "签到", "ffffff-202020", "无尽黑暗.txt", 0.8, out cx, out cy) != -1)
                {
                    await _worker.OL_LeftClick(cx, cy);
                    await _worker.SmartSleep(1000);
                }
            }
            else
            {
                _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ⚠️ 未找到签到图标");
            }
        }

        private async Task SideQuest()
        {
            _worker.StatusCallback?.Invoke(_worker.RowIndex, "执行支线中...", _worker.CurrentBindHwnd.ToString());
            _worker.EnsureGameRunning();

            if (!await _worker.SmartSleep(3000)) return;

            while (true)
            {
                if (!await _worker.SmartSleep(1000)) return;

                if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                {
                    MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                    return;
                }

                if (await _worker.OL_CmpColor("200,300,00FF00|210,310,FFFFFF", 200, 300, 3000)) continue;
                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "支线_前往.bmp", 500, 500, 2000)) continue;

                string ocrText = _worker.OL_OcrFromDict(50, 200, 350, 600, "ffffff-101010");
                if (ocrText.Contains("支线"))
                {
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 发现任务文本: {ocrText}");
                    await _worker.OL_LeftClick(100, 250, 15);
                    await _worker.SmartSleep(5000);
                }
                else
                {
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ✅ 暂无支线任务");
                    break;
                }
            }
        }

        private async Task AfkTask()
        {
            _worker.StatusCallback?.Invoke(_worker.RowIndex, "开始挂机...", _worker.CurrentBindHwnd.ToString());
            _worker.EnsureGameRunning();

            if (!await _worker.SmartSleep(3000)) return;

            await _worker.OL_CmpColor("800,600,FF00FF", 800, 600, 500);

            var autoRes = _worker.Ola.MatchWindowsFromPath(0, 0, 1280, 720, @"afk\auto_fight.bmp", 0.9, 0, 0, 1.0);
            if (autoRes.MatchState)
            {
                _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ⚔️ 已开启自动战斗");
                await _worker.OL_LeftClick(autoRes.X, autoRes.Y);
            }

            while (true)
            {
                if (!await _worker.SmartSleep(5000)) return;
                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "网络重连.bmp", 480, 360, 5000)) continue;
                if (await _worker.OL_CmpColor("480,360,FF0000", 480, 360, 1000)) continue;
            }
        }
    }
}
