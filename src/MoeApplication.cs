// This sample is based on example_glfw_opengl3
// https://github.com/ocornut/imgui/tree/704ab1114aa54858b690711554cf3312fbbcc3fc/examples/example_glfw_opengl3
// We derive from OpenTK's NativeWindow rather than GameWindow for the sake of simplicity.
// Some of the direct GLFW access could be replaced with higher level calls or removed if GameWindow was used instead.
//
// Things that are quirks of using Biohazrd and could stand to be improved are marked with `BIOQUIRK`
using Mochi.DearImGui;
using Mochi.DearImGui.OpenTK;
using MoeTag.Debug;
using MoeTag.UI;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

// Start in the app's base directory to avoid polluting imgui.ini and to make fonts accessible
Environment.CurrentDirectory = AppContext.BaseDirectory;

// Setup window
NativeWindowSettings nativeWindowSettings = new()
{
    Size = new(1280, 720),
    Title = "MoeTag " + MoeApplication.V_MAJOR + "." + MoeApplication.V_MINOR + "." + MoeApplication.V_PATCH,
};

static WindowIcon CreateWindowIcon()
{
    var image = (Image<Rgba32>)SixLabors.ImageSharp.Image.Load(Configuration.Default, "./resources/icon256.png");

    //Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
    var pixels = new List<byte>(4 * image.Width * image.Height);

    for (int y = 0; y < image.Height; y++)
    {
        for (int x = 0; x < image.Width; x++)
        {
            pixels.Add(image[x, y].R);
            pixels.Add(image[x, y].G);
            pixels.Add(image[x, y].B);
            pixels.Add(image[x, y].A);
        }
    }

    var windowIcon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(image.Width, image.Height, pixels.ToArray()));

    return windowIcon;
}
nativeWindowSettings.Icon = CreateWindowIcon();


// Decide GL+GLSL versions
string glslVersion;
//TODO: GLES support
if (OperatingSystem.IsMacOS())
{
    // GL 3.2 + GLSL 150
    glslVersion = "#version 150";
    nativeWindowSettings.APIVersion = new(3, 2);
    nativeWindowSettings.Profile = ContextProfile.Core; // 3.2+ only
    nativeWindowSettings.Flags = ContextFlags.ForwardCompatible; // Required on macOS
}
else
{
    // GL 3.0 + GLSL 130
    glslVersion = "#version 130";
    nativeWindowSettings.APIVersion = new(3, 0);
    nativeWindowSettings.Profile = ContextProfile.Any;
    //nativeWindowSettings.Profile = ContextProfile.Core; // 3.2+ only
    //nativeWindowSettings.Flags = ContextFlags.ForwardCompatible; // 3.0+ only
}

using MoeApplication window = new(nativeWindowSettings, glslVersion);
window.Run();

internal unsafe sealed class MoeApplication : NativeWindow
{
    public static uint V_MAJOR = 0;
    public static uint V_MINOR = 1;
    public static uint V_PATCH = 2;

    private readonly string? GlslVersion;
    private readonly RendererBackend RendererBackend;
    private readonly PlatformBackend PlatformBackend;

    private readonly ImGuiIO* io;

    private bool _rendering;

    public static IntPtr HWND = IntPtr.Zero;

    MoeTagUI _moeTagUI;

    public MoeApplication(NativeWindowSettings nativeWindowSettings, string? glslVersion)
        : base(nativeWindowSettings)
    {
        // Operation NUKE ALL CULTURE
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        GlslVersion = glslVersion;
        Context.MakeCurrent();
        VSync = VSyncMode.On; // Enable vsync

        // Setup Dear ImGui context
        ImGui.CHECKVERSION();
        ImGui.CreateContext();
        io = ImGui.GetIO();
        io->ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard; // Enable Keyboard Controls
        //io->ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad; // Enable Gamepad Controls
        io->ConfigFlags |= ImGuiConfigFlags.DockingEnable; // Enable Docking
        io->ConfigFlags |= ImGuiConfigFlags.ViewportsEnable; // Enable Multi-Viewport / Platform Windows
        //io->ConfigViewportsNoAutoMerge = true;
        //io->ConfigViewportsNoTaskBarIcon = true;

        // Setup Dear ImGui style
        //ImGui.StyleColorsDark();
        //ImGui.StyleColorsClassic();
        MoeDarkTheme.ApplyTheme();

        // When viewports are enabled we tweak WindowRounding/WindowBg so platform windows can look identical to regular ones.
        ImGuiStyle* style = ImGui.GetStyle();
        if (io->ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            style->WindowRounding = 0f;
            //BIOQUIRK: We should special-case this to make it more friendly. (https://github.com/MochiLibraries/Biohazrd/issues/139 would help here)
            style->Colors[(int)ImGuiCol.WindowBg].W = 1f;
        }

        // Setup Platform/Renderer backends
        PlatformBackend = new(this, true);
        RendererBackend = new(GlslVersion);

        Resize += (ResizeEventArgs e) =>
        {
            RenderFrame(e.Width, e.Height);
        };

        // Load Fonts
        // - If no fonts are loaded, dear imgui will use the default font. You can also load multiple fonts and use ImGui::PushFont()/PopFont() to select them.
        // - AddFontFromFileTTF() will return the ImFont* so you can store it if you need to select the font among multiple.
        // - If the file cannot be loaded, the function will return NULL. Please handle those errors in your application (e.g. use an assertion, or display an error and quit).
        // - The fonts will be rasterized at a given size (w/ oversampling) and stored into a texture when calling ImFontAtlas::Build()/GetTexDataAsXXXX(), which ImGui_ImplXXXX_NewFrame below will call.
        // - Read 'docs/FONTS.md' for more instructions and details.
        // - Remember that in C# if you want to include a backslash \ in a string literal you need to write a double backslash \\ !

        ImFont* font = io->Fonts->AddFontFromFileTTF(Path.Join("resources", "Roboto-Regular.ttf"), 18.0f);
        Debug.Assert(font != null);

        //ImFont* font = io->Fonts->AddFontFromFileTTF(@"C:\Windows\Fonts\ArialUni.ttf", 18.0f, null, io->Fonts->GetGlyphRangesJapanese());
        //Debug.Assert(font != null);

        _moeTagUI = new MoeTagUI();

        _rendering = false;
    }

    public void Run()
    {
        // Main loop
        while (!GLFW.WindowShouldClose(WindowPtr))
        {
            // Poll and handle events (inputs, window resize, etc.)
            // You can read the io.WantCaptureMouse, io.WantCaptureKeyboard flags to tell if dear imgui wants to use your inputs.
            // - When io.WantCaptureMouse is true, do not dispatch mouse input data to your main application.
            // - When io.WantCaptureKeyboard is true, do not dispatch keyboard input data to your main application.
            // Generally you may always pass all inputs to dear imgui, and hide them from your application based on those two flags.
            ProcessEvents();
            // Render the frame out
            GLFW.GetFramebufferSize(WindowPtr, out int displayW, out int displayH);
            RenderFrame(displayW, displayH);
        }
    }

    private void RenderFrame(int displayW, int displayH)
    {
        if (!_rendering)
        {
            _rendering = true; // Lock

            Vector3 clearColor = new(0.45f, 0.55f, 0.6f);

            // Start the Dear ImGui frame
            RendererBackend.NewFrame();
            PlatformBackend.NewFrame();
            ImGui.NewFrame();

            // Render
            _moeTagUI.DrawUI(ClientSize);

            // Rendering
            {
                ImGui.Render();
                GL.Viewport(0, 0, displayW, displayH);
                GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                RendererBackend.RenderDrawData(ImGui.GetDrawData());

                // Update and Render additional Platform Windows
                // (Platform functions may change the current OpenGL context, so we save/restore it to make it easier to paste this code elsewhere.
                //  For this specific demo app we could also call glfwMakeContextCurrent(window) directly)
                if (io->ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                {
                    Window* backupCurrentContext = GLFW.GetCurrentContext();
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                    GLFW.MakeContextCurrent(backupCurrentContext);
                }

                GLFW.SwapBuffers(WindowPtr);
            }

            _rendering = false; // Unlock
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _moeTagUI.SaveData();
        base.OnClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        // Cleanup
        // The managed backends do not have finalizers because it isn't safe for them to be disposed of in an unpredictable order relative to this class
        // As such we dispose of them regardless of `disposing`.
        RendererBackend.Dispose();
        PlatformBackend.Dispose();
        ImGui.DestroyContext();

        _moeTagUI.Dispose();

        base.Dispose(disposing);
    }
}
