using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Services
{
    /// <summary>
    /// 用于通过修改Windows工作区来保留屏幕区域的服务
    /// </summary>
    public class ScreenReservationService : ReactiveObject, IDisposable
    {
        #region Win32 API 声明

        // SystemParametersInfo 相关常量
        private const int SPI_GETWORKAREA = 0x0030;
        private const int SPI_SETWORKAREA = 0x002F;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        // AppBar 相关常量
        private const int ABM_NEW = 0;
        private const int ABM_REMOVE = 1;
        private const int ABM_QUERYPOS = 2;
        private const int ABM_SETPOS = 3;
        private const int ABM_GETSTATE = 4;
        private const int ABM_GETTASKBARPOS = 5;
        private const int ABM_ACTIVATE = 6;
        private const int ABM_GETAUTOHIDEBAR = 7;
        private const int ABM_SETAUTOHIDEBAR = 8;
        private const int ABE_LEFT = 0;
        private const int ABE_TOP = 1;
        private const int ABE_RIGHT = 2;
        private const int ABE_BOTTOM = 3;

        // Shell 消息常量
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int HWND_BROADCAST = 0xFFFF;
        private const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

        // RECT 结构体用于定义矩形区域 - 修改为public以解决可访问性问题
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public override string ToString()
            {
                return $"{{Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}}}";
            }
        }

        // APPBARDATA 结构体用于AppBar消息
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;

            public APPBARDATA(IntPtr hwnd)
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                hWnd = hwnd;
                uCallbackMessage = 0;
                uEdge = 0;
                rc = new RECT();
                lParam = IntPtr.Zero;
            }
        }

        // WNDPROC 委托用于窗口过程回调
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // 导入 SystemParametersInfo 函数
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint uiAction,
            uint uiParam,
            ref RECT pvParam,
            uint fWinIni);

        // 导入 SHAppBarMessage 函数
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(
            int dwMessage,
            ref APPBARDATA pData);

        // 获取操作系统版本
        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFO osVersionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OSVERSIONINFO
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        // 用于查找窗口的API
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // 用于发送消息的API
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 注册消息回调
        [DllImport("user32.dll")]
        private static extern uint RegisterWindowMessage(string lpString);

        #endregion

        // 保存原始工作区
        private RECT _originalWorkArea;
        private bool _isAreaReserved;
        private bool _isAppBarRegistered;
        private IntPtr _appBarHwnd;
        private readonly bool _isWindows10OrLater;
        private APPBARDATA _appBarData;
        private CancellationTokenSource _explorerWatcherCts;
        private RECT _lastAppBarRect;
        private int _lastAppBarEdge;
        private bool _isMonitoringExplorer;
        private const int EXPLORER_CHECK_INTERVAL = 5000; // 5秒检查一次

        [Reactive] public bool IsAreaReserved { get; private set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ScreenReservationService()
        {
            // 初始化时获取原始工作区
            _originalWorkArea = new RECT();
            _isAreaReserved = false;
            _isAppBarRegistered = false;
            _appBarHwnd = IntPtr.Zero;
            _lastAppBarEdge = -1;

            // 检测操作系统版本
            _isWindows10OrLater = DetectWindows10OrLater();

            // 初始化Explorer监听
            _explorerWatcherCts = new CancellationTokenSource();
        }

        /// <summary>
        /// 检测是否Windows 10或更高版本
        /// </summary>
        private bool DetectWindows10OrLater()
        {
            try
            {
                // 获取操作系统版本
                OSVERSIONINFO osVersionInfo = new()
                {
                    dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFO))
                };

                if (GetVersionEx(ref osVersionInfo))
                {
                    // Windows 10的主版本号是10
                    return osVersionInfo.dwMajorVersion >= 10;
                }

                // 无法获取版本信息时，也认为是较新的Windows
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测操作系统版本时出错: {ex.Message}");
                // 出错时默认使用新的方法
                return true;
            }
        }

        /// <summary>
        /// 保留屏幕区域
        /// </summary>
        /// <param name="left">左边界</param>
        /// <param name="top">上边界</param>
        /// <param name="right">右边界</param>
        /// <param name="bottom">下边界</param>
        /// <returns>是否成功保留区域</returns>
        public bool ReserveArea(int left, int top, int right, int bottom)
        {
            // 如果已经保留了区域，先恢复
            if (_isAreaReserved)
            {
                _ = RestoreWorkArea();
            }

            // 保存原始工作区
            if (!GetWorkArea(out _originalWorkArea))
            {
                Debug.WriteLine("获取原始工作区失败");
                return false;
            }

            // 计算新的工作区
            RECT newWorkArea = new(
                left,
                top,
                right,
                bottom);

            bool result;

            // 根据系统版本选择不同的实现方式
            if (_isWindows10OrLater)
            {
                // Windows 10+使用AppBar
                result = RegisterAppBar(newWorkArea);

                // 保存设置，以便重新注册
                _lastAppBarRect = newWorkArea;
            }
            else
            {
                // 旧版Windows使用SystemParametersInfo
                result = SystemParametersInfo(
                    SPI_SETWORKAREA,
                    0,
                    ref newWorkArea,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }

            if (result)
            {
                _isAreaReserved = true;
                IsAreaReserved = true;
                Debug.WriteLine($"成功保留屏幕区域: {newWorkArea}");

                // 开始监视Explorer
                StartExplorerWatcher();
            }
            else
            {
                Debug.WriteLine($"保留屏幕区域失败: {Marshal.GetLastWin32Error()}");
            }

            return result;
        }

        /// <summary>
        /// 在屏幕侧边保留指定宽度的区域
        /// </summary>
        /// <param name="sideWidth">要保留的宽度（像素）</param>
        /// <param name="position">保留位置（左、右、上、下）</param>
        /// <returns>是否成功保留区域</returns>
        public bool ReserveAreaOnSide(int sideWidth, DockPosition position)
        {
            // 获取当前屏幕尺寸
            if (!GetWorkArea(out RECT currentWorkArea))
            {
                return false;
            }

            RECT newWorkArea = currentWorkArea;
            int appBarEdge = -1;

            // 根据位置调整工作区
            switch (position)
            {
                case DockPosition.Left:
                    newWorkArea.Left += sideWidth;
                    appBarEdge = ABE_LEFT;
                    break;
                case DockPosition.Right:
                    newWorkArea.Right -= sideWidth;
                    appBarEdge = ABE_RIGHT;
                    break;
                case DockPosition.Top:
                    newWorkArea.Top += sideWidth;
                    appBarEdge = ABE_TOP;
                    break;
                case DockPosition.Bottom:
                    newWorkArea.Bottom -= sideWidth;
                    appBarEdge = ABE_BOTTOM;
                    break;
            }

            // 如果使用AppBar，保存边缘信息
            if (_isWindows10OrLater)
            {
                // 计算AppBar的大小
                RECT appBarRect = CalculateAppBarRect(currentWorkArea, position, sideWidth);

                // 保存设置，以便重新注册
                _lastAppBarRect = appBarRect;
                _lastAppBarEdge = appBarEdge;

                bool registerAppBarSuccess = RegisterAppBar(appBarRect, appBarEdge);
                if (!registerAppBarSuccess)
                {
                    _isAreaReserved = true;
                    IsAreaReserved = true;
                    registerAppBarSuccess = RestoreWorkArea();
                }
                _isAreaReserved = registerAppBarSuccess;
                IsAreaReserved = registerAppBarSuccess;

                // 开始监视Explorer
                StartExplorerWatcher();

                return registerAppBarSuccess;
            }

            // 否则使用老方法
            return ReserveArea(
                newWorkArea.Left,
                newWorkArea.Top,
                newWorkArea.Right,
                newWorkArea.Bottom);
        }

        /// <summary>
        /// 开始监视Explorer进程
        /// </summary>
        private void StartExplorerWatcher()
        {
            if (_isMonitoringExplorer)
            {
                return;
            }

            _isMonitoringExplorer = true;

            // 取消之前的任务（如果有）
            _explorerWatcherCts?.Cancel();
            _explorerWatcherCts = new CancellationTokenSource();

            // 启动后台任务监视Explorer进程
            _ = Task.Run(async () =>
            {
                bool wasExplorerRunning = IsExplorerRunning();

                while (!_explorerWatcherCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        bool isExplorerRunning = IsExplorerRunning();

                        // 如果Explorer从不运行状态恢复到运行状态，重新注册AppBar
                        if (!wasExplorerRunning && isExplorerRunning)
                        {
                            Debug.WriteLine("检测到Explorer重启，重新注册AppBar");

                            // 等待Explorer完全启动
                            await Task.Delay(2000, _explorerWatcherCts.Token);

                            // 在UI线程上重新注册AppBar
                            await Task.Delay(100);  // 确保UI线程可用

                            ReregisterAppBar();
                        }

                        wasExplorerRunning = isExplorerRunning;

                        // 定期检查
                        await Task.Delay(EXPLORER_CHECK_INTERVAL, _explorerWatcherCts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"监视Explorer时出错: {ex.Message}");
                    }
                }

                _isMonitoringExplorer = false;

            }, _explorerWatcherCts.Token);
        }

        /// <summary>
        /// 检查Explorer是否正在运行
        /// </summary>
        private bool IsExplorerRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("explorer");
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查Explorer进程时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重新注册AppBar
        /// </summary>
        private void ReregisterAppBar()
        {
            if (!_isAreaReserved || !_isWindows10OrLater)
            {
                return;
            }

            Debug.WriteLine("尝试重新注册AppBar");

            try
            {
                // 尝试取消注册现有AppBar（如果存在）
                _ = UnregisterAppBar();

                // 延迟一段时间，让系统处理
                Thread.Sleep(100);

                // 重新注册AppBar
                if (_lastAppBarRect.Right != 0 && _lastAppBarRect.Bottom != 0)
                {
                    bool success = RegisterAppBar(_lastAppBarRect, _lastAppBarEdge);
                    Debug.WriteLine($"AppBar重新注册{(success ? "成功" : "失败")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重新注册AppBar时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据边缘位置和宽度计算AppBar的矩形范围
        /// </summary>
        private RECT CalculateAppBarRect(RECT workArea, DockPosition position, int size)
        {
            RECT rect = workArea;

            switch (position)
            {
                case DockPosition.Left:
                    rect.Right = rect.Left + size;
                    break;
                case DockPosition.Right:
                    rect.Left = rect.Right - size;
                    break;
                case DockPosition.Top:
                    rect.Bottom = rect.Top + size;
                    break;
                case DockPosition.Bottom:
                    rect.Top = rect.Bottom - size;
                    break;
            }

            return rect;
        }

        /// <summary>
        /// 注册AppBar
        /// </summary>
        private bool RegisterAppBar(RECT rect, int edge = -1)
        {
            // 检查是否已经注册
            if (_isAppBarRegistered)
            {
                _ = UnregisterAppBar();
            }

            // 目前我们没有真实的窗口句柄，使用桌面窗口句柄作为替代
            // 注意：在实际应用中，你应该使用真正的Avalonia窗口句柄
            _appBarHwnd = GetDesktopWindow();

            // 初始化AppBar数据
            _appBarData = new APPBARDATA(_appBarHwnd)
            {
                uCallbackMessage = 0x8001, // 自定义消息
                uEdge = edge >= 0 ? edge : ABE_RIGHT // 如果没有指定边缘，默认使用右边
            };

            // 注册AppBar
            if (SHAppBarMessage(ABM_NEW, ref _appBarData) == IntPtr.Zero)
            {
                Debug.WriteLine("注册AppBar失败");
                return false;
            }

            // 设置AppBar位置和大小
            _appBarData.rc = rect;

            // 查询位置
            _ = SHAppBarMessage(ABM_QUERYPOS, ref _appBarData);

            // 设置位置
            _ = SHAppBarMessage(ABM_SETPOS, ref _appBarData);

            _isAppBarRegistered = true;
            return true;
        }

        /// <summary>
        /// 注销AppBar
        /// </summary>
        private bool UnregisterAppBar()
        {
            if (!_isAppBarRegistered)
            {
                return true;
            }

            // 移除AppBar
            bool result = SHAppBarMessage(ABM_REMOVE, ref _appBarData) != IntPtr.Zero;

            if (result)
            {
                _isAppBarRegistered = false;
            }

            return result;
        }

        /// <summary>
        /// 获取桌面窗口句柄
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// 恢复工作区到原始状态
        /// </summary>
        /// <returns>是否成功恢复工作区</returns>
        public bool RestoreWorkArea()
        {
            if (!_isAreaReserved)
            {
                // 如果未保留区域，则无需恢复
                return true;
            }

            // 停止监视Explorer
            _explorerWatcherCts?.Cancel();
            _isMonitoringExplorer = false;

            bool result;

            if (_isWindows10OrLater)
            {
                // Windows 10+取消注册AppBar
                result = UnregisterAppBar();
            }
            else
            {
                // 旧版Windows恢复工作区
                result = SystemParametersInfo(
                    SPI_SETWORKAREA,
                    0,
                    ref _originalWorkArea,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }

            if (result)
            {
                _isAreaReserved = false;
                IsAreaReserved = false;
                Debug.WriteLine($"成功恢复原始工作区: {_originalWorkArea}");
            }
            else
            {
                Debug.WriteLine($"恢复原始工作区失败: {Marshal.GetLastWin32Error()}");
            }

            return result;
        }

        /// <summary>
        /// 获取当前工作区
        /// </summary>
        /// <param name="workArea">输出参数，当前工作区</param>
        /// <returns>是否成功获取工作区</returns>
        public bool GetWorkArea(out RECT workArea)
        {
            workArea = new RECT();
            return SystemParametersInfo(
                SPI_GETWORKAREA,
                0,
                ref workArea,
                0);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 确保恢复工作区
            if (_isAreaReserved)
            {
                _ = RestoreWorkArea();
            }

            // 取消后台任务
            _explorerWatcherCts?.Cancel();
            _explorerWatcherCts?.Dispose();
        }
    }

    /// <summary>
    /// 指定保留区域的位置
    /// </summary>
    public enum DockPosition
    {
        Left,
        Right,
        Top,
        Bottom
    }
}