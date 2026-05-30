using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using OLAPlug;

namespace OLA
{
    public class IniHelper
    {
        // ==========================================
        // 1. INI 文件路径定义
        // ==========================================
        // INI 文件名，放在程序所在目录
        private static readonly string IniFileName = "config.ini";

        private const string AuthSection = "授权";
        private const string OldAuthSection = "Authorization";

        // 完整路径
        public static string IniPath
        {
            get
            {
                // 获取当前执行程序的目录
                string? exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (exePath == null)
                {
                    // Fallback, though should not happen in a standard WinForms app
                    exePath = Directory.GetCurrentDirectory();
                }
                return Path.Combine(exePath, IniFileName);
            }
        }

        // ==========================================
        // 2. Windows API 声明 (P/Invoke)
        // ==========================================

        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode)]
        public static extern int GetIniString(
            string lpAppName,       // Section (例如: "General")
            string? lpKeyName,      // Key (例如: "BasePath")；为 null 时删除整个 Section
            string lpDefault,       // 默认值 (如果找不到 Section 或 Key)
            StringBuilder lpReturnedString, // 用于接收读取结果的 StringBuilder
            int nSize,              // lpReturnedString 的最大容量
            string lpFileName       // INI 文件完整路径
        );

        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW", CharSet = CharSet.Unicode)]
        public static extern bool WriteIniString(
            string lpAppName,       // Section (例如: "General")
            string? lpKeyName,      // Key (例如: "BasePath")；为 null 时删除整个 Section
            string? lpString,       // 要写入的值；为 null 时删除 Key 或 Section
            string lpFileName       // INI 文件完整路径
        );

        // ==========================================
        // 3. INI 基础读写
        // ==========================================

        public static string Read(string Section, string Key, string DefaultValue)
        {
            StringBuilder sb = new StringBuilder(500);
            GetIniString(Section, Key, DefaultValue, sb, sb.Capacity, IniPath);
            return sb.ToString();
        }

        public static string ReadWithFallback(
            string Section,
            string Key,
            string OldSection,
            string OldKey,
            string DefaultValue)
        {
            string value = Read(Section, Key, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Read(OldSection, OldKey, DefaultValue);
        }

        public static bool Write(string Section, string Key, string Value)
        {
            return WriteIniString(Section, Key, Value, IniPath);
        }

        public static bool DeleteKey(string Section, string Key)
        {
            return WriteIniString(Section, Key, null, IniPath);
        }

        public static bool DeleteSection(string Section)
        {
            return WriteIniString(Section, null, null, IniPath);
        }

        // ==========================================
        // 4. OLA 授权登录流程
        //    官方建议流程：Login -> 失败后 Activate -> 再次 Login。
        // ==========================================

        public static bool EnsureOlaAuthorization(
            string dllPath,
            string userCode,
            string softCode,
            string featureList,
            string softVersion,
            string dealerCode)
        {
            OLAPlugServer? ola = null;

            try
            {
                ola = new OLAPlugServer(dllPath);

                // 启动时先按官方流程尝试 Login。Login 成功会自动注册插件功能，不需要再调用 Reg。
                string loginJson = ola.Login(userCode, softCode, featureList, softVersion, dealerCode);
                if (IsSuccess(loginJson, out string loginMessage))
                {
                    SaveLoginResult(loginJson);
                    return true;
                }

                string promptMessage = File.Exists(IniPath)
                    ? $"授权登录失败，请输入新的激活码重新激活。\r\n{FormatMessage(loginMessage)}"
                    : "首次启动需要输入授权激活码。";

                while (true)
                {
                    string? licenseKey = PromptLicenseKey(promptMessage);
                    if (string.IsNullOrWhiteSpace(licenseKey))
                    {
                        MessageBox.Show("未输入激活码，程序将退出。", "授权失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    string activateJson = ola.Activate(userCode, softCode, softVersion, dealerCode, licenseKey.Trim());
                    if (!IsSuccess(activateJson, out string activateMessage))
                    {
                        DialogResult retry = MessageBox.Show(
                            $"激活失败。\r\n{FormatMessage(activateMessage)}\r\n\r\n是否重新输入激活码？",
                            "授权失败",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (retry == DialogResult.Yes)
                        {
                            promptMessage = "请重新输入授权激活码。";
                            continue;
                        }

                        return false;
                    }

                    SaveActivationConfig(activateJson);

                    // 激活成功后必须再次 Login 建立会话。
                    loginJson = ola.Login(userCode, softCode, featureList, softVersion, dealerCode);
                    if (IsSuccess(loginJson, out loginMessage))
                    {
                        SaveLoginResult(loginJson);
                        MessageBox.Show("授权登录成功。", "授权成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }

                    MessageBox.Show(
                        $"激活成功，但再次登录失败。\r\n{FormatMessage(loginMessage)}",
                        "授权失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"授权 DLL 调用失败或 OLA.dll 丢失：{ex.Message}", "授权异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                try
                {
                    ola?.ReleaseObj();
                }
                catch
                {
                    // 忽略释放阶段异常，避免覆盖真实授权结果。
                }
            }
        }

        private static bool IsSuccess(string json, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                message = "接口返回为空。";
                return false;
            }

            try
            {
                JObject obj = JObject.Parse(json);
                message = obj.Value<string>("Message") ?? string.Empty;
                return obj.Value<int?>("Status") == 1;
            }
            catch (Exception ex)
            {
                message = $"接口返回无法解析：{ex.Message}";
                return false;
            }
        }

        private static void SaveActivationConfig(string activateJson)
        {
            // 激活成功后，只记录用户需要看的授权信息，不保存卡密、软件码、用户码等内部信息。
            SaveCommonAuthFields(activateJson);
            CleanOldAuthorizationFields();
        }

        private static void SaveLoginResult(string loginJson)
        {
            SaveCommonAuthFields(loginJson);
            Write(AuthSection, "最近登录", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            CleanOldAuthorizationFields();
        }

        private static void SaveCommonAuthFields(string json)
        {
            try
            {
                JObject obj = JObject.Parse(json);
                int status = obj.Value<int?>("Status") ?? 0;

                Write(AuthSection, "授权状态", status == 1 ? "正常" : "失败");
                Write(AuthSection, "授权类型", obj.Value<string>("LicenseTypeStr") ?? string.Empty);
                Write(AuthSection, "到期时间", FormatDateTimeWithoutMilliseconds(obj.Value<string>("EndTime") ?? string.Empty));
                Write(AuthSection, "接口提示", obj.Value<string>("Message") ?? string.Empty);
            }
            catch
            {
                // 已在调用方判断过 JSON 状态，这里只负责尽量落盘。
            }
        }

        private static string FormatDateTimeWithoutMilliseconds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParse(value, out DateTime dateTime))
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            int dotIndex = value.LastIndexOf('.');
            if (dotIndex > 0)
            {
                return value.Substring(0, dotIndex);
            }

            return value;
        }

        public static string GetAuthorizationDisplayText()
        {
            string status = Read(AuthSection, "授权状态", string.Empty);
            string licenseType = Read(AuthSection, "授权类型", string.Empty);
            string endTime = FormatDateTimeWithoutMilliseconds(Read(AuthSection, "到期时间", string.Empty));

            if (string.IsNullOrWhiteSpace(status))
            {
                status = "未登录";
            }

            if (string.IsNullOrWhiteSpace(licenseType))
            {
                licenseType = "未知";
            }

            if (string.IsNullOrWhiteSpace(endTime))
            {
                endTime = "未知";
            }

            return $"授权状态：{status}    授权类型：{licenseType}    到期时间：{endTime}";
        }

        private static void CleanOldAuthorizationFields()
        {
            // 清理旧版英文授权段，避免 config.ini 里同时出现两套授权记录。
            DeleteSection(OldAuthSection);

            // 清理曾经写入新版授权段的多余字段。
            string[] oldKeys =
            {
                "LicenseKey",
                "UserCode",
                "SoftCode",
                "FeatureList",
                "SoftVersion",
                "DealerCode",
                "LastActivateJson",
                "LastActivateTime",
                "Status",
                "EndTime",
                "LicenseType",
                "LicenseTypeStr",
                "RemainingCount",
                "ServerTime",
                "Message",
                "LastLoginJson",
                "LastLoginTime",
                "LastVersion"
            };

            foreach (string key in oldKeys)
            {
                DeleteKey(AuthSection, key);
            }
        }

        private static string FormatMessage(string message)
        {
            return string.IsNullOrWhiteSpace(message) ? "未返回错误信息。" : message.Trim();
        }

        private static string? PromptLicenseKey(string message)
        {
            using Form form = new Form();
            using Label label = new Label();
            using TextBox textBox = new TextBox();
            using Button okButton = new Button();
            using Button cancelButton = new Button();

            form.Text = "授权登录";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ClientSize = new System.Drawing.Size(420, 160);
            form.ShowInTaskbar = false;

            label.Text = message + "\r\n请输入授权激活码：";
            label.Location = new System.Drawing.Point(16, 14);
            label.Size = new System.Drawing.Size(388, 58);

            textBox.Location = new System.Drawing.Point(16, 78);
            textBox.Size = new System.Drawing.Size(388, 25);
            textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            okButton.Text = "确定";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new System.Drawing.Point(248, 118);
            okButton.Size = new System.Drawing.Size(75, 28);

            cancelButton.Text = "取消";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(329, 118);
            cancelButton.Size = new System.Drawing.Size(75, 28);

            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;
            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);

            return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
