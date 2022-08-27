using MoeTag.Debug;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace MoeTag.Extern
{
    public class MPV : IDisposable
    {
        public double PlayPercent;

        private const int MpvFormatString = 1;
        private IntPtr _libMpvDll;

        private IntPtr _mpvHandle;
        private IntPtr _renderContext;
        private IntPtr _eventHandle;

        public bool Playing;

        private Task _eventThread;
        private CancellationTokenSource _eventThreadTokenSource;

        [DllImport("libdl.so")]
        internal static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        internal static extern IntPtr dlsym(IntPtr handle, string symbol);


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvCreate();
        private MpvCreate _mpvCreate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvInitialize(IntPtr mpvHandle);
        private MpvInitialize _mpvInitialize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvCommand(IntPtr mpvHandle, IntPtr strings);
        private MpvCommand _mpvCommand;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvDestroy(IntPtr mpvHandle);
        private MpvDestroy _mpvDestroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvTerminateDestroy(IntPtr mpvHandle);
        private MpvTerminateDestroy _mpvTerminateDestroy;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOption(IntPtr mpvHandle, byte[] name, int format, ref long data);
        private MpvSetOption _mpvSetOption;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetOptionString(IntPtr mpvHandle, byte[] name, byte[] value);
        private MpvSetOptionString _mpvSetOptionString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvGetPropertystring(IntPtr mpvHandle, byte[] name, int format, ref IntPtr data);
        private MpvGetPropertystring _mpvGetPropertyString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvSetProperty(IntPtr mpvHandle, byte[] name, int format, ref byte[] data);
        private MpvSetProperty _mpvSetProperty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvObserveProperty(IntPtr mpvHandle, ulong reply_userdata, byte[] name,  int format);
        private MpvObserveProperty _mpvObserveProperty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvRenderContextCreate(ref IntPtr context, IntPtr mpvHandler, IntPtr parameters);
        private MpvRenderContextCreate _mpvRenderContextCreate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvRenderContextRender(IntPtr context, IntPtr parameters);
        private MpvRenderContextRender _mpvRenderContextRender;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MpvFree(IntPtr data);
        private MpvFree _mpvFree;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr OpenGlRenderContextCallback(IntPtr ctx, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MpvRenderContextUpdate(IntPtr context);
        private MpvRenderContextUpdate _mpvRenderContextUpdate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvCreateClient(IntPtr context, IntPtr name);
        private MpvCreateClient _mpvCreateClient;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvWaitEvent(IntPtr mpvHandle, double timeout);
        private MpvWaitEvent _mpvWaitEvent;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr MpvRequestLogMessages(IntPtr mpvHandle, byte[] level);
        private MpvRequestLogMessages _mpvRequestLogMessages;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MpvRenderContextFree(IntPtr mpvHandle);
        private MpvRenderContextFree _mpvRenderContextFree;

        // DATA TYPES

        public enum MpvRenderParamType
        {
            Invalid = 0,
            ApiType = 1,
            InitParams = 2,
            Fbo = 3,
            FlipY = 4,
            Depth = 5,
            IccProfile = 6,
            AmbientLight = 7,
            X11Display = 8,
            WlDisplay = 9,
            AdvancedControl = 10,
            NextFrameInfo = 11,
            BlockForTargetTime = 12,
            SkipRendering = 13,
            DrmDisplay = 14,
            DrmDrawSurfaceSize = 15,
            DrmDisplayV2 = 15
        }

        public enum MpvEventId
        {
            None = 0,
            Shutdown = 1,
            LogMessage = 2,
            GetPropertyReply = 3,
            SetPropertyReply = 4,
            CommandReply = 5,
            StartFile = 6,
            EndFile = 7,
            FileLoaded = 8,
            TracksChanged = 9,
            TrackSwitched = 10,
            Idle = 11,
            Pause = 12,
            Unpause = 13,
            Tick = 14,
            ScriptInputDispatch = 15,
            ClientMessage = 16,
            VideoReconfig = 17,
            AudioReconfig = 18,
            MetadataUpdate = 19,
            Seek = 20,
            PlaybackRestart = 21,
            PropertyChange = 22,
            ChapterChange = 23,
            QueueOverflow = 24
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mpv_event
        {
            public MpvEventId event_id;
            public int error;
            public UInt64 reply_userdata;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvRenderParam
        {
            public MpvRenderParamType type;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvOpenGlInitParams
        {
            public OpenGlRenderContextCallback get_proc_address;
            public IntPtr get_proc_address_ctx;
            public IntPtr extra_exts;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvFboParams
        {
            public Int32 fbo;
            public Int32 w;
            public Int32 h;
            public Int32 internal_format;
        }

        public enum mpv_log_level
        {
            MPV_LOG_LEVEL_NONE = 0,    /// "no"    - disable absolutely all messages
            MPV_LOG_LEVEL_FATAL = 10,   /// "fatal" - critical/aborting errors
            MPV_LOG_LEVEL_ERROR = 20,   /// "error" - simple errors
            MPV_LOG_LEVEL_WARN = 30,   /// "warn"  - possible problems
            MPV_LOG_LEVEL_INFO = 40,   /// "info"  - informational message
            MPV_LOG_LEVEL_V = 50,   /// "v"     - noisy informational message
            MPV_LOG_LEVEL_DEBUG = 60,   /// "debug" - very noisy technical information
            MPV_LOG_LEVEL_TRACE = 70,   /// "trace" - extremely noisy
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mpv_event_log_message
        {
            public string prefix;
            public string level;
            public string text;
            public mpv_log_level log_level;
        }

        // Functions

        public MPV()
        {
            try
            {
                _libMpvDll = LoadLibrary("mpv-1.dll"); // The dll is included in the DEV builds by lachs0r: https://mpv.srsfckn.biz/
                _mpvCreate = (MpvCreate)GetDllType(typeof(MpvCreate), "mpv_create");
                _mpvInitialize = (MpvInitialize)GetDllType(typeof(MpvInitialize), "mpv_initialize");
                _mpvRenderContextCreate = (MpvRenderContextCreate)GetDllType(typeof(MpvRenderContextCreate), "mpv_render_context_create");
                _mpvDestroy = (MpvDestroy)GetDllType(typeof(MpvDestroy), "mpv_destroy");
                _mpvTerminateDestroy = (MpvTerminateDestroy)GetDllType(typeof(MpvTerminateDestroy), "mpv_terminate_destroy");
                _mpvCommand = (MpvCommand)GetDllType(typeof(MpvCommand), "mpv_command");
                _mpvSetOption = (MpvSetOption)GetDllType(typeof(MpvSetOption), "mpv_set_option");
                _mpvSetOptionString = (MpvSetOptionString)GetDllType(typeof(MpvSetOptionString), "mpv_set_option_string");
                _mpvGetPropertyString = (MpvGetPropertystring)GetDllType(typeof(MpvGetPropertystring), "mpv_get_property");
                _mpvSetProperty = (MpvSetProperty)GetDllType(typeof(MpvSetProperty), "mpv_set_property");
                _mpvSetProperty = (MpvSetProperty)GetDllType(typeof(MpvSetProperty), "mpv_set_property");
                _mpvObserveProperty = (MpvObserveProperty)GetDllType(typeof(MpvObserveProperty), "mpv_observe_property");
                _mpvFree = (MpvFree)GetDllType(typeof(MpvFree), "mpv_free");
                _mpvRenderContextRender = (MpvRenderContextRender)GetDllType(typeof(MpvRenderContextRender), "mpv_render_context_render");
                _mpvRenderContextUpdate = (MpvRenderContextUpdate)GetDllType(typeof(MpvRenderContextUpdate), "mpv_render_context_update");
                _mpvCreateClient = (MpvCreateClient)GetDllType(typeof(MpvCreateClient), "mpv_create_client");
                _mpvWaitEvent = (MpvWaitEvent)GetDllType(typeof(MpvWaitEvent), "mpv_wait_event");
                _mpvRequestLogMessages = (MpvRequestLogMessages)GetDllType(typeof(MpvRequestLogMessages), "mpv_request_log_messages");
                _mpvRenderContextFree = (MpvRenderContextFree)GetDllType(typeof(MpvRenderContextFree), "mpv_render_context_free");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to load libaries: " + ex.Message);
                return;
            }

            Setup();

            _eventThreadTokenSource = new CancellationTokenSource();
            CancellationToken ct = _eventThreadTokenSource.Token;
            _eventThread = Task.Run(() =>
            {
                while (_eventHandle != IntPtr.Zero)
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException e)
                    {
                        MoeLogger.Log(this, e.Message);
                        return;
                    }
                    Event();
                }
            }, _eventThreadTokenSource.Token);

            Playing = false;
        }

        private object GetDllType(Type type, string name)
        {
            IntPtr address = GetProcAddress(_libMpvDll, name);
            if (address != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer(address, type);
            return null;
        }

        public void Pause()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("yes");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        public void Play()
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            var bytes = GetUtf8Bytes("no");
            _mpvSetProperty(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref bytes);
        }

        public bool IsPaused()
        {
            if (_mpvHandle == IntPtr.Zero)
                return true;

            var lpBuffer = IntPtr.Zero;
            _mpvGetPropertyString(_mpvHandle, GetUtf8Bytes("pause"), MpvFormatString, ref lpBuffer);
            var isPaused = Marshal.PtrToStringAnsi(lpBuffer) == "yes";
            _mpvFree(lpBuffer);
            return isPaused;
        }

        public void SetTime(double value)
        {
            if (_mpvHandle == IntPtr.Zero)
                return;

            DoMpvCommand("seek", value.ToString(CultureInfo.InvariantCulture), "absolute-percent");
        }

        private static byte[] GetUtf8Bytes(string s)
        {
            return Encoding.UTF8.GetBytes(s + "\0");
        }
        public static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
        {
            int numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
            byteArrayPointers = new IntPtr[numberOfStrings];
            IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);
            for (int index = 0; index < arr.Length; index++)
            {
                var bytes = GetUtf8Bytes(arr[index]);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                byteArrayPointers[index] = unmanagedPointer;
            }
            Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
            return rootPointer;
        }
        private void DoMpvCommand(params string[] args)
        {
            IntPtr[] byteArrayPointers;
            var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out byteArrayPointers);
            _mpvCommand(_mpvHandle, mainPtr);
            foreach (var ptr in byteArrayPointers)
            {
                Marshal.FreeHGlobal(ptr);
            }
            Marshal.FreeHGlobal(mainPtr);
        }

        public unsafe void LoadFile(string path)
        {
            DoMpvCommand("loadfile", path);
            Play();
        }

        private unsafe void Setup()
        {
            if (_mpvHandle != IntPtr.Zero)
                _mpvDestroy(_mpvHandle);

            if (_libMpvDll == IntPtr.Zero)
                return;

            _mpvHandle = _mpvCreate.Invoke();
            if (_mpvHandle == IntPtr.Zero)
                return;

            _mpvSetOptionString(_mpvHandle, GetUtf8Bytes("keep-open"), GetUtf8Bytes("always"));
            _mpvSetOptionString(_mpvHandle, GetUtf8Bytes("osc"), GetUtf8Bytes("no"));
            _mpvSetOptionString(_mpvHandle, GetUtf8Bytes("loop-file"), GetUtf8Bytes("inf"));

            _mpvInitialize.Invoke(_mpvHandle);

            _eventHandle = _mpvCreateClient(_mpvHandle, Marshal.StringToHGlobalAnsi("moetag"));
            if (_eventHandle == IntPtr.Zero)
                return;

            _mpvObserveProperty(_eventHandle, 0, GetUtf8Bytes("percent-pos"), 5); // 5 = DOUBLE FORMAT


            _mpvRequestLogMessages(_eventHandle, GetUtf8Bytes("debug"));

            MpvOpenGlInitParams oglInitParams = new MpvOpenGlInitParams();

            IntPtr plugin(byte* name)
            {
                IntPtr result = GLFW.GetProcAddressRaw(name);
                string description = "No Error";
                if (GLFW.GetError(out description) != OpenTK.Windowing.GraphicsLibraryFramework.ErrorCode.NoError)
                {
                    Console.Error.WriteLine("ProcAddressError: " + description);
                }
                MoeLogger.Log(this, "Using Address: " + result.ToInt64());
                return result;
            }
            oglInitParams.get_proc_address = (ctx, name) => plugin((byte*)name);
            oglInitParams.get_proc_address_ctx = IntPtr.Zero;
            oglInitParams.extra_exts = IntPtr.Zero;

            IntPtr ac = Marshal.AllocCoTaskMem(sizeof(Int32));
            Marshal.WriteInt32(ac, 1);

            var size = Marshal.SizeOf<MpvOpenGlInitParams>();
            var oglInitParamsBuf = new byte[size];

            fixed (byte* arrPtr = oglInitParamsBuf)
            {
                IntPtr oglInitParamsPtr = new IntPtr(arrPtr);
                Marshal.StructureToPtr(oglInitParams, oglInitParamsPtr, true);

                MpvRenderParam* parameters = stackalloc MpvRenderParam[4];

                parameters[0].type = MpvRenderParamType.ApiType;
                parameters[0].data = Marshal.StringToHGlobalAnsi("opengl");

                parameters[1].type = MpvRenderParamType.InitParams;
                parameters[1].data = oglInitParamsPtr;

                parameters[2].type = MpvRenderParamType.AdvancedControl;
                parameters[2].data = ac;

                parameters[3].type = MpvRenderParamType.Invalid;
                parameters[3].data = IntPtr.Zero;

                var renderParamSize = Marshal.SizeOf<MpvRenderParam>();

                var paramBuf = new byte[renderParamSize * 4];
                fixed (byte* paramBufPtr = paramBuf)
                {
                    IntPtr param1Ptr = new IntPtr(paramBufPtr);
                    Marshal.StructureToPtr(parameters[0], param1Ptr, true);

                    IntPtr param2Ptr = new IntPtr(paramBufPtr + renderParamSize);
                    Marshal.StructureToPtr(parameters[1], param2Ptr, true);

                    IntPtr param3Ptr = new IntPtr(paramBufPtr + renderParamSize + renderParamSize);
                    Marshal.StructureToPtr(parameters[2], param3Ptr, true);

                    IntPtr param4Ptr = new IntPtr(paramBufPtr + renderParamSize + renderParamSize + renderParamSize);
                    Marshal.StructureToPtr(parameters[3], param4Ptr, true);

                    _renderContext = new IntPtr(0);
                    _mpvRenderContextCreate(ref _renderContext, _mpvHandle, param1Ptr);
                }
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        unsafe struct mpv_event_property
        {
            /**
             * Name of the property.
             */
            public IntPtr name;
            /**
             * Format of the data field in the same struct. See enum mpv_format.
             * This is always the same format as the requested format, except when
             * the property could not be retrieved (unavailable, or an error happened),
             * in which case the format is MPV_FORMAT_NONE.
             */
            public int format;
            /**
             * Received property value. Depends on the format. This is like the
             * pointer argument passed to mpv_get_property().
             *
             * For example, for MPV_FORMAT_STRING you get the string with:
             *
             *    char *value = *(char **)(event_property->data);
             *
             * Note that this is set to NULL if retrieving the property failed (the
             * format will be MPV_FORMAT_NONE).
             */
            public IntPtr data;
        }


        private unsafe void Event()
        {
            if (_eventHandle == IntPtr.Zero) { return; }
            IntPtr ptr = _mpvWaitEvent(_eventHandle, -1);
            if (ptr != IntPtr.Zero)
            {
                object? evtRet = Marshal.PtrToStructure(ptr, typeof(mpv_event));
                if (evtRet != null) {
                    mpv_event evt = (mpv_event)evtRet;
                    MoeLogger.Log(this, "GOT EVENT: " + evt.event_id);
                    switch (evt.event_id)
                    {
                        case MpvEventId.PropertyChange:
                            IntPtr iDE = evt.data;
                            object? evtLogRetE = Marshal.PtrToStructure(iDE, typeof(mpv_event_property));
                            if (evtLogRetE != null)
                            {
                                mpv_event_property dataE = (mpv_event_property)evtLogRetE;
                                if (dataE.data != IntPtr.Zero)
                                {
                                    double* d = (double*)dataE.data.ToPointer();
                                    PlayPercent = *d;
                                }
                            }
                            break;
                        case MpvEventId.Shutdown:
                            MoeLogger.Log(this, "Shutdown requested!");
                            _mpvDestroy(_eventHandle);
                            _eventThreadTokenSource.Cancel();
                            break;
                        case MpvEventId.LogMessage:
                            IntPtr iD = evt.data;
                            object? evtLogRet = Marshal.PtrToStructure(iD, typeof(mpv_event_log_message));
                            if (evtLogRet != null) {
                                mpv_event_log_message data = (mpv_event_log_message)evtLogRet;
                                string tmx = data.text.Replace(Environment.NewLine, "");
                                string pfx = data.prefix.Replace(Environment.NewLine, "");
                                MoeLogger.Log(this, "[" + pfx + "] >" + tmx);
                            }
                            break;
                    }
                }
            }
        }

        public unsafe bool Update()
        {
            return (_mpvRenderContextUpdate(_renderContext) & 1) == 1;
        }

        public unsafe void Render(int w, int h, int fbo)
        {
            Playing = !IsPaused();
            if (!Playing) { return; }

            MpvFboParams fboParams = new MpvFboParams();
            fboParams.w = w;
            fboParams.h = h;
            fboParams.fbo = fbo;
            fboParams.internal_format = 0;

            var size = Marshal.SizeOf<MpvFboParams>();
            var oglInitParamsBuf = new byte[size];
            fixed (byte* arrPtr = oglInitParamsBuf)
            {
                IntPtr oglInitParamsPtr = new IntPtr(arrPtr);
                Marshal.StructureToPtr(fboParams, oglInitParamsPtr, true);

                MpvRenderParam* parameters = stackalloc MpvRenderParam[3];

                // FLIP Y
                var flp = Marshal.AllocCoTaskMem(sizeof(Int32));
                Marshal.WriteInt32(flp, 0);
                parameters[0].type = MpvRenderParamType.FlipY;
                parameters[0].data = flp;

                // Setup Structure FBO
                parameters[1].type = MpvRenderParamType.Fbo;
                parameters[1].data = oglInitParamsPtr;

                // Ending
                parameters[2].type = MpvRenderParamType.Invalid;
                parameters[2].data = IntPtr.Zero;

                var renderParamSize = Marshal.SizeOf<MpvRenderParam>();
                var paramBuf = new byte[renderParamSize * 3];
                fixed (byte* paramBufPtr = paramBuf)
                {
                    IntPtr param1Ptr = new IntPtr(paramBufPtr);
                    Marshal.StructureToPtr(parameters[0], param1Ptr, true);

                    IntPtr param2Ptr = new IntPtr(paramBufPtr + renderParamSize);
                    Marshal.StructureToPtr(parameters[1], param2Ptr, true);

                    IntPtr param3Ptr = new IntPtr(paramBufPtr + renderParamSize + renderParamSize);
                    Marshal.StructureToPtr(parameters[2], param3Ptr, true);

                    int result = _mpvRenderContextRender(_renderContext, param1Ptr);
                    if (result != 0)
                    {
                        Console.Error.WriteLine(result);
                    }
                }
            }
        }

        public void Dispose()
        {
            Pause();

            MoeLogger.Log(this, "Disposed Thread");

            if (_renderContext != IntPtr.Zero)
            {
                _mpvRenderContextFree(_renderContext);
                _renderContext = IntPtr.Zero;
            }
            MoeLogger.Log(this, "Disposed Render Context");

            if (_mpvHandle != IntPtr.Zero)
            {
                MoeLogger.Log(this, "Waiting for Termination...");
                _mpvTerminateDestroy(_mpvHandle);
                _mpvHandle = IntPtr.Zero;
            }
            MoeLogger.Log(this, "Disposed + Terminated MPV Handle");

            _libMpvDll = IntPtr.Zero;

            GC.SuppressFinalize(this);
        }
    }
}

