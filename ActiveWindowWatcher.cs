using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Diagnostics
{

    /// <summary>Provides access to currently active window and triggers an event when it changes. This applies to all 
    /// application running on the system, not just the host application.</summary>
    /// <remarks>To use the ActiveWindowWatcher component, add the ActiveWindowWatcher.cs to your project. The 
    /// component will be available in the Form Designer and can added to a form or other control. The Enabled 
    /// property is set to false by default. Set this to true at design-time or run-time and the component will 
    /// automatically respond to the appropriate Windows events and update the respective properties to correspond 
    /// with the currently active window and will call the Changed event (if EnableRaisingEvents is true).</remarks> 
    [System.ComponentModel.DesignerCategory(""), DefaultProperty("Enabled")]
    public class ActiveWindowWatcher : Component
    {

        #region Private members

        private bool enabled;
        private int processId;
        private int threadId;
        private Process process;
        private WinEventDelegate dele = null;
        private IntPtr m_hhook = IntPtr.Zero;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int ProcessId);

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            update();
        }

        private void update()
        {
            processId = 0;
            threadId = 0;

            IntPtr handle = IntPtr.Zero;
            handle = GetForegroundWindow();
            threadId = GetWindowThreadProcessId(handle, out processId);
            process = Process.GetProcessById(processId);

            if (EnableRaisingEvents)
                Changed(this, new EventArgs());
        }

        private void hook()
        {
            dele = new WinEventDelegate(WinEventProc);
            m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0,
                WINEVENT_OUTOFCONTEXT);

            // Check for error
            if (m_hhook == IntPtr.Zero)
                throw new WinEventHookException("Error when setting Windows event hook.");
        }

        private void unhook()
        {
            UnhookWinEvent(m_hhook);
            dele = null;
        }

        #endregion

        #region Public members

        /// <summary>Gets the ID of the process associated with the currently active window.</summary>
        [Category("Read-Only Properties"), DefaultValue(0)]
        [Description("The ID of the process associated with the currently active window.")]
        public int ProcessId
        {
            get
            {
                return processId;
            }
        }

        /// <summary>Gets the ID of the thread associated with the currently active window.</summary>
        [Category("Read-Only Properties"), DefaultValue(0)]
        [Description("The ID of the thread associated with the currently active window.")]
        public int ThreadId
        {
            get
            {
                return threadId;
            }
        }

        /// <summary>An object representing the process associated with the currently active window.</summary>
        [Category("Read-Only Properties"), DefaultValue(null)]
        [Description("An object representing the process associated with the currently active window.")]
        public Process Process
        {
            get
            {
                return process;
            }
        }

        /// <summary>Gets a value equivalent to calling Process.MainWindowTitle.</summary>
        [Category("Read-Only Properties"), DefaultValue(null)]
        [Description("The title of currently active window.")]
        public string WindowTitle
        {
            get
            {
                return process != null ? process.MainWindowTitle : "";
            }
        }

        /// <summary>Gets a value equivalent to calling Process.MainModule.ModuleName.</summary>
        [Category("Read-Only Properties"), DefaultValue(null)]
        [Description("The name of the module (executable) associated with the currenctly active window.")]
        public string ModuleName
        {
            get
            {
                return process != null ? process.MainModule.ModuleName : "";
            }
        }


        /// <summary>Gets a value indicating whether the ActiveWindowWatcher component is enabled.</summary>
        /// <remarks>When set to <b>true</b>, the ActiveWindowWatcher component will update the <see cref="ProcessId"/>,
        /// <see cref="threadId"/> and <see cref=">Process"/> properties in response to changes to the active window 
        /// and will trigger the <see cref="Changed"/> event (unless <see cref="EnableRaisingEvents"/> property is
        /// <b>false</b>.</remarks>
        [Category("Behavior"), DefaultValue(false)]
        [Description("Indicates whether the component is enabled or not.")]
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (DesignMode) {
                    enabled = value;
                } else {
                    if (enabled && !value)
                        Disable();
                    if (!enabled && value)
                        Enable();
                }
            }
        }

        /// <summary>Gets or a set a value indicating whether the ActiveWindowWatcher component will raise events.</summary>
        /// <remarks>
        /// <para>Setting Enabled to <b>true</b> is the same as calling <see cref="Enable"/>, while setting Enabled to
        /// <b>false</b> is the same as calling <see cref="Disable"/>.</para>
        /// <para>If set to <b>false</b>, the component will not raise any events, however the
        /// <see cref="ProcessId"/>, <see cref="threadId"/> and <see cref=">Process"/> properties will continue
        /// to update when the active window changes.</para></remarks>
        [Category("Misc"), DefaultValue(true)]
        [Description("Indicates whether or not the component will trigger events.")]
        public bool EnableRaisingEvents { get; set; }

        /// <summary>Occurs when the currently active window changes.</summary>
        [Description("Occurs when the currently active window changes.")]
        public event EventHandler Changed;

        /// <summary>Intializes a new instance of the ActiveWindowWatcher class.</summary>
        public ActiveWindowWatcher()
        {
            enabled = false;
            EnableRaisingEvents = true;
            processId = 0;
            threadId = 0;
            process = null;
        }

        /// <summary>Enables the ActiveWindowWatcher component.</summary>
        /// <remarks>When enabled, the <see cref="ProcessId"/>,
        /// <see cref="threadId"/> and <see cref=">Process"/> properties will update to reflect the state of currently
        /// active window and the <see cref="Changed"/> event  will occurr whenever the active window is changed
        /// (unless <see cref="EnableRaisingEvents"/> property is <b>false</b>.</remarks>
        public void Enable()
        {
            hook();
            update();
            enabled = true;
        }

        /// <summary>Disables the ActiveWindowWatcher component.</summary>
        /// <remarks>When disabled, the <see cref="ProcessId"/>, <see cref="threadId"/> and <see cref=">Process"/>
        /// properties will remaining unchanged, until the component is enabled.</remarks>
        public void Disable()
        {
            unhook();
            enabled = false;

        }

        protected override void Dispose(bool disposing)
        {
            Disable();
            base.Dispose(disposing);
        }

        #endregion
    }

    public class WinEventHookException : Exception
    {
        public WinEventHookException() : base() { }
        public WinEventHookException(string message) : base(message) { }

    }

}
