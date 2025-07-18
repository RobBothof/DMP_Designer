﻿using System;
using System.IO;
using System.Configuration;
using System.Numerics;
using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;
using System.Text;
using ImGuiNET;
// using System.IO.Ports;
using IniParser.Model;
using IniParser;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.CodeDom.Compiler;
// using AssetPrimitives;

namespace Designer
{

    public enum LineType : byte
    {
        None = 0,
        Straight = 1,
        QuadraticBezier = 2,
        CubicBezier = 3,
        CatmullRom = 4
    }

    public enum Acceleration : byte
    {
        Single = 0,
        Start = 1,
        Continue = 2,
        Stop = 3
    }

    public struct DrawInstruction
    {
        public UInt64 index;
        public LineType type;
        public Acceleration acceleration;
        public sbyte dirX;
        public sbyte dirY;
        public sbyte dirZ;
        public byte projection;
        public byte groupIndex;
        public byte groupSize;
        public Int64 startX;
        public Int64 startY;
        public Int64 startZ;
        public Int64 endX;
        public Int64 endY;
        public Int64 endZ;
        public Int64 deltaX;
        public Int64 deltaY;
        public Int64 deltaZ;
        public Int64 deltaXX;
        public Int64 deltaYY;
        public Int64 deltaZZ;
        public Int64 deltaXY;
        public Int64 deltaXZ;
        public Int64 deltaYZ;
        public Int64 deltaMax;
        public Int64 err;
        public Int64 errX;
        public Int64 errY;
        public Int64 errZ;
        public UInt64 steps;
    }

    // public struct DrawInstruction2D {
    //     public UInt64 index;
    //     public LineType type;
    //     public sbyte dir_x;
    //     public sbyte dir_y;
    //     public Int64 x_start;
    //     public Int64 y_start;
    //     public Int64 x_end;
    //     public Int64 y_end;
    //     public Int64 delta_x;
    //     public Int64 delta_y;
    //     public Int64 delta_xx;
    //     public Int64 delta_yy;
    //     public Int64 delta_xy;
    //     public Int64 err;
    //     public Int64 steps;
    // }

    public class Line
    {
        public Vector2[] lineData;
        public Vector3[] points;
        public Vector3[] worldPoints;
        public LineType type = LineType.Straight;
        public Acceleration acceleration = Acceleration.Single;
        public int layer;
        public uint vCount;
    }

    public class Dot
    {
        public Vector2 position;
        public RgbaFloat color;
        public float size;
        public int layer;
    }

    public static class Data
    {
        public const int stepsPerMM = 1280;
        public static Vector2 paperSize = Vector2.Zero;
        public const int rasterPixelsPerStep = 512;
        public const uint rasterWidth = 420 * (1280 / rasterPixelsPerStep); // depthmap uses 1 pixel per 20 steps for A2 paper size
        public const uint rasterHeight = 594 * (1280 / rasterPixelsPerStep); // keep texture size around 2GB: 420 x 594 x (1280/20) x (1280/20) = 1.021.870.080 * 2 bytes for uint16
        public static List<string> DebugConsole = new List<string>();
        public static ushort[] depthMap = new ushort[rasterWidth * rasterHeight];
        public static ushort[] shadowMap = new ushort[rasterWidth * rasterHeight];
        public static ConcurrentBag<Line> lines = new ConcurrentBag<Line>();
        public static ConcurrentBag<Dot> dots = new ConcurrentBag<Dot>();
    }

    struct DotVertex
    {
        public Vector2 Position;
        public RgbaFloat Color;
        public Vector2 UV;
        public float Layer;
    }

    struct LineVertex
    {
        public Vector2 Position;
        public RgbaFloat Color;
        public float Edge;
        public float Width;
        public float Layer;
    }

    struct DepthVertex
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public float Alpha;
    }

    struct ShadowVertex
    {
        public Vector2 Position;
        public Vector2 TexCoords;
        public float Alpha;
    }

    public interface IGenerator
    {
        void Generate(int seed, CancellationToken token = default);
    }

    public static class Program
    {

        // private static float R2 = 1.25f; // diameter (r*2) of the wheel on motor
        private static Sdl2Window _window;
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;

        //shared resources for viewport and camera
        private static DeviceBuffer _projectionBuffer;
        private static DeviceBuffer _cameraBuffer;
        private static DeviceBuffer _rotationBuffer;
        private static DeviceBuffer _translationBuffer;
        private static ResourceSet _viewportResourceSet;

        // preview of DepthBuffer
        private static Texture _depthTexture;
        private static TextureView _depthTextureView;
        private static ResourceSet _depthTextureSet;

        private static DeviceBuffer _depthVertexBuffer;
        private static Shader[] _depthShaders;
        private static Pipeline _depthPipeline;
        private static bool _recreateDepthVerticeArray = true;

        private static float _depthIntensity = 0.5f;

        // prewview of ShadowBuffer

        private static Texture _shadowTexture;
        private static TextureView _shadowTextureView;
        private static ResourceSet _shadowTextureSet;

        private static DeviceBuffer _shadowVertexBuffer;
        private static Shader[] _shadowShaders;
        private static Pipeline _shadowPipeline;
        private static bool _recreateShadowVerticeArray = true;
        private static float _shadowIntensity = 0.5f;

        // drawtype: Line
        private static DeviceBuffer _lineVertexBuffer;
        private static Shader[] _lineShaders;
        private static Pipeline _linePipeline;
        private static bool _recreateLineVerticeArray = true;

        // drawtype: Dot
        private static DeviceBuffer _dotVertexBuffer;
        private static Shader[] _dotShaders;
        private static Pipeline _dotPipeline;
        private static bool _recreateDotVerticeArray = true;

        // drawtype: GridLine
        private static DeviceBuffer _gridLineVertexBuffer;
        private static Shader[] _gridLineShaders;
        private static Pipeline _gridLinePipeline;
        private static bool _recreateGridLineVerticeArray = true;

        //
        private static int _windowWidth = 800;
        private static int _windowHeight = 1200;
        private static int _windowLeft = 0;
        private static int _windowTop = 0;

        private static ImGuiRenderer _imGuiRenderer;
        private static String[] scriptNames;
        private static String[] directoryNames;
        private static String _lastDirectory;
        private static String _lastScript;

        private static int _selectedScript;
        private static int _selectedDirectory;

        public static int[] _drawSize = { 1, 1 };
        private static int[] _gridSize = { 1, 1 };
        private static float _gridIntensity = 0.5f;
        private static float _gridlinewidth = 100f;

        private static Vector3 _borderColor = new Vector3(0.0f, 0.0f, 1.0f);
        private static float _borderWidth = 50f;

        private static Vector3 _clearColor = new Vector3(0.0f, 0.0f, 0.0f);
        private static Vector3 _drawColor = new Vector3(0.3f, 0.6f, 0.4f);
        private static Vector2 _cameraPosition = new Vector2(0.0f, 0.0f);
        private static int _zoom = 13;
        private static float[] _zoomlevels = { 1f, 1.5f, 2f, 3.33f, 5f, 6.25f, 8.33f, 12.5f, 16.67f, 25f, 33.33f, 50f, 66.67f, 100f, 150f, 200f, 300f, 400f, 600f, 800f, 1000f, 2000f, 4000f, 6000f, 8000f, 12000f, 16000f, 24000f, 32000f, 64000f, 128000f };
        private static float _linewidth = 10f;
        private static float _cameraRotation = 0.0f;

        // UI state
        private static bool _showSettingsWindow = true;
        private static bool _showImGuiDemoWindow = false;
        private static bool _openDrawSettingsHeader = false;
        private static bool _openCameraHeader = false;
        private static bool _openSerialMonitorHeader = false;
        private static bool _openStatisticsHeader = false;
        private static bool _openDebugConsoleHeader = false;
        private static bool _openGeneratorHeader = false;

        private static int _numDebugLines = 0;

        private static FileIniDataParser _iniParser;
        private static IniData _iniData;
        private static Vector4 textcolor1 = new Vector4(0.81f, 0.88f, 0.72f, 1.00f);
        private static Vector4 textcolor2 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        private static bool _useRandomSeed = false;

        private static int _seed = 209284240;
        private static CodeCompiler _compiler;

        private static DriExporter _exporter;

        private static string _exportfilepath = "/home/robber/DrawMachineProject/drawings/";
        private static string _exportfilename = "";

        public static void AddNewQuadraticBezier(int i)
        {
            // Console.WriteLine(i.ToString());
        }

        private static float _memUsage = 0;
        private static double _cpuUsage = 0;
        private static TimeSpan _startCpuTime;
        private static DateTime _startTime;
        private static DateTime _endTime;
        private static TimeSpan _endCpuTime;

        static void Main(string[] args)
        {
            Process process = Process.GetCurrentProcess();
            _startCpuTime = process.TotalProcessorTime;
            _startTime = DateTime.UtcNow;
            _endCpuTime = process.TotalProcessorTime;
            _endTime = DateTime.UtcNow;

            _compiler = new CodeCompiler();
            _exporter = new DriExporter();

            //load user settings
            _iniParser = new FileIniDataParser();
            _iniData = _iniParser.ReadFile("Configuration.ini");
            if (_iniData["Header"]["DrawSettings"] == "true") _openDrawSettingsHeader = true;
            if (_iniData["Header"]["Camera"] == "true") _openCameraHeader = true;
            if (_iniData["Header"]["SerialMonitor"] == "true") _openSerialMonitorHeader = true;
            if (_iniData["Header"]["Statistics"] == "true") _openStatisticsHeader = true;
            if (_iniData["Header"]["DebugConsole"] == "true") _openDebugConsoleHeader = true;
            if (_iniData["Header"]["Generator"] == "true") _openGeneratorHeader = true;

            if (_iniData["Camera"]["PosX"] != null) _cameraPosition.X = float.Parse(_iniData["Camera"]["PosX"]);
            if (_iniData["Camera"]["PosY"] != null) _cameraPosition.Y = float.Parse(_iniData["Camera"]["PosY"]);
            if (_iniData["Camera"]["Rotation"] != null) _cameraRotation = float.Parse(_iniData["Camera"]["Rotation"]);
            if (_iniData["Camera"]["Zoom"] != null) _zoom = int.Parse(_iniData["Camera"]["Zoom"]);

            if (_iniData["DrawColor"]["R"] != null) _drawColor.X = float.Parse(_iniData["DrawColor"]["R"]);
            if (_iniData["DrawColor"]["G"] != null) _drawColor.Y = float.Parse(_iniData["DrawColor"]["G"]);
            if (_iniData["DrawColor"]["B"] != null) _drawColor.Z = float.Parse(_iniData["DrawColor"]["B"]);

            if (_iniData["ClearColor"]["R"] != null) _clearColor.X = float.Parse(_iniData["ClearColor"]["R"]);
            if (_iniData["ClearColor"]["G"] != null) _clearColor.Y = float.Parse(_iniData["ClearColor"]["G"]);
            if (_iniData["ClearColor"]["B"] != null) _clearColor.Z = float.Parse(_iniData["ClearColor"]["B"]);

            if (_iniData["BorderColor"]["R"] != null) _borderColor.X = float.Parse(_iniData["BorderColor"]["R"]);
            if (_iniData["BorderColor"]["G"] != null) _borderColor.Y = float.Parse(_iniData["BorderColor"]["G"]);
            if (_iniData["BorderColor"]["B"] != null) _borderColor.Z = float.Parse(_iniData["BorderColor"]["B"]);

            if (_iniData["Border"]["Width"] != null) _borderWidth = float.Parse(_iniData["Border"]["Width"]);

            if (_iniData["Line"]["Width"] != null) _linewidth = float.Parse(_iniData["Line"]["Width"]);

            if (_iniData["Draw"]["Width"] != null) _drawSize[0] = int.Parse(_iniData["Draw"]["Width"]);
            if (_iniData["Draw"]["Height"] != null) _drawSize[1] = int.Parse(_iniData["Draw"]["Height"]);

            if (_iniData["Grid"]["Width"] != null) _gridSize[0] = int.Parse(_iniData["Grid"]["Width"]);
            if (_iniData["Grid"]["Height"] != null) _gridSize[1] = int.Parse(_iniData["Grid"]["Height"]);
            if (_iniData["Grid"]["LineWidth"] != null) _gridlinewidth = float.Parse(_iniData["Grid"]["LineWidth"]);
            if (_iniData["Grid"]["Intensity"] != null) _gridIntensity = float.Parse(_iniData["Grid"]["Intensity"]);

            if (_iniData["Depth"]["Intensity"] != null) _depthIntensity = float.Parse(_iniData["Depth"]["Intensity"]);
            if (_iniData["Shadow"]["Intensity"] != null) _shadowIntensity = float.Parse(_iniData["Shadow"]["Intensity"]);

            if (_iniData["Generator"]["FilePath"] != null) _exportfilepath = _iniData["Generator"]["FilePath"];
            if (_iniData["Generator"]["FileName"] != null) _exportfilename = _iniData["Generator"]["FileName"];
            if (_iniData["Generator"]["Seed"] != null) _seed = int.Parse(_iniData["Generator"]["Seed"]);
            if (_iniData["Generator"]["UseSeed"] != null) _useRandomSeed = bool.Parse(_iniData["Generator"]["UseSeed"]);

            if (_iniData["Program"]["WindowWidth"] != null) _windowWidth = int.Parse(_iniData["Program"]["WindowWidth"]);
            if (_iniData["Program"]["WindowHeight"] != null) _windowHeight = int.Parse(_iniData["Program"]["WindowHeight"]);
            if (_iniData["Program"]["WindowLeft"] != null) _windowLeft = int.Parse(_iniData["Program"]["WindowLeft"]);
            if (_iniData["Program"]["WindowTop"] != null) _windowTop = int.Parse(_iniData["Program"]["WindowTop"]);
            if (_iniData["Program"]["LastDirectory"] != null) _lastDirectory = _iniData["Program"]["LastDirectory"];
            if (_iniData["Program"]["LastScript"] != null) _lastScript = _iniData["Program"]["LastScript"];

            //get a list of script files

            Data.paperSize = new Vector2(_drawSize[0], _drawSize[1]);

            directoryNames = Directory.GetDirectories("scripts");
            if (directoryNames.Length > 0)
            {
                if (directoryNames.Contains(_lastDirectory))
                {
                    _selectedDirectory = Array.IndexOf(directoryNames, _lastDirectory);
                    scriptNames = Directory.GetFiles(_lastDirectory, "*.cs");
                }
                else
                {
                    _lastDirectory = directoryNames[0];
                    scriptNames = Directory.GetFiles(directoryNames[0], "*.cs");
                    _selectedDirectory = 0;
                }
            }

            if (scriptNames.Length > 0)
            {
                if (scriptNames.Contains(_lastScript))
                {
                    _selectedScript = Array.IndexOf(scriptNames, _lastScript);
                }
                else
                {
                    _lastScript = scriptNames[0];
                    _selectedScript = 0;
                }
            }
            _iniData["Program"]["LastScript"] = _lastScript;
            _iniData["Program"]["LastDirectory"] = _lastDirectory;
            _iniParser.WriteFile("Configuration.ini", _iniData);


            // SDL_WindowFlags.Fullscreen;
            // SDL_WindowFlags.Maximized;
            // SDL_WindowFlags.Minimized;
            // SDL_WindowFlags.FullScreenDesktop;
            // SDL_WindowFlags.Hidden;
            // SDL_WindowFlags.OpenGL;
            // SDL_WindowFlags.Resizable;

            //create Window            
            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable | SDL_WindowFlags.Shown;
            _window = new Sdl2Window("DMP Designer v0.1.0", _windowLeft, _windowTop, _windowWidth, _windowHeight, flags, false);

            Sdl2Native.SDL_ShowCursor(0);

            //create Graphics Device
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(
                _window,
                new GraphicsDeviceOptions
                {
                    SwapchainDepthFormat = PixelFormat.R32_Float,
                    ResourceBindingModel = ResourceBindingModel.Improved,
                    PreferStandardClipSpaceYDirection = true,
                    PreferDepthRangeZeroToOne = true,
                    SyncToVerticalBlank = true,
                    Debug = false
                },
                GraphicsBackend.Vulkan
            );

            _window.Resized += () =>
            {
                _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            };

            //The ResourceFactory is the source for graphicsdevice related objects such as buffers, shaders, pipeline
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            //// Setup Rendering

            //// ------------------- General Resource Set ------------------------- ////
            // First we set up a resource set for common used resources such as viewport/projection buffers
            // we set up the viewport as 2 buffers holding our camera and projection transformation matrices.
            // To use matrices in the shader, we need to describe the buffer layout (uniform buffers for use by the vertex shader) for the pipeline and create a ResourceSet for the commandlist

            _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _cameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _rotationBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _translationBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            ResourceLayout viewportResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("RotationBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("TranslationBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            ));
            _viewportResourceSet = factory.CreateResourceSet(new ResourceSetDescription(viewportResourceLayout, _projectionBuffer, _cameraBuffer, _rotationBuffer, _translationBuffer));

            //// ------------------- Lines ------------------------- ////

            //create a vertex buffer
            _lineVertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 36, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_lineVertexBuffer, 0, new LineVertex[0]);

            //describe the data layout
            VertexLayoutDescription lineVertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Edge", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("Width", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("Layer", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
            );
            //load Shaders
            _lineShaders = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("Shaders/line_shader-vert.glsl"), "main"), new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("Shaders/line_shader-frag.glsl"), "main"));

            //describe graphics pipeline
            GraphicsPipelineDescription linePipelineDescription = new GraphicsPipelineDescription();
            linePipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            linePipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            linePipelineDescription.DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, comparisonKind: ComparisonKind.LessEqual);
            linePipelineDescription.RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back, fillMode: PolygonFillMode.Solid, frontFace: FrontFace.Clockwise, depthClipEnabled: true, scissorTestEnabled: true);
            linePipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            linePipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { lineVertexBufferLayout }, shaders: _lineShaders);

            //add resource sets: general viewport and optional rendertype specific
            linePipelineDescription.ResourceLayouts = new[] { viewportResourceLayout };
            _linePipeline = factory.CreateGraphicsPipeline(linePipelineDescription);


            //// ------------------- Dots ------------------------- ////

            //create a vertex buffer
            _dotVertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 36, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_dotVertexBuffer, 0, new DotVertex[0]);

            //describe the data layout
            VertexLayoutDescription dotVertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Layer", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
            );
            //load Shaders
            _dotShaders = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("Shaders/dot_shader-vert.glsl"), "main"), new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("Shaders/dot_shader-frag.glsl"), "main"));

            //describe graphics pipeline
            GraphicsPipelineDescription dotPipelineDescription = new GraphicsPipelineDescription();
            dotPipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            dotPipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            dotPipelineDescription.DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, comparisonKind: ComparisonKind.LessEqual);
            dotPipelineDescription.RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back, fillMode: PolygonFillMode.Solid, frontFace: FrontFace.Clockwise, depthClipEnabled: true, scissorTestEnabled: true);
            dotPipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            dotPipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { dotVertexBufferLayout }, shaders: _dotShaders);

            //add resource sets: general viewport and optional rendertype specific
            dotPipelineDescription.ResourceLayouts = new[] { viewportResourceLayout };
            _dotPipeline = factory.CreateGraphicsPipeline(dotPipelineDescription);

            //// ------------------- GridLines ------------------------- ////

            //create a vertex buffer
            _gridLineVertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 36, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_gridLineVertexBuffer, 0, new LineVertex[0]);

            //describe the data layout
            VertexLayoutDescription gridLineVertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Edge", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("Width", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                new VertexElementDescription("Layer", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
            );
            //load Shaders
            _gridLineShaders = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("Shaders/grid_shader-vert.glsl"), "main"), new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("Shaders/grid_shader-frag.glsl"), "main"));

            //describe graphics pipeline
            GraphicsPipelineDescription gridLinePipelineDescription = new GraphicsPipelineDescription();
            gridLinePipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            gridLinePipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            gridLinePipelineDescription.DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, comparisonKind: ComparisonKind.LessEqual);
            gridLinePipelineDescription.RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back, fillMode: PolygonFillMode.Solid, frontFace: FrontFace.Clockwise, depthClipEnabled: true, scissorTestEnabled: true);
            gridLinePipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            gridLinePipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { gridLineVertexBufferLayout }, shaders: _gridLineShaders);

            //add resource sets: general viewport and optional rendertype specific
            gridLinePipelineDescription.ResourceLayouts = new[] { viewportResourceLayout };
            _gridLinePipeline = factory.CreateGraphicsPipeline(gridLinePipelineDescription);

            //// ------------------- DepthBuffer ------------------------- ////

            //create a vertex buffer
            _depthVertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 20, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_depthVertexBuffer, 0, new LineVertex[0]);

            //describe the data layout
            VertexLayoutDescription depthVertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Alpha", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
            );
            //load Shaders
            _depthShaders = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("Shaders/depth_shader-vert.glsl"), "main"), new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("Shaders/depth_shader-frag.glsl"), "main"));
            _depthTexture = factory.CreateTexture(TextureDescription.Texture2D(Data.rasterWidth, Data.rasterHeight, 1, 1, PixelFormat.R16_UNorm, TextureUsage.Sampled));
            _depthTextureView = factory.CreateTextureView(_depthTexture);

            // Array.Clear(Data.depthMap,0,Data.depthMap.Length);
            Array.Fill(Data.depthMap, ushort.MaxValue);

            _graphicsDevice.UpdateTexture<UInt16>(_depthTexture, Data.depthMap, 0, 0, 0, Data.rasterWidth, Data.rasterHeight, 1, 0, 0);

            ResourceLayout depthTextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            _depthTextureSet = factory.CreateResourceSet(new ResourceSetDescription(depthTextureLayout, _depthTextureView, _graphicsDevice.PointSampler));

            //describe graphics pipeline
            GraphicsPipelineDescription depthPipelineDescription = new GraphicsPipelineDescription();
            depthPipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            depthPipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            depthPipelineDescription.DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, comparisonKind: ComparisonKind.LessEqual);
            depthPipelineDescription.RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back, fillMode: PolygonFillMode.Solid, frontFace: FrontFace.Clockwise, depthClipEnabled: true, scissorTestEnabled: true);
            depthPipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            depthPipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { depthVertexBufferLayout }, shaders: _depthShaders);

            //add resource sets: general viewport and optional rendertype specific
            depthPipelineDescription.ResourceLayouts = new[] { viewportResourceLayout, depthTextureLayout };
            _depthPipeline = factory.CreateGraphicsPipeline(depthPipelineDescription);

            //// ------------------- ShadowBuffer ------------------------- ////

            //create a vertex buffer
            _shadowVertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 20, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_shadowVertexBuffer, 0, new LineVertex[0]);

            //describe the data layout
            VertexLayoutDescription shadowVertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Alpha", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1)
            );
            //load Shaders
            _shadowShaders = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes("Shaders/shadow_shader-vert.glsl"), "main"), new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes("Shaders/depth_shader-frag.glsl"), "main"));
            _shadowTexture = factory.CreateTexture(TextureDescription.Texture2D(Data.rasterWidth, Data.rasterHeight, 1, 1, PixelFormat.R16_UNorm, TextureUsage.Sampled));
            _shadowTextureView = factory.CreateTextureView(_shadowTexture);


            Array.Fill(Data.shadowMap, ushort.MaxValue);

            _graphicsDevice.UpdateTexture<UInt16>(_shadowTexture, Data.shadowMap, 0, 0, 0, Data.rasterWidth, Data.rasterHeight, 1, 0, 0);

            ResourceLayout shadowTextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            _shadowTextureSet = factory.CreateResourceSet(new ResourceSetDescription(shadowTextureLayout, _shadowTextureView, _graphicsDevice.PointSampler));

            //describe graphics pipeline
            GraphicsPipelineDescription shadowPipelineDescription = new GraphicsPipelineDescription();
            shadowPipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            shadowPipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            shadowPipelineDescription.DepthStencilState = new DepthStencilStateDescription(depthTestEnabled: true, depthWriteEnabled: true, comparisonKind: ComparisonKind.LessEqual);
            shadowPipelineDescription.RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back, fillMode: PolygonFillMode.Solid, frontFace: FrontFace.Clockwise, depthClipEnabled: true, scissorTestEnabled: true);
            shadowPipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            shadowPipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { shadowVertexBufferLayout }, shaders: _shadowShaders);

            //add resource sets: general viewport and optional rendertype specific
            shadowPipelineDescription.ResourceLayouts = new[] { viewportResourceLayout, shadowTextureLayout };
            _shadowPipeline = factory.CreateGraphicsPipeline(shadowPipelineDescription);

            //// ------------------- Init ------------------------- ////

            // Create the commandlist, to record and execute graphics commands.
            _commandList = factory.CreateCommandList();

            // Create ImGuiRenderer.
            _imGuiRenderer = new ImGuiRenderer(_graphicsDevice, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height, ColorSpaceHandling.Linear);

            Stopwatch sw = Stopwatch.StartNew();
            double previousTime = sw.Elapsed.TotalSeconds;

            //// ------------------- RENDERING LOOP ------------------------- ////
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (_window.Exists)
                {
                    double newTime = sw.Elapsed.TotalSeconds;
                    float deltaSeconds = (float)(newTime - previousTime);

                    _imGuiRenderer.UpdateInput(snapshot, deltaSeconds, _window.Width, _window.Height);
                    ImGui.NewFrame();
                    UpdateInput(snapshot, deltaSeconds);

                    RebuildGUI();
                    ImGui.Render();

                    // Recreate GUI buffers.
                    _imGuiRenderer.UpdateGeometry(_graphicsDevice);

                    // Stage all drawing commands in the command list.
                    _commandList.Begin();

                    // Set and clear framebuffer.
                    _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                    _commandList.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                    _commandList.ClearDepthStencil(1f);

                    // Create view matrix and projection matrix.
                    _commandList.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreateOrthographic(_window.Width * (Data.stepsPerMM * 10f) / _zoomlevels[_zoom], _window.Height * (Data.stepsPerMM * 10f) / _zoomlevels[_zoom], 0.1f, 50000f));
                    _commandList.UpdateBuffer(_cameraBuffer, 0, Matrix4x4.CreateLookAt(new Vector3(_cameraPosition.X, _cameraPosition.Y, 1000), new Vector3(_cameraPosition.X, _cameraPosition.Y, 0f), Vector3.UnitY));
                    _commandList.UpdateBuffer(_rotationBuffer, 0, Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, _cameraRotation * 0.01745329252f));
                    _commandList.UpdateBuffer(_translationBuffer, 0, new Vector4(_drawSize[0] * (Data.stepsPerMM * 0.5f), _drawSize[1] * (Data.stepsPerMM * 0.5f), 0, 1));

                    _recreateLineVerticeArray = true;

                    if (_compiler.IsGenerating)
                    {
                        // Update geometry if needed and draw
                        _recreateDotVerticeArray = true;
                        _recreateDepthVerticeArray = true;
                        _recreateShadowVerticeArray = true;
                    }

                    UpdateAndDrawDots();
                    UpdateAndDrawShadowMap();
                    UpdateAndDrawDepthMap();
                    UpdateAndDrawGridLines();
                    UpdateAndDrawLines();

                    //// Draw UI
                    _imGuiRenderer.Draw(_commandList);

                    // End() must be called before commands can be submitted for execution.
                    // Once commands have been submitted, the rendered image can be presented to the application window.
                    _commandList.End();
                    _graphicsDevice.SubmitCommands(_commandList);
                    _graphicsDevice.SwapBuffers();
                    _graphicsDevice.WaitForIdle();


                    // Save window size and position
                    if (_window.Width != _windowWidth || _window.Height != _windowHeight || _window.X != _windowLeft || _window.Y != _windowTop)
                    {
                        _windowWidth = _window.Width;
                        _windowHeight = _window.Height;
                        _windowLeft = _window.X;
                        _windowTop = _window.Y;
                        _iniData["Program"]["WindowWidth"] = _windowWidth.ToString();
                        _iniData["Program"]["WindowHeight"] = _windowHeight.ToString();
                        _iniData["Program"]["WindowLeft"] = _windowLeft.ToString();
                        _iniData["Program"]["WindowTop"] = _windowTop.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    previousTime = newTime;
                }
            }

            // Clean up.
            _linePipeline.Dispose();
            _lineVertexBuffer.Dispose();
            foreach (Shader shader in _lineShaders)
            {
                shader.Dispose();
            }

            _dotPipeline.Dispose();
            _dotVertexBuffer.Dispose();
            foreach (Shader shader in _dotShaders)
            {
                shader.Dispose();
            }

            _gridLinePipeline.Dispose();
            _gridLineVertexBuffer.Dispose();
            foreach (Shader shader in _gridLineShaders)
            {
                shader.Dispose();
            }

            _depthTexture.Dispose();
            _depthTextureView.Dispose();
            _depthPipeline.Dispose();
            _depthVertexBuffer.Dispose();
            foreach (Shader shader in _depthShaders)
            {
                shader.Dispose();
            }

            _shadowTexture.Dispose();
            _shadowTextureView.Dispose();
            _shadowPipeline.Dispose();
            _shadowVertexBuffer.Dispose();
            foreach (Shader shader in _shadowShaders)
            {
                shader.Dispose();
            }

            _cameraBuffer.Dispose();
            _projectionBuffer.Dispose();
            _rotationBuffer.Dispose();
            _translationBuffer.Dispose();

            _imGuiRenderer.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
        }

        private static void Export(string s)
        {
            _exporter.Export(s);
            _recreateDotVerticeArray = true;
        }

        private static void Generate()
        {
            if (!_compiler.IsCompiling)
            {
                if (scriptNames.Length > 0 && File.Exists(scriptNames[_selectedScript]))
                {
                    Console.WriteLine("Compiling script.");
                    Data.DebugConsole.Add(("Compiling script."));

                    Type scriptType = _compiler.Compile(scriptNames[_selectedScript]);

                    if (scriptType != null)
                    {
                        Console.WriteLine("Executing script.");
                        Data.DebugConsole.Add(("Executing script."));

                        // Compile and run script
                        if (!_useRandomSeed)
                        {
                            _seed = new Random().Next();
                            _iniData["Generator"]["Seed"] = _seed.ToString();
                            _iniParser.WriteFile("Configuration.ini", _iniData);
                        }

                        _compiler.Run(scriptType, _seed);
                    }
                    else
                    {
                        Console.WriteLine("Script compilation failed..");
                        Data.DebugConsole.Add(("Script compilation failed.."));
                    }

                }
            }
        }

        private static void UpdateAndDrawDots()
        {
            Dot[] dotsSnapshot = Data.dots.ToArray();
            if (_recreateDotVerticeArray)
            {
                DotVertex[] dotVertices = new DotVertex[Data.dots.Count * 4];
                Parallel.For(0, dotsSnapshot.Length, d =>
                        {
                            dotVertices[d * 4 + 0] = new DotVertex();
                            dotVertices[d * 4 + 0].Position = new Vector2(dotsSnapshot[d].position.X - dotsSnapshot[d].size, dotsSnapshot[d].position.Y + dotsSnapshot[d].size);
                            dotVertices[d * 4 + 0].Color = dotsSnapshot[d].color;
                            dotVertices[d * 4 + 0].UV = new Vector2(0f, 1f); ;
                            dotVertices[d * 4 + 0].Layer = dotsSnapshot[d].layer;

                            dotVertices[d * 4 + 1] = new DotVertex();
                            dotVertices[d * 4 + 1].Position = new Vector2(dotsSnapshot[d].position.X - dotsSnapshot[d].size, dotsSnapshot[d].position.Y + dotsSnapshot[d].size);
                            dotVertices[d * 4 + 1].Color = dotsSnapshot[d].color;
                            dotVertices[d * 4 + 1].UV = new Vector2(1f, 1f);
                            dotVertices[d * 4 + 1].Layer = dotsSnapshot[d].layer;

                            dotVertices[d * 4 + 2] = new DotVertex();
                            dotVertices[d * 4 + 2].Position = new Vector2(dotsSnapshot[d].position.X - dotsSnapshot[d].size, dotsSnapshot[d].position.Y + dotsSnapshot[d].size);
                            dotVertices[d * 4 + 2].Color = dotsSnapshot[d].color;
                            dotVertices[d * 4 + 2].UV = new Vector2(0f, 0f); ;
                            dotVertices[d * 4 + 2].Layer = dotsSnapshot[d].layer;

                            dotVertices[d * 4 + 3] = new DotVertex();
                            dotVertices[d * 4 + 3].Position = new Vector2(dotsSnapshot[d].position.X - dotsSnapshot[d].size, dotsSnapshot[d].position.Y + dotsSnapshot[d].size);
                            dotVertices[d * 4 + 3].Color = dotsSnapshot[d].color;
                            dotVertices[d * 4 + 3].UV = new Vector2(1f, 0f); ;
                            dotVertices[d * 4 + 3].Layer = dotsSnapshot[d].layer;
                        });
                _graphicsDevice.DisposeWhenIdle(_dotVertexBuffer);
                _dotVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)Data.dots.Count * 4 * 36, BufferUsage.VertexBuffer));
                _graphicsDevice.UpdateBuffer(_dotVertexBuffer, 0, dotVertices);
                _recreateDotVerticeArray = false;
            }

            _commandList.SetPipeline(_dotPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _dotVertexBuffer);

            uint vStart = 0;
            for (int l = 0; l < dotsSnapshot.Length; l++)
            {
                _commandList.Draw(vertexCount: 4, instanceCount: 1, vertexStart: vStart, instanceStart: 0);
                vStart += 4;
            }
        }

        private static void UpdateAndDrawLines()
        {
            Line[] lineSnapshot = Data.lines.ToArray();
            if (_recreateLineVerticeArray)
            {
                int vCount = 0;
                List<LineVertex> lineVertices = new List<LineVertex>();
                for (int l = 0; l < lineSnapshot.Length; l++)
                {
                    // Straight Lines
                    if (lineSnapshot[l].type == LineType.Straight)
                    {
                        for (int ctr = 0; ctr < lineSnapshot[l].points.Length; ctr++)
                        {
                            LineVertex v1 = new LineVertex();
                            v1.Width = _linewidth * Data.stepsPerMM * 0.5f + lineSnapshot[l].points[ctr].Z * 0.35f;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = _linewidth * Data.stepsPerMM * 0.5f + lineSnapshot[l].points[ctr].Z * 0.35f;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector2 vnorm;

                            if (ctr == 0)
                            {
                                vnorm = Vector2.Normalize(Vector2.Subtract(lineSnapshot[l].points[ctr + 1].XY(), lineSnapshot[l].points[ctr].XY()));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = lineSnapshot[l].points[ctr].XY() - vperp * v1.Width;
                                v2.Position = lineSnapshot[l].points[ctr].XY() + vperp * v2.Width;
                            }

                            if (ctr > 0 && ctr + 1 < lineSnapshot[l].points.Length)
                            {
                                Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(lineSnapshot[l].points[ctr].XY(), lineSnapshot[l].points[ctr - 1].XY())); //incoming line
                                Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(lineSnapshot[l].points[ctr + 1].XY(), lineSnapshot[l].points[ctr].XY())); //outgoing line
                                vnorm = Vector2.Normalize(new Vector2((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y)));
                                float len = (v1.Width) / Vector2.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = lineSnapshot[l].points[ctr].XY() - vperp * (len);
                                v2.Position = lineSnapshot[l].points[ctr].XY() + vperp * (len);
                            }

                            //line end
                            if (ctr + 1 == lineSnapshot[l].points.Length)
                            {
                                vnorm = Vector2.Normalize(Vector2.Subtract(lineSnapshot[l].points[ctr].XY(), lineSnapshot[l].points[ctr - 1].XY()));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = lineSnapshot[l].points[ctr].XY() - vperp * v1.Width;
                                v2.Position = lineSnapshot[l].points[ctr].XY() + vperp * v2.Width;
                            }

                            lineVertices.Insert(vCount + ctr * 2, v1);
                            lineVertices.Insert(vCount + ctr * 2 + 1, v2);

                        }
                        lineSnapshot[l].vCount = (uint)lineSnapshot[l].points.Length * 2;
                        vCount += lineSnapshot[l].points.Length * 2;
                    }

                    // Curves

                    if (lineSnapshot[l].type == LineType.QuadraticBezier)
                    {
                        //generate points
                        Vector3 A = lineSnapshot[l].points[0];
                        Vector3 B = lineSnapshot[l].points[1];
                        Vector3 C = lineSnapshot[l].points[2];
                        Vector3[] points = new Vector3[101];

                        for (int p = 0; p <= 100; p++)
                        {
                            float t = ((float)p) / 100;
                            points[p] = (1f - t) * (1f - t) * A + 2 * (1f - t) * t * B + t * t * C;
                        }
                        for (int pctr = 0; pctr < points.Length; pctr++)
                        {
                            LineVertex v1 = new LineVertex();
                            v1.Width = points[pctr].Z * 0.35f + _linewidth * Data.stepsPerMM * 0.5f;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = points[pctr].Z * 0.35f + _linewidth * Data.stepsPerMM * 0.5f;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector2 point = new Vector2(points[pctr].X, points[pctr].Y);

                            Vector2 vnorm;
                            //line start
                            if (pctr == 0)
                            {
                                Vector2 nextpoint = new Vector2(points[pctr + 1].X, points[pctr + 1].Y);
                                vnorm = Vector2.Normalize(Vector2.Subtract(nextpoint, point));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = point - vperp * v1.Width;
                                v2.Position = point + vperp * v2.Width;
                            }

                            if (pctr > 0 && pctr + 1 < points.Length)
                            {
                                Vector2 nextpoint = new Vector2(points[pctr + 1].X, points[pctr + 1].Y);
                                Vector2 prevpoint = new Vector2(points[pctr - 1].X, points[pctr - 1].Y);
                                Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(point, prevpoint)); //incoming line
                                Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(nextpoint, point)); //outgoing line
                                vnorm = Vector2.Normalize(new Vector2((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y)));
                                float len = v1.Width / Vector2.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = point - vperp * (len);
                                v2.Position = point + vperp * (len);
                            }

                            //line end
                            if (pctr + 1 == points.Length)
                            {
                                Vector2 prevpoint = new Vector2(points[pctr - 1].X, points[pctr - 1].Y);
                                vnorm = Vector2.Normalize(Vector2.Subtract(point, prevpoint));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = point - vperp * v1.Width;
                                v2.Position = point + vperp * v2.Width;
                            }

                            lineVertices.Insert(vCount + pctr * 2, v1);
                            lineVertices.Insert(vCount + pctr * 2 + 1, v2);

                        }
                        lineSnapshot[l].vCount = (uint)points.Length * 2;
                        vCount += points.Length * 2;
                    }
                }

                _graphicsDevice.DisposeWhenIdle(_lineVertexBuffer);
                _lineVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)vCount * 36, BufferUsage.VertexBuffer));

                _graphicsDevice.UpdateBuffer(_lineVertexBuffer, 0, lineVertices.ToArray());
                _recreateLineVerticeArray = false;
            }

            _commandList.SetPipeline(_linePipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _lineVertexBuffer);

            uint vStart = 0;
            uint vLength = 0;
            for (int l = 0; l < lineSnapshot.Length; l++)
            {
                if (lineSnapshot[l].type == LineType.QuadraticBezier)
                {
                    vLength = lineSnapshot[l].vCount;
                }

                if (lineSnapshot[l].type == LineType.Straight)
                {
                    vLength = lineSnapshot[l].vCount;
                }

                _commandList.Draw(vertexCount: vLength, instanceCount: 1, vertexStart: vStart, instanceStart: 0);
                vStart += vLength;
            }
        }

        private static void UpdateAndDrawDepthMap()
        {
            if (_recreateDepthVerticeArray)
            {
                DepthVertex[] depthVertices = new DepthVertex[4];

                depthVertices[0] = new DepthVertex() { TexCoords = new Vector2(0.0f, 0.0f), Position = new Vector2(0, 0), Alpha = _depthIntensity };
                depthVertices[1] = new DepthVertex() { TexCoords = new Vector2(0.0f, 1.0f), Position = new Vector2(0, _drawSize[1] * Data.stepsPerMM), Alpha = _depthIntensity };
                depthVertices[2] = new DepthVertex() { TexCoords = new Vector2(1.0f, 0.0f), Position = new Vector2(_drawSize[0] * Data.stepsPerMM, 0), Alpha = _depthIntensity };
                depthVertices[3] = new DepthVertex() { TexCoords = new Vector2(1.0f, 1.0f), Position = new Vector2(_drawSize[0] * Data.stepsPerMM, _drawSize[1] * Data.stepsPerMM), Alpha = _depthIntensity };

                _depthVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(4 * 20, BufferUsage.VertexBuffer));
                _graphicsDevice.UpdateBuffer(_depthVertexBuffer, 0, depthVertices.ToArray());

                _graphicsDevice.UpdateTexture<UInt16>(_depthTexture, Data.depthMap, 0, 0, 0, Data.rasterWidth, Data.rasterHeight, 1, 0, 0);
                _recreateDepthVerticeArray = false;
            }
            _commandList.SetPipeline(_depthPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetGraphicsResourceSet(1, _depthTextureSet);
            _commandList.SetVertexBuffer(0, _depthVertexBuffer);

            // Draw the depthbuffer.
            _commandList.Draw(vertexCount: 4, instanceCount: 1, vertexStart: 0, instanceStart: 0);
        }

        private static void UpdateAndDrawShadowMap()
        {
            if (_recreateShadowVerticeArray)
            {
                // Create a quad to draw the shadowbuffer, and update the shadowbuffer from the Data.shadowMap.

                ShadowVertex[] shadowVertices = new ShadowVertex[4];
                shadowVertices[0] = new ShadowVertex() { TexCoords = new Vector2(0.0f, 0.0f), Position = new Vector2(0, 0), Alpha = _shadowIntensity };
                shadowVertices[1] = new ShadowVertex() { TexCoords = new Vector2(0.0f, 1.0f), Position = new Vector2(0, _drawSize[1] * Data.stepsPerMM), Alpha = _shadowIntensity };
                shadowVertices[2] = new ShadowVertex() { TexCoords = new Vector2(1.0f, 0.0f), Position = new Vector2(_drawSize[0] * Data.stepsPerMM, 0), Alpha = _shadowIntensity };
                shadowVertices[3] = new ShadowVertex() { TexCoords = new Vector2(1.0f, 1.0f), Position = new Vector2(_drawSize[0] * Data.stepsPerMM, _drawSize[1] * Data.stepsPerMM), Alpha = _shadowIntensity };

                _shadowVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(4 * 20, BufferUsage.VertexBuffer));
                _graphicsDevice.UpdateBuffer(_shadowVertexBuffer, 0, shadowVertices.ToArray());

                _graphicsDevice.UpdateTexture<UInt16>(_shadowTexture, Data.shadowMap, 0, 0, 0, Data.rasterWidth, Data.rasterHeight, 1, 0, 0);
                _recreateShadowVerticeArray = false;
            }

            _commandList.SetPipeline(_shadowPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetGraphicsResourceSet(1, _shadowTextureSet);
            _commandList.SetVertexBuffer(0, _shadowVertexBuffer);

            // Draw the shadowbuffer.
            _commandList.Draw(vertexCount: 4, instanceCount: 1, vertexStart: 0, instanceStart: 0);
        }

        private static void UpdateAndDrawGridLines()
        {
            if (_gridSize[0] <= 0 || _gridSize[1] <= 0)
                return;

            LineVertex[] gridLineVertices = new LineVertex[_gridSize[0] * _gridSize[1] * 4 + 10];

            int layer = 0;
            int lastlayer = 0;
            int w = (_drawSize[0] / _gridSize[0]) * Data.stepsPerMM;
            int h = (_drawSize[1] / _gridSize[1]) * Data.stepsPerMM;
            int index = 0;

            for (int xtile = 0; xtile < _gridSize[0]; xtile++)
            {
                if (lastlayer == layer) layer = (layer + 1) % 2;
                lastlayer = layer;
                for (int ytile = 0; ytile < _gridSize[1]; ytile++)
                {
                    layer = (layer + 1) % 2;

                    Vector2 start = new Vector2(xtile * w, ytile * h);
                    Vector2 size = new Vector2(w, h);

                    // Choose color based on layer.
                    RgbaFloat color;
                    if (layer == 0)
                    {
                        float r = _clearColor.X > 0.5f ? _clearColor.X - 0.5f * _gridIntensity : _clearColor.X + 0.5f * _gridIntensity;
                        float g = _clearColor.Y > 0.5f ? _clearColor.Y - 0.5f * _gridIntensity : _clearColor.Y + 0.5f * _gridIntensity;
                        float b = _clearColor.Z > 0.5f ? _clearColor.Z - 0.5f * _gridIntensity : _clearColor.Z + 0.5f * _gridIntensity;
                        color = new RgbaFloat(r, g, b, 1.0f);
                    }
                    else
                    {
                        float r = _clearColor.X > 0.5f ? _clearColor.X - 1.0f * _gridIntensity : _clearColor.X + 1.0f * _gridIntensity;
                        float g = _clearColor.Y > 0.5f ? _clearColor.Y - 1.0f * _gridIntensity : _clearColor.Y + 1.0f * _gridIntensity;
                        float b = _clearColor.Z > 0.5f ? _clearColor.Z - 1.0f * _gridIntensity : _clearColor.Z + 1.0f * _gridIntensity;
                        color = new RgbaFloat(r, g, b, 1.0f);
                    }

                    // Create four vertices for the current grid tile.
                    gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = color, Position = start };
                    gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = color, Position = start + new Vector2(0, size.Y) };
                    gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = color, Position = start + new Vector2(size.X, 0) };
                    gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = color, Position = start + size };
                }
            }

            /// Add the border.
            /// 

            RgbaFloat borderC = new RgbaFloat(_borderColor.X, _borderColor.Y, _borderColor.Z, 1.0f);

            /// Create ten vertices for the border.
            /// 
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = borderC, Position = new Vector2(-_borderWidth * Data.stepsPerMM, -_borderWidth * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = borderC, Position = new Vector2(0f, 0f) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = borderC, Position = new Vector2((_drawSize[0] + _borderWidth) * Data.stepsPerMM, -_borderWidth * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = borderC, Position = new Vector2(_drawSize[0] * Data.stepsPerMM, 0f) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = borderC, Position = new Vector2((_drawSize[0] + _borderWidth) * Data.stepsPerMM, (_drawSize[1] + _borderWidth) * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = borderC, Position = new Vector2(_drawSize[0] * Data.stepsPerMM, _drawSize[1] * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = borderC, Position = new Vector2((-_borderWidth) * Data.stepsPerMM, (_drawSize[1] + _borderWidth) * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = borderC, Position = new Vector2(0, _drawSize[1] * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 0, Color = borderC, Position = new Vector2(-_borderWidth * Data.stepsPerMM, -_borderWidth * Data.stepsPerMM) };
            gridLineVertices[index++] = new LineVertex() { Width = _gridlinewidth, Edge = 1, Color = borderC, Position = new Vector2(0f, 0f) };

            // Update the grid vertex buffer.
            _graphicsDevice.DisposeWhenIdle(_gridLineVertexBuffer);
            _gridLineVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)gridLineVertices.Length * 36, BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_gridLineVertexBuffer, 0, gridLineVertices);

            _commandList.SetPipeline(_gridLinePipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _gridLineVertexBuffer);

            // Draw the grid.
            _commandList.Draw(vertexCount: (uint)gridLineVertices.Length - 10, instanceCount: 1, vertexStart: 0, instanceStart: 0);

            // Draw the border.
            _commandList.Draw(vertexCount: 10, instanceCount: 1, vertexStart: (uint)gridLineVertices.Length - 10, instanceStart: 0);
        }

        private static void UpdateInput(InputSnapshot snapshot, float deltaSeconds)
        {
            // Handle input that is not used by ImGui.
            if (!ImGui.GetIO().WantCaptureMouse)
            {
                // if (snapshot.IsMouseDown(MouseButton.Left)) {
                // } else {
                //     ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                // }
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    // Console.WriteLine("leftDoubleClicked");
                }
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    // Console.WriteLine("leftReleased");
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                }

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                    _cameraPosition = _cameraPosition - new Vector2(ImGui.GetMouseDragDelta().X * ((25600f / MathF.PI) / _zoomlevels[_zoom]), -ImGui.GetMouseDragDelta().Y * ((25600f / MathF.PI) / _zoomlevels[_zoom]));
                    _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                    _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                    _iniParser.WriteFile("Configuration.ini", _iniData);

                    ImGui.ResetMouseDragDelta();
                }

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                }

                // for (int i = 0; i < snapshot.MouseEvents.Count; i++) {
                //     MouseEvent me = snapshot.MouseEvents[i];
                //     if (me.Down) {
                //         switch (me.MouseButton) {
                //             case MouseButton.Left:
                //                 Console.WriteLine("leftPressed");
                //                 Data.DebugConsole.Add("Mouse: Left Button Pressed");
                //                 //compose the gui and build drawdata with ImGui.Render

                //                 // ImGuiIOPtr io = ImGui.GetIO();
                //                 // if (io.MouseDown[0]) {
                //                 // Console.WriteLine("mousepress");
                //                 //     ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                //                 // }

                //                 break;
                //             case MouseButton.Middle:
                //                 Console.WriteLine("middlePressed");
                //                 Data.DebugConsole.Add("Mouse: Middle Button Pressed");
                //                 break;
                //             case MouseButton.Right:
                //                 Console.WriteLine("rightPressed");
                //                 Data.DebugConsole.Add("Mouse: Right Button Pressed");
                //                 break;
                //         } 
                //     }
                // }

                if (snapshot.WheelDelta > 0)
                {
                    // _cameraPosition = _cameraPosition - new Vector3(0,0,10);
                    if (_zoom < _zoomlevels.Count() - 1)
                    {
                        _zoom += 1;
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);

                    }
                    // Console.WriteLine("zoomin");
                }
                if (snapshot.WheelDelta < 0)
                {
                    // Console.WriteLine("zoomout");
                    // _cameraPosition = _cameraPosition + new Vector3(0,0,10);
                    if (_zoom > 0)
                    {
                        _zoom -= 1;
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }


                //check if Keyboared in use by ImGui
                if (!ImGui.GetIO().WantCaptureKeyboard)
                {
                    for (int i = 0; i < snapshot.KeyEvents.Count; i++)
                    {
                        KeyEvent ke = snapshot.KeyEvents[i];
                        if (ke.Down)
                        {
                            // Console.Write("keydown: ");
                            // Console.WriteLine(ke.Key);
                            // Data.DebugConsole.Add("keydown: " + ke.Key.ToString());
                        }
                        else
                        {
                            // Console.Write("keyup: ");
                            // Console.WriteLine(ke.Key);
                            // Data.DebugConsole.Add("keyup: " + ke.Key.ToString());
                        }
                    }
                }
            }
        }

        private static void RebuildGUI()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Quit", "", false, true))
                    {
                        _window.Close();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Window"))
                {
                    ImGui.MenuItem("Settings", string.Empty, ref _showSettingsWindow, true);
                    ImGui.MenuItem("ImGui Demo", string.Empty, ref _showImGuiDemoWindow, true);
                    ImGui.Separator();
                    ImGui.MenuItem("Draw Settings", string.Empty, ref _openDrawSettingsHeader, true);
                    ImGui.MenuItem("Viewport", string.Empty, ref _openCameraHeader, true);
                    ImGui.MenuItem("Serial Monitor", string.Empty, ref _openSerialMonitorHeader, true);
                    ImGui.MenuItem("Statistics", string.Empty, ref _openStatisticsHeader, true);
                    ImGui.MenuItem("Debug Console", string.Empty, ref _openDebugConsoleHeader, true);
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            ImGui.PopStyleColor();

            if (_showImGuiDemoWindow)
            {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            }

            if (_showSettingsWindow)
            {
                ImGui.SetNextWindowPos(new Vector2(0, 24f));
                ImGui.SetNextWindowSize(new Vector2(316f, _window.Height - 24f));
                ImGui.Begin("Settings", ref _showSettingsWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);

                ImGui.SetNextItemOpen(_openDrawSettingsHeader);
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                if (ImGui.CollapsingHeader("Draw Settings"))
                {
                    ImGui.PopStyleColor();

                    if (!_openDrawSettingsHeader)
                    {
                        _openDrawSettingsHeader = true;
                        _iniData["Header"]["DrawSettings"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();

                    ImGui.Text("Clear color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(73, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.ColorEdit3("##clearcol", ref _clearColor))
                    {
                        _iniData["ClearColor"]["R"] = _clearColor.X.ToString();
                        _iniData["ClearColor"]["G"] = _clearColor.Y.ToString();
                        _iniData["ClearColor"]["B"] = _clearColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Draw color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(74, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.ColorEdit3("##drawcol", ref _drawColor))
                    {
                        _iniData["DrawColor"]["R"] = _drawColor.X.ToString();
                        _iniData["DrawColor"]["G"] = _drawColor.Y.ToString();
                        _iniData["DrawColor"]["B"] = _drawColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateLineVerticeArray = true;
                    }

                    ImGui.Text("Base width (mm):");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(26, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragFloat("##line", ref _linewidth, 0.1f, 0.0f, 70.0f))
                    {
                        _iniData["Line"]["Width"] = _linewidth.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateLineVerticeArray = true;
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();

                    ImGui.Text("Grid color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(82, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.SliderFloat("##gridcol", ref _gridIntensity, 0, 1))
                    {
                        _iniData["Grid"]["Intensity"] = _gridIntensity.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Size (mm):");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(80, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragInt2("##drawsize", ref _drawSize[0], 1, 1, 1500))
                    {
                        _iniData["Draw"]["Width"] = _drawSize[0].ToString();
                        _iniData["Draw"]["Height"] = _drawSize[1].ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        Data.paperSize = new Vector2(_drawSize[0], _drawSize[1]);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Grid:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(127, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragInt2("##gridsub", ref _gridSize[0], 1, 1, 1500))
                    {
                        _iniData["Grid"]["Width"] = _gridSize[0].ToString();
                        _iniData["Grid"]["Height"] = _gridSize[1].ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Border color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(63, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.ColorEdit3("##bordercol", ref _borderColor))
                    {
                        _iniData["BorderColor"]["R"] = _borderColor.X.ToString();
                        _iniData["BorderColor"]["G"] = _borderColor.Y.ToString();
                        _iniData["BorderColor"]["B"] = _borderColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Border width (mm):");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(15, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragFloat("##borderwidth", ref _borderWidth, 1.0f, 1.0f, 200.0f))
                    {
                        _iniData["Border"]["Width"] = _borderWidth.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Depth color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(70, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.SliderFloat("##depthcol", ref _depthIntensity, 0, 1))
                    {
                        _iniData["Depth"]["Intensity"] = _depthIntensity.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateDepthVerticeArray = true;
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Shadow color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(70, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.SliderFloat("##shadowcol", ref _shadowIntensity, 0, 1))
                    {
                        _iniData["Shadow"]["Intensity"] = _shadowIntensity.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateShadowVerticeArray = true;
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                }
                else
                {
                    ImGui.PopStyleColor();
                    if (_openDrawSettingsHeader)
                    {
                        _openDrawSettingsHeader = false;
                        _iniData["Header"]["DrawSettings"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }

                ImGui.SetNextItemOpen(_openCameraHeader);
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                if (ImGui.CollapsingHeader("Viewport"))
                {
                    ImGui.PopStyleColor();
                    if (!_openCameraHeader)
                    {
                        _openCameraHeader = true;
                        _iniData["Header"]["Camera"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Spacing();

                    ImGui.Dummy(new Vector2(4, 4));
                    ImGui.Text("Position (px) ");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(86, 26));
                    ImGui.SameLine();
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
                    if (ImGui.Button("center", new Vector2(104, 24)))
                    {
                        _cameraPosition = new Vector2(_drawSize[0] * (Data.stepsPerMM * 0.5f), _drawSize[1] * (Data.stepsPerMM * 0.5f));
                        _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                        _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);

                    }
                    ImGui.PopStyleVar();


                    ImGui.SetNextItemWidth(300.0f);
                    if (ImGui.DragFloat2("##_camPos", ref _cameraPosition))
                    {
                        _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                        _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();

                    ImGui.Text("Rotation                      Zoom");
                    ImGui.SetNextItemWidth(149.0f);
                    if (ImGui.DragFloat("##_camRot", ref _cameraRotation))
                    {
                        _iniData["Camera"]["Rotation"] = _cameraRotation.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(149.0f);
                    if (ImGui.SliderInt("##_camZoom2", ref _zoom, 0, _zoomlevels.Count() - 1, _zoomlevels[_zoom].ToString() + "%%"))
                    {
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Dummy(new Vector2(4, 4));

                    //ImGui.PopStyleColor();
                    //ImGui.PopFont();

                }
                else
                {
                    ImGui.PopStyleColor();
                    if (_openCameraHeader)
                    {
                        _openCameraHeader = false;
                        _iniData["Header"]["Camera"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }

                ImGui.SetNextItemOpen(_openGeneratorHeader);
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                if (ImGui.CollapsingHeader("Generator"))
                {
                    ImGui.PopStyleColor();
                    if (!_openGeneratorHeader)
                    {
                        _openGeneratorHeader = true;
                        _iniData["Header"]["Generator"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Dummy(new Vector2(4, 4));

                    ImGui.Spacing();
                    ImGui.Text("Script");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(142, 0));
                    ImGui.SameLine();
                    ImGui.SameLine();
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("refresh##scriptfiles", new Vector2(104f, 24f)))
                    {
                        directoryNames = Directory.GetDirectories("scripts");
                        if (directoryNames.Length > 0)
                        {
                            if (directoryNames.Contains(_lastDirectory))
                            {
                                _selectedDirectory = Array.IndexOf(directoryNames, _lastDirectory);
                                scriptNames = Directory.GetFiles(_lastDirectory, "*.cs");
                            }
                            else
                            {
                                _lastDirectory = directoryNames[0];
                                scriptNames = Directory.GetFiles(directoryNames[0], "*.cs");
                                _selectedDirectory = 0;

                            }
                        }

                        if (scriptNames.Length > 0)
                        {
                            if (scriptNames.Contains(_lastScript))
                            {
                                _selectedScript = Array.IndexOf(scriptNames, _lastScript);
                            }
                            else
                            {
                                _lastScript = scriptNames[0];
                                _selectedScript = 0;
                            }
                        }
                        // scriptNames = Directory.GetFiles("scripts", "*.cs");
                    }
                    ImGui.PopStyleVar();

                    ImGui.Dummy(new Vector2(4, 4));

                    if (directoryNames.Length > 0)
                    {
                        // ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        // ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        // ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushItemWidth(300);
                        if (ImGui.BeginCombo("##comboscriptdirs", directoryNames[_selectedDirectory].Split("scripts/")[1], ImGuiComboFlags.HeightLargest))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                            for (int n = 0; n < directoryNames.Length; n++)
                            {
                                bool is_selected = (directoryNames[_selectedDirectory] == directoryNames[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(directoryNames[n].Split("scripts/")[1], is_selected))
                                    _selectedDirectory = n;

                                if (Directory.Exists(directoryNames[_selectedDirectory]))
                                {
                                    scriptNames = Directory.GetFiles(directoryNames[_selectedDirectory], "*.cs");

                                    if (scriptNames.Length > 0)
                                    {
                                        if (scriptNames.Contains(_lastScript))
                                        {
                                            _selectedScript = Array.IndexOf(scriptNames, _lastScript);
                                        }
                                        else
                                        {
                                            _selectedScript = 0;
                                        }
                                        _lastScript = scriptNames[_selectedScript];
                                        _iniData["Program"]["LastScript"] = scriptNames[_selectedScript];
                                    }
                                }
                                _lastDirectory = directoryNames[_selectedDirectory];
                                _iniData["Program"]["LastDirectory"] = directoryNames[_selectedDirectory];
                                _iniParser.WriteFile("Configuration.ini", _iniData);
                                if (is_selected)
                                {
                                    ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                                }
                            }
                            ImGui.PopStyleColor();
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        //ImGui.PopStyleColor();
                        //ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("No script folders found.");
                        ImGui.SameLine();
                        ImGui.Dummy(new Vector2(109f, 0f));
                    }
                    // ---

                    ImGui.Dummy(new Vector2(4, 4));

                    if (scriptNames.Length > 0)
                    {
                        // ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        // ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        // ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));

                        ImGui.PushItemWidth(300);

                        if (ImGui.BeginCombo("##comboscriptfiles", scriptNames[_selectedScript].Split("/").Last<String>(), ImGuiComboFlags.HeightLargest))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                            for (int n = 0; n < scriptNames.Count(); n++)
                            {
                                bool is_selected = (scriptNames[_selectedScript] == scriptNames[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(scriptNames[n].Split("/").Last<String>(), is_selected))
                                    _selectedScript = n;
                                _lastScript = scriptNames[_selectedScript];
                                _iniData["Program"]["LastScript"] = scriptNames[_selectedScript];
                                _iniParser.WriteFile("Configuration.ini", _iniData);
                                if (is_selected)
                                {
                                    ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                                }
                            }
                            ImGui.PopStyleColor();
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        //ImGui.PopStyleColor();
                        //ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("No scripts found.");
                        ImGui.SameLine();
                        ImGui.Dummy(new Vector2(109f, 0f));
                    }

                    ImGui.Dummy(new Vector2(4, 20));
                    ImGui.Spacing();

                    ImGui.SetNextItemWidth(180);
                    if (ImGui.DragInt("##RandomSeed", ref _seed))
                    {
                        _iniData["Generator"]["Seed"] = _seed.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ;
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(10f, 0f));
                    ImGui.SameLine();
                    if (ImGui.Checkbox(" Use Seed", ref _useRandomSeed))
                    {
                        _iniData["Generator"]["UseSeed"] = _useRandomSeed.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ;

                    ImGui.Spacing();
                    // ImGui.Dummy(new Vector2(0f, 10f));
                    ImGui.Dummy(new Vector2(4, 4));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

                    if (_compiler.IsGenerating)
                    {
                        if (ImGui.Button("stop", new Vector2(300, 24)))
                        {
                            _compiler.Stop();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("compile and generate", new Vector2(300, 24)))
                        {
                            Generate();
                        }
                    }


                    ImGui.PopStyleVar();

                    ImGui.Dummy(new Vector2(0, 15f));
                    ImGui.Text("File export");

                    ImGui.Spacing();
                    ImGui.SetNextItemWidth(300f);
                    if (ImGui.InputText("##expfilepath", ref _exportfilepath, 80))
                    {
                        _iniData["Generator"]["FilePath"] = _exportfilepath;
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ;
                    ImGui.SetNextItemWidth(300f);
                    if (ImGui.InputText("##expfilename", ref _exportfilename, 80))
                    {
                        _iniData["Generator"]["FileName"] = _exportfilename;
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ;

                    ImGui.Dummy(new Vector2(4, 4f));

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("choose file", new Vector2(120, 24)))
                    {
                        List<String> outputs = new List<string>();
                        string arg = " --getsavefilename ";
                        if (System.IO.Directory.Exists(_exportfilepath))
                        {
                            arg += _exportfilepath;
                        }
                        else
                        {
                            arg += Environment.GetEnvironmentVariable("HOME");
                        }
                        ProcessStartInfo startInfo = new ProcessStartInfo()
                        {
                            FileName = "kdialog",
                            Arguments = arg,
                            RedirectStandardOutput = true
                        };
                        Process proc = new Process() { StartInfo = startInfo, };
                        proc.Start();
                        StreamReader reader = proc.StandardOutput;
                        proc.WaitForExit();

                        string line;
                        string lastline = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            lastline = line;
                        }
                        if (lastline != "")
                        {
                            string[] dirs = lastline.Split('/');
                            _exportfilename = dirs.Last<String>();
                            _exportfilepath = lastline.Substring(0, lastline.Length - _exportfilename.Length);

                            _iniData["Generator"]["FileName"] = _exportfilename;
                            _iniData["Generator"]["FilePath"] = _exportfilepath;
                            _iniParser.WriteFile("Configuration.ini", _iniData);
                        }
                    }
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(54, 0));
                    ImGui.SameLine();
                    if (ImGui.Button("export file", new Vector2(120, 24)))
                    {
                        if (_exportfilename.Length > 0)
                        {
                            if (System.IO.Directory.Exists(_exportfilepath))
                            {
                                Export(_exportfilepath + _exportfilename);
                            }
                            else
                            {
                                Console.WriteLine("directory does not exist.");
                                Data.DebugConsole.Add("Directory does not exist.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No filename specified.");
                            Data.DebugConsole.Add("No filename specified.");
                        }
                    }
                    ImGui.PopStyleVar();
                    ImGui.Dummy(new Vector2(4, 4f));
                    //ImGui.PopFont();
                    ImGui.Spacing();
                    ImGui.Spacing();
                }
                else
                {
                    ImGui.PopStyleColor();
                    if (_openGeneratorHeader)
                    {
                        _openGeneratorHeader = false;
                        _iniData["Header"]["Generator"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }


                ImGui.SetNextItemOpen(_openStatisticsHeader);
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                if (ImGui.CollapsingHeader("Stats"))
                {
                    ImGui.PopStyleColor();
                    if (!_openStatisticsHeader)
                    {
                        _openStatisticsHeader = true;
                        _iniData["Header"]["Statistics"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Spacing();
                    ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

                    ImGui.Text($"Window width  : {_window.Width} px");
                    ImGui.Text($"Window height : {_window.Height} px");

                    ImGui.Spacing();
                    ImGuiIOPtr io = ImGui.GetIO();
                    ImGui.Text($"Application ms/frame: {1000.0f / io.Framerate:0.##}");
                    ImGui.Text($"Application FPS     : {io.Framerate:0.#}");
                    ImGui.Text($"Application delta   : {io.DeltaTime:0.##} ms");
                    ImGui.Spacing();
                    Process currentProcess = Process.GetCurrentProcess();
                    // long memoryUsedBytes = currentProcess.WorkingSet64; // in bytes
                    _memUsage = _memUsage * 0.9f + (currentProcess.WorkingSet64 / (1024 * 1024)) * 0.1f;
                    ImGui.Text($"Memory: {_memUsage:0#} mb");
                    ImGui.Text($"CPU Cores: {Environment.ProcessorCount} ");

                    Process process = Process.GetCurrentProcess();
                    _endTime = DateTime.UtcNow;
                    _endCpuTime = process.TotalProcessorTime;

                    double cpuUsedMs = (_endCpuTime - _startCpuTime).TotalMilliseconds;
                    double totalMsPassed = (_endTime - _startTime).TotalMilliseconds;

                    _cpuUsage = _cpuUsage * 0.95f + ((cpuUsedMs / totalMsPassed) / Environment.ProcessorCount) * 0.05f;

                    ImGui.SameLine();
                    ImGui.ProgressBar((float)_cpuUsage, new Vector2(170, 20));
                    _startCpuTime = process.TotalProcessorTime;
                    _startTime = DateTime.UtcNow;

                    int coreCount = Environment.ProcessorCount;

                    ImGui.Dummy(new Vector2(0, 12f));
                    //ImGui.PopStyleColor();
                    //ImGui.PopFont();

                }
                else
                {
                    ImGui.PopStyleColor();
                    if (_openStatisticsHeader)
                    {
                        _openStatisticsHeader = false;
                        _iniData["Header"]["Statistics"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }

                ImGui.SetNextItemOpen(_openDebugConsoleHeader);
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                if (ImGui.CollapsingHeader("Debug Console"))
                {
                    ImGui.PopStyleColor();
                    if (!_openDebugConsoleHeader)
                    {
                        _openDebugConsoleHeader = true;
                        _iniData["Header"]["DebugConsole"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();
                    ImGui.BeginChild("DebugConsoleChild", new Vector2(300f, 420f), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);
                    foreach (String msg in Data.DebugConsole)
                    {
                        ImGui.Text(msg);
                    }
                    ImGui.Dummy(new Vector2(0, 8));
                    if (Data.DebugConsole.Count > _numDebugLines)
                    {
                        ImGui.SetScrollHereY(1.0f);
                        _numDebugLines = Data.DebugConsole.Count;
                    }
                    ImGui.EndChild();

                    ImGui.Dummy(new Vector2(0, 15f));
                    //ImGui.PopStyleColor();
                    //ImGui.PopFont();
                }
                else
                {
                    ImGui.PopStyleColor();

                    if (_openDebugConsoleHeader)
                    {
                        _openDebugConsoleHeader = false;
                        _iniData["Header"]["DebugConsole"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                //ImGui.PopStyleColor();
                //ImGui.PopFont();
                ImGui.End();
            }

        }
    }
}