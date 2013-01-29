using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Diagnostics
{

    /// <summary>Provides access to a <see cref="Process"/> object representing the currently active window
    /// (system-wide).</summary>
    [System.ComponentModel.DesignerCategory("")]
    public class ActiveWindowWatcher : Component
    {

        #region Private members

        private bool enabled;
        private int processId;
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
            ThreadId = 0;

            IntPtr handle = IntPtr.Zero;
            handle = GetForegroundWindow();
            ThreadId = GetWindowThreadProcessId(handle, out processId);
            Process = Process.GetProcessById(processId);

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
        [Browsable(false)]
        public int ProcessId
        {
            get
            {
                return processId;
            }
        }

        /// <summary>Gets the ID of the thread associated with the currently active window.</summary>
        [Browsable(false)]
        public int ThreadId { get; private set; }

        /// <summary>Gets an object representing the process associated with the currently active window.</summary>
        [Browsable(false)]
        public Process Process { get; private set; }

        /// <summary>Gets a value indicating whether the ActiveWindowWatcher component is enabled.</summary>
        /// <remarks>When set to <b>true</b>, the ActiveWindowWatcher component will update the <see cref="ProcessId"/>,
        /// <see cref="ThreadId"/> and <see cref=">Process"/> properties in response to changes to the active window 
        /// and will trigger the <see cref="Changed"/> event (unless <see cref="EnableRaisingEvents"/> property is
        /// <b>false</b>.</remarks>
        [DefaultValue(false)]
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
        /// <see cref="ProcessId"/>, <see cref="ThreadId"/> and <see cref=">Process"/> properties will continue
        /// to update when the active window changes.</para></remarks>
        [DefaultValue(true)]
        public bool EnableRaisingEvents { get; set; }

        /// <summary>Occurs when the currently active window changes.</summary>
        public event EventHandler Changed;

        /// <summary>Intializes a new instance of the ActiveWindowWatcher class.</summary>
        public ActiveWindowWatcher()
        {
            enabled = false;
            EnableRaisingEvents = true;
            processId = 0;
            ThreadId = 0;
            Process = null;
        }

        /// <summary>Enables the ActiveWindowWatcher component.</summary>
        /// <remarks>When enabled, the <see cref="ProcessId"/>,
        /// <see cref="ThreadId"/> and <see cref=">Process"/> properties will update to reflect the state of currently
        /// active window and the <see cref="Changed"/> event  will occurr whenever the active window is changed
        /// (unless <see cref="EnableRaisingEvents"/> property is <b>false</b>.</remarks>
        public void Enable()
        {
            hook();
            update();
            enabled = true;
        }

        /// <summary>Disables the ActiveWindowWatcher component.</summary>
        /// <remarks>When disabled, the <see cref="ProcessId"/>, <see cref="ThreadId"/> and <see cref=">Process"/>
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
