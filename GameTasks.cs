using System;
using System.Threading.Tasks;
using OLAPlug;

namespace OLA
{
    public class GameTask
    {
        private readonly TaskWorker _worker;

        // 当前任务的默认 UI 状态。
        // 例如主线任务默认显示“执行主线中...”。
        // 临时步骤显示 2 秒后，如果没有新的步骤更新，会自动恢复到这个默认状态。
        private string _defaultStep = "";

        // 记录上一次显示到 UI 的步骤，避免重复刷新同一个状态。
        private string _lastStep = "";

        // UI 步骤版本号。
        // 每次 ShowStep 都会 +1，2 秒后恢复默认状态时会校验版本号。
        // 如果 2 秒内出现了新的 ShowStep，旧的恢复任务不会再生效。
        private int _stepVersion = 0;

        public bool LastTaskCompleted { get; private set; } = true;

        public GameTask(TaskWorker worker)
        {
            _worker = worker;
        }

        private void MarkTaskFailed(string message)
        {
            // 任务失败后取消所有等待恢复默认状态的 UI 任务，防止失败后又被恢复成“执行中”。
            _stepVersion++;

            LastTaskCompleted = false;
            _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            _worker.MarkCurrentTaskUnfinished();
        }

        // 设置当前任务的默认 UI 状态。
        // 例如主线任务默认是“执行主线中...”。
        // 临时步骤显示 2 秒后，会自动恢复到这个默认状态。
        private void SetDefaultStep(string step)
        {
            _defaultStep = step;
            ShowStep(step, autoBack: false);
        }

        // UI 步骤显示函数。
        // 规则：
        // 1. ShowStep("A") 会立即把 UI 更新成 A。
        // 2. 默认 autoBack = true，表示 A 是临时状态。
        // 3. 如果 2 秒内没有新的 ShowStep，UI 自动恢复成 _defaultStep。
        // 4. 如果 2 秒内又出现 ShowStep("B")，A 的自动恢复失效，等待 B 的 2 秒恢复。
        // 5. autoBack = false 表示这个状态不自动恢复，适合任务开始、任务结束、任务失败。
        private void ShowStep(string step, bool autoBack = true, int autoBackMs = 2000)
        {
            int version = ++_stepVersion;
            SetStep(step);

            if (!autoBack) return;

            _ = Task.Run(async () =>
            {
                await Task.Delay(autoBackMs);

                // 只有 2 秒内没有新的 ShowStep，才恢复默认状态。
                if (version == _stepVersion && !string.IsNullOrEmpty(_defaultStep))
                {
                    SetStep(_defaultStep);
                }
            });
        }

        // 真正刷新 UI 的方法。
        // 同一个状态不重复刷新，避免 UI 频繁跳动。
        private void SetStep(string step)
        {
            if (_lastStep == step) return;

            _lastStep = step;
            _worker.StatusCallback?.Invoke(_worker.RowIndex, step, _worker.CurrentBindHwnd.ToString());
        }

        public async Task Execute(string taskName)
        {
            LastTaskCompleted = true;
            _worker.LastActionTime = DateTime.Now;
            _lastStep = "";
            _defaultStep = "";
            _stepVersion++;

            switch (taskName)
            {
                case "主线任务":
                    await MainQuest();
                    break;

                case "每日活跃":
                    await DailyActive();
                    break;

                case "每日签到":
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
            // 默认 UI 状态。
            // 临时步骤显示 2 秒后，如果没有新的 ShowStep，会自动恢复到“执行主线中...”。
            SetDefaultStep("执行主线中...");

            if (!await _worker.SmartSleep(1000)) return;

            DateTime enterStartTime = DateTime.Now;

            // =========================
            // 小循环：主线任务进入条件
            // 进入条件：执行到了 MainQuest()
            // 退出条件 1：找到 游戏主页面.bmp -> break -> 进入下面正式主线大循环
            // 退出条件 2：3 分钟内没找到 游戏主页面.bmp -> MarkTaskFailed + return
            // 注意：这里用 _worker.Ola.MatchWindowsFromPath，只判断图片，不点击。
            // =========================
            while (true)
            {
                if (!await _worker.SmartSleep(1000)) return;

                var mainPage = _worker.Ola.MatchWindowsFromPath(0, 0, 960, 540, "游戏主页面.bmp", 0.85, 0, 0, 1.0);
                if (mainPage != null && mainPage.MatchState)
                {
                    ShowStep("主线：进入主线循环");
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 找到游戏主页面.bmp，进入主线任务循环");
                    await _worker.SmartSleep(1000);
                    break;
                }

                if ((DateTime.Now - enterStartTime).TotalMinutes >= 3)
                {
                    MarkTaskFailed("⏳ 三分钟内未找到游戏主页面.bmp，无法进入主线任务循环");
                    return;
                }
            }

            _worker.LastActionTime = DateTime.Now;

            // =========================
            // 大循环：正式主线任务循环
            // 进入条件：上面小循环已经找到 游戏主页面.bmp
            // 退出条件 1：找到 等级不足.bmp -> break -> 主线任务结束
            // 退出条件 2：3 分钟没有任何点击动作 -> MarkTaskFailed + return
            // 退出条件 3：任务被停止/取消 -> SmartSleep 返回 false -> return
            // =========================
            while (true)
            {
                if (!await _worker.SmartSleep(1000)) return;

                // 防卡死：LastActionTime 会在 OL_LeftClick 里更新。
                // 如果 3 分钟没有点击动作，认为卡住，直接标记失败并退出主线任务。
                if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                {
                    MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                    return;
                }

                // 主线大循环的正常退出条件：等级不足。
                // 这里只判断图片，不点击；找到后 break 退出大循环。
                var im = _worker.Ola.MatchWindowsFromPath(0, 0, 960, 540, "等级不足.bmp", 0.85, 0, 0, 1.0);
                if (im != null && im.MatchState)
                {
                    ShowStep("主线：等级不足，结束", autoBack: false);
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ⛔ 等级不足，退出主线循环");
                    await _worker.SmartSleep(1000);
                    break;
                }


                if (await _worker.OL_CmpColor("46,181,d7d7b9|65,182,e9e9c8|129,198,f1f1f1|142,199,e0e0e0|167,201,dedede", 126, 208, 500))
                {
                    ShowStep("击杀个人头领");
                    continue;
                }


                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "召唤.bmp", 663, 344))
                {
                    ShowStep("召唤");
                    continue;
                }

                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "头领已解封.bmp", 695, 448))
                {
                    ShowStep("头领已解封");
                    continue;
                }

                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "召唤个人头领.bmp", 398, 467))
                {
                    ShowStep("召唤个人头领");
                    continue;
                }
                if (await _worker.OL_CmpColor("161,147,eaeac9|161,151,eaeac9|221,133,fffe53|215,135,fffd75|81,151,d9d9d9", 541, 34, 500))
                {
                    ShowStep("主线：个人头领");
                    continue;
                }


                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "幸运轮盘抽奖.bmp", 437, 303))
                {
                    ShowStep("幸运轮盘抽奖");
                    continue;
                }

                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "可穿戴装备.bmp", 651, 364))
                {
                    ShowStep("有可穿戴装备");
                    continue;
                }
                if (await _worker.OL_CmpColor("433,383,e6e6e6|451,379,ececec|465,386,e6e6e6|479,378,f6f6f6", 342, 106, 500))
                {
                    ShowStep("主线：弹窗关闭");
                    continue;
                }

                if (await _worker.OL_CmpColor("367,14,e5e5e5|374,25,e4e4e4|474,18,fbfbd7|484,16,ededcc", 75, 238, 500))
                {
                    ShowStep("主线：战场PK");
                    continue;
                }

                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "新兵战场.bmp", 760, 354, 2000))
                {
                    ShowStep("主线：新兵战场");
                    continue;
                }

                if (await _worker.OL_CmpColor("398,142,f8e67d|461,143,f9e894|474,149,faefaa|557,154,fbf3b9", 523, 383, 500))
                {
                    ShowStep("主线：技能解锁跳过");
                    continue;
                }
                if (await _worker.OL_CmpColor("46,125,fbfbd8|46,129,e2e2c2|54,129,e5e5c4|54,130,e5e5c4", 102, 136, 500))
                {
                    ShowStep("主线：主线任务1");
                    continue;
                }


                // 普通主线步骤：找到后点击，然后 continue 回到大循环开头重新识别。
                // ShowStep 写在 if 里面，表示“确实找到了并执行了这一步”。
                if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "主线任务引导.bmp", 95, 136))
                {
                    ShowStep("主线：任务引导");
                    continue;
                }

                // 对话/跳过类处理：这是普通步骤，找到并点击后直接 continue。
                if (await _worker.OL_CmpColor("834,373,f9e0b9|834,377,dbc693|808,373,fbfbd7", 808, 376, 500))
                {
                    ShowStep("主线：跳过对话");
                    continue;
                }

                // 特殊步骤：立即加点。
                // 找到后会进入一个小循环，专门处理加点界面和相关弹窗。
                if (await _worker.OL_CmpColor("46,177,ececca|81,201,d8d8d8|98,201,e5e5e5|132,201,e6e6e6|214,187,fefcb1", 671, 30, 500))
                {
                    ShowStep("支线-10次联邦");
                    await _worker.SmartSleep(1000);

                    // =========================
                    // 小循环：立即加点处理循环
                    // 进入条件：外层大循环找到了 立即加点.bmp
                    // 退出条件 1：第一条 OL_CmpColor 成功 -> break -> 回到外层主线大循环
                    // 退出条件 2：3 分钟没有点击动作 -> MarkTaskFailed + return
                    // =========================
                    while (true)
                    {
                        if (!await _worker.SmartSleep(100)) return;

                        if ((DateTime.Now - _worker.LastActionTime).TotalMinutes >= 3)
                        {
                            MarkTaskFailed("⏳ 三分钟没识别到任务，防卡死触发");
                            return;
                        }

                        if (await _worker.OL_CmpColor("514,116,a37b20|520,116,8d6d21|520,117,916f21|840,22,d7e1eb", 939, 22, 500))
                        {
                            ShowStep("主线：加点处理完成");
                            break;
                        }

                        // 以下都是加点界面内的分支处理：匹配成功 -> 点击 -> continue 重新识别当前界面。



                        if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "提交确认.bmp", 636, 400))
                        {
                            ShowStep("日常-联邦提交确认");
                            continue;
                        }



                        if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "提交装备.bmp", 488, 172))
                        {
                            ShowStep("日常-联邦提交装备");
                            await _worker.OL_LeftClick(563, 173);
                            await _worker.SmartSleep(500);
                            continue;
                        }




                        if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "联邦任务购买.bmp", 481, 374))
                        {
                            ShowStep("日常-联邦任务购买");
                            continue;
                        }

                        if (await _worker.OL_CmpColor("546,362,04eaee|447,357,fffaf1", 547, 363, 500))
                        {
                            ShowStep("寻路传送");
                            continue;
                        }

                        if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "联邦任务2.bmp", 387, 139))
                        {
                            ShowStep("日常-联邦任务2");
                            continue;
                        }
                        if (await _worker.OL_MatchWindowsFromPath(0, 0, 960, 540, "联邦任务.bmp", 387, 139))
                        {
                            ShowStep("日常-联邦任务");
                            continue;
                        }
                        if (await _worker.OL_CmpColor("159,97,d8ba6c|282,394,e3cc9b|292,393,eed9a8|208,191,59c225|188,105,d2b469", 278, 395, 500))
                        {
                            ShowStep("日常-联邦任务-领取");
                            continue;
                        }

                        if (await _worker.OL_CmpColor("834,373,f9e0b9|834,377,dbc693|808,373,fbfbd7", 808, 376, 500))
                        {
                            ShowStep("跳过对话");
                            continue;
                        }


                        await _worker.SmartSleep(1000);
                    }
                }

                // 特殊步骤：等级/地图引导处理。
                // 找到这个界面后进入小循环，直到识别到“等级达到30”后 break 回到主线大循环。
               

            }

            ShowStep("主线任务结束", autoBack: false);
        }

        private async Task DailyActive()
        {
            SetDefaultStep("准备日常...");

            if (!await _worker.SmartSleep(1000)) return;

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
                if (rewardRes != null && rewardRes.MatchState)
                {
                    ShowStep("日常：领取奖励");
                    _worker.LogCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 领取日常奖励");
                    await _worker.OL_LeftClick(rewardRes.X, rewardRes.Y);
                    await _worker.SmartSleep(1500);
                }
            }
        }

        private async Task AutoSign()
        {
            SetDefaultStep("自动签到中...");

            if (!await _worker.SmartSleep(1000)) return;

            await _worker.OL_MatchWindowsFromPath(0, 0, 1280, 720, "关闭弹窗.bmp", 1200, 50, 1000);
            await _worker.OL_CmpColor("640,360,FFFFFF", 640, 360, 2000);

            var iconRes = _worker.Ola.MatchWindowsFromPath(0, 0, 1280, 720, @"sign\icon.bmp", 0.9, 0, 0, 1.0);
            if (iconRes != null && iconRes.MatchState)
            {
                ShowStep("签到：打开签到界面");
                await _worker.OL_LeftClick(iconRes.X, iconRes.Y);
                await _worker.SmartSleep(2000);

                ShowStep("点击签到按钮");

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
            SetDefaultStep("执行支线中...");

            if (!await _worker.SmartSleep(1000)) return;

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
                    ShowStep("支线：点击任务文本");
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
            SetDefaultStep("开始挂机...");

            if (!await _worker.SmartSleep(1000)) return;

            await _worker.OL_CmpColor("800,600,FF00FF", 800, 600, 500);

            var autoRes = _worker.Ola.MatchWindowsFromPath(0, 0, 1280, 720, @"afk\auto_fight.bmp", 0.9, 0, 0, 1.0);
            if (autoRes != null && autoRes.MatchState)
            {
                ShowStep("挂机：开启自动战斗");
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
