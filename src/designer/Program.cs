using System;
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
using System.IO.Ports;
using IniParser.Model;
using IniParser;

namespace Designer {

    public enum lineType : byte {
        none = 0,
        Straight = 1,
        QuadraticBezier = 2,
        CubicBezier = 3,
        CatmullRom = 4
    }

    public struct DrawInstruction {
        public UInt64 index;
        public lineType type;
        public sbyte dir_x;
        public sbyte dir_y;
        public Int64 x_start;
        public Int64 y_start;
        public Int64 x_end;
        public Int64 y_end;
        public Int64 delta_x;
        public Int64 delta_y;
        public Int64 delta_xx;
        public Int64 delta_yy;
        public Int64 delta_xy;
        public Int64 err;
        public Int64 steps;
    }
    public class Line {
        public Vector2[] lineData;
        public Vector3[] widthData;
        public lineType type;
        public int layer;
    }

    public class Dot {
        public Vector2 position;
        public RgbaFloat color;
        public float size;
        public int layer;
    }

    public static class Data {
        public static List<Line> lines = new List<Line>();
        public static List<Dot> dots = new List<Dot>();
        public static List<String> DebugConsole = new List<string>();
        public static List<Line> gridLines = new List<Line>();
    }

    struct DotVertex {
        public Vector2 Position;
        public RgbaFloat Color;
        public Vector2 UV;
        public float Layer;
    }

    struct LineVertex {
        public Vector2 Position;
        public RgbaFloat Color;
        public float Edge;
        public float Width;
        public float Layer;
    }

    public interface IGenerator {
        void Generate(int seed);
    }

    public static class Program {
        private static float R2 = 1.25f; // diameter (r*2) of the wheel on motor

        private static Sdl2Window _window;
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;

        //shared resources for viewport and camera
        private static DeviceBuffer _projectionBuffer;
        private static DeviceBuffer _cameraBuffer;
        private static DeviceBuffer _rotationBuffer;
        private static DeviceBuffer _translationBuffer;
        private static ResourceSet _viewportResourceSet;

        // drawtype: Line
        private static DeviceBuffer _lineVertexBuffer;
        private static Shader[] _lineShaders;
        private static Pipeline _linePipeline;
        // private static ResourceSet  _lineResourceSet;        
        private static bool _recreateLineVerticeArray = true;

        // drawtype: Dot
        private static DeviceBuffer _dotVertexBuffer;
        private static Shader[] _dotShaders;
        private static Pipeline _dotPipeline;
        // private static ResourceSet  _dotResourceSet;
        private static bool _recreateDotVerticeArray = true;

        // drawtype: GridLine
        private static DeviceBuffer _gridLineVertexBuffer;
        private static Shader[] _gridLineShaders;
        private static Pipeline _gridLinePipeline;
        // private static ResourceSet  _gridLineResourceSet;        
        private static bool _recreateGridLineVerticeArray = true;

        // private static SerialPort _serialPort;
        private static String[] serialPortNames;
        private static int _selectedSerialPort = 0;
        private static int[] baudrates = { 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000 };
        private static int _selectedSerialBaudrate = 10;

        private static ImGuiRenderer _imGuiRenderer;
        private static String[] scriptNames;
        private static int _selectedScript;

        private static int[] _drawSize = { 1, 1 };
        private static int[] _gridSize = { 1, 1 };
        private static Vector3 _gridColor = new Vector3(0.3f, 0.6f, 0.4f);
        private static float _gridIntensity = 0.5f;
        private static float _gridlinewidth = 100f;

        private static Vector3 _clearColor = new Vector3(0.0f, 0.0f, 0.0f);
        private static Vector3 _drawColor = new Vector3(0.3f, 0.6f, 0.4f);
        private static Vector2 _cameraPosition = new Vector2(0.0f, 0.0f);
        private static int _zoom = 13;
        private static float[] _zoomlevels = { 1f, 1.5f, 2f, 3.33f, 5f, 6.25f, 8.33f, 12.5f, 16.67f, 25f, 33.33f, 50f, 66.67f, 100f, 150f, 200f, 300f, 400f, 500f, 625f, 833, 1000f, 1500f, 2000f, 3000f, 4000f };
        private static float _linewidth = 3f;
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

        public static void AddNewQuadraticBezier(int i) {
            Console.WriteLine(i.ToString());
        }

        static void Main(string[] args) {
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


            if (_iniData["Line"]["Width"] != null) _linewidth = float.Parse(_iniData["Line"]["Width"]);

            if (_iniData["Draw"]["Width"] != null) _drawSize[0] = int.Parse(_iniData["Draw"]["Width"]);
            if (_iniData["Draw"]["Height"] != null) _drawSize[1] = int.Parse(_iniData["Draw"]["Height"]);

            if (_iniData["Grid"]["Width"] != null) _gridSize[0] = int.Parse(_iniData["Grid"]["Width"]);
            if (_iniData["Grid"]["Height"] != null) _gridSize[1] = int.Parse(_iniData["Grid"]["Height"]);
            if (_iniData["Grid"]["LineWidth"] != null) _gridlinewidth = float.Parse(_iniData["Grid"]["LineWidth"]);
            if (_iniData["Grid"]["Intensity"] != null) _gridIntensity = float.Parse(_iniData["Grid"]["Intensity"]);

            if (_iniData["Generator"]["FilePath"] != null) _exportfilepath = _iniData["Generator"]["FilePath"];
            if (_iniData["Generator"]["FileName"] != null) _exportfilename = _iniData["Generator"]["FileName"];
            //get list of serialports
            serialPortNames = SerialPort.GetPortNames();

            //get a list of script files
            scriptNames = Directory.GetFiles("scripts", "*.cs");

            // SDL_WindowFlags.Fullscreen;
            // SDL_WindowFlags.Maximized;
            // SDL_WindowFlags.Minimized;
            // SDL_WindowFlags.FullScreenDesktop;
            // SDL_WindowFlags.Hidden;
            // SDL_WindowFlags.OpenGL;
            // SDL_WindowFlags.Resizable;

            //create Window            
            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable | SDL_WindowFlags.Shown;
            _window = new Sdl2Window("DMP Designer v0.1.0", 0, 0, 800, 1075, flags, false);

            Sdl2Native.SDL_ShowCursor(0);

            //create Graphics Device
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(
                _window,
                new GraphicsDeviceOptions {
                    SwapchainDepthFormat = PixelFormat.R32_Float,
                    ResourceBindingModel = ResourceBindingModel.Improved,
                    PreferStandardClipSpaceYDirection = true,
                    PreferDepthRangeZeroToOne = true,
                    SyncToVerticalBlank = true,
                    Debug = false
                },
                GraphicsBackend.Vulkan
            );

            _window.Resized += () => {
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

            //// -------------------      ------------------------- ////

            //create the commandlist, which lets us record and execute graphics commands
            _commandList = factory.CreateCommandList();

            //create ImGuiRenderer
            _imGuiRenderer = new ImGuiRenderer(_graphicsDevice, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height, ColorSpaceHandling.Linear);

            Stopwatch sw = Stopwatch.StartNew();
            double previousTime = sw.Elapsed.TotalSeconds;

            //RENDERING LOOP
            while (_window.Exists) {
                InputSnapshot snapshot = _window.PumpEvents();
                if (_window.Exists) {
                    double newTime = sw.Elapsed.TotalSeconds;
                    float deltaSeconds = (float)(newTime - previousTime);

                    _imGuiRenderer.UpdateInput(snapshot, deltaSeconds, _window.Width, _window.Height);
                    ImGui.NewFrame();
                    UpdateInput(snapshot, deltaSeconds);

                    rebuildGUI();
                    ImGui.Render();

                    //recreate the buffers used by gui, prepare for drawing
                    _imGuiRenderer.UpdateGeometry(_graphicsDevice);

                    //We stage all drawwing commands in the commandlist
                    _commandList.Begin();
                    //set and clear framebuffer
                    _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                    _commandList.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                    _commandList.ClearDepthStencil(1f);

                    //create view and projection matrix
                    // _commandList.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreateOrthographic(_window.Width * 100f / _zoomlevels[_zoom], _window.Height * 100f / _zoomlevels[_zoom], 0.1f, 10000f));
                    _commandList.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreateOrthographic(_window.Width * (51200f / (R2 * MathF.PI)) / _zoomlevels[_zoom], _window.Height * (51200f / (R2 * MathF.PI)) / _zoomlevels[_zoom], 0.1f, 50000f));
                    _commandList.UpdateBuffer(_cameraBuffer, 0, Matrix4x4.CreateLookAt(new Vector3(_cameraPosition.X, _cameraPosition.Y, 1000), new Vector3(_cameraPosition.X, _cameraPosition.Y, 0f), Vector3.UnitY));
                    _commandList.UpdateBuffer(_rotationBuffer, 0, Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, _cameraRotation * 0.01745329252f));
                    _commandList.UpdateBuffer(_translationBuffer, 0, new Vector4(_drawSize[0] * (2560f / (R2 * MathF.PI)), _drawSize[1] * (2560f / (R2 * MathF.PI)), 0, 1));

                    UpdateGridLineGeometry();
                    DrawGridLines();

                    //Update our geometry if needed and draw
                    UpdateLineGeometry();
                    DrawLines();

                    UpdateDotGeometry();
                    DrawDots();

                    ////Draw UI
                    _imGuiRenderer.Draw(_commandList);

                    // End() must be called before commands can be submitted for execution.
                    // Once commands have been submitted, the rendered image can be presented to the application window.
                    _commandList.End();
                    _graphicsDevice.SubmitCommands(_commandList);
                    _graphicsDevice.SwapBuffers();
                    _graphicsDevice.WaitForIdle();

                    previousTime = newTime;
                }
            }

            //CLEAN UP
            _linePipeline.Dispose();
            _lineVertexBuffer.Dispose();
            foreach (Shader shader in _lineShaders) {
                shader.Dispose();
            }

            _dotPipeline.Dispose();
            _dotVertexBuffer.Dispose();
            foreach (Shader shader in _dotShaders) {
                shader.Dispose();
            }

            _gridLinePipeline.Dispose();
            _gridLineVertexBuffer.Dispose();
            foreach (Shader shader in _gridLineShaders) {
                shader.Dispose();
            }
            _cameraBuffer.Dispose();
            _projectionBuffer.Dispose();
            _rotationBuffer.Dispose();
            _translationBuffer.Dispose();

            _imGuiRenderer.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();

            //DONE
        }

        private static void Export(string s) {
            _exporter.Export(s);
            _recreateDotVerticeArray = true;
        }

        private static void Generate() {
            if (File.Exists(scriptNames[_selectedScript])) {
                //run script
                if (!_useRandomSeed) {
                    _seed = new Random().Next();
                }
                if (_compiler.CompileAndRun(scriptNames[_selectedScript], _seed) == 1) {
                    Console.WriteLine("Scipt executed succesfully.");
                    Data.DebugConsole.Add(("Scipt executed succesfully."));
                } else {
                    Console.WriteLine("Script execution failed..");
                    Data.DebugConsole.Add(("Scipt execution failed.."));
                }
                _recreateDotVerticeArray = true;
                _recreateLineVerticeArray = true;
            }
        }

        private static void UpdateDotGeometry() {
            if (_recreateDotVerticeArray) {
                DotVertex[] dotVertices = new DotVertex[Data.dots.Count * 4];
                for (int d = 0; d < Data.dots.Count; d++) {
                    dotVertices[d * 4 + 0] = new DotVertex();
                    dotVertices[d * 4 + 0].Position = new Vector2(Data.dots[d].position.X - Data.dots[d].size, Data.dots[d].position.Y + Data.dots[d].size);
                    dotVertices[d * 4 + 0].Color = Data.dots[d].color;
                    dotVertices[d * 4 + 0].UV = new Vector2(0f, 1f); ;
                    dotVertices[d * 4 + 0].Layer = Data.dots[d].layer;

                    dotVertices[d * 4 + 1] = new DotVertex();
                    dotVertices[d * 4 + 1].Position = new Vector2(Data.dots[d].position.X + Data.dots[d].size, Data.dots[d].position.Y + Data.dots[d].size);
                    dotVertices[d * 4 + 1].Color = Data.dots[d].color;
                    dotVertices[d * 4 + 1].UV = new Vector2(1f, 1f);
                    dotVertices[d * 4 + 1].Layer = Data.dots[d].layer;

                    dotVertices[d * 4 + 2] = new DotVertex();
                    dotVertices[d * 4 + 2].Position = new Vector2(Data.dots[d].position.X - Data.dots[d].size, Data.dots[d].position.Y - Data.dots[d].size);
                    dotVertices[d * 4 + 2].Color = Data.dots[d].color;
                    dotVertices[d * 4 + 2].UV = new Vector2(0f, 0f); ;
                    dotVertices[d * 4 + 2].Layer = Data.dots[d].layer;

                    dotVertices[d * 4 + 3] = new DotVertex();
                    dotVertices[d * 4 + 3].Position = new Vector2(Data.dots[d].position.X + Data.dots[d].size, Data.dots[d].position.Y - Data.dots[d].size);
                    dotVertices[d * 4 + 3].Color = Data.dots[d].color;
                    dotVertices[d * 4 + 3].UV = new Vector2(1f, 0f); ;
                    dotVertices[d * 4 + 3].Layer = Data.dots[d].layer;
                }
                _graphicsDevice.DisposeWhenIdle(_dotVertexBuffer);
                _dotVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)Data.dots.Count * 4 * 36, BufferUsage.VertexBuffer));
                _graphicsDevice.UpdateBuffer(_dotVertexBuffer, 0, dotVertices);
                _recreateDotVerticeArray = false;
            }
        }

        private static void UpdateLineGeometry() {
            if (_recreateLineVerticeArray) {
                int vCount = 0;
                List<LineVertex> lineVertices = new List<LineVertex>();

                for (int l = 0; l < Data.lines.Count; l++) {
                    if (Data.lines[l].type == lineType.Straight) {
                        for (int ctr = 0; ctr < Data.lines[l].lineData.Length; ctr++) {

                            LineVertex v1 = new LineVertex();
                            v1.Width = _linewidth;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = _linewidth;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector2 vnorm;
                            //line start
                            if (ctr == 0) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr + 1], Data.lines[l].lineData[ctr]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = Data.lines[l].lineData[ctr] - vperp * _linewidth;
                                v2.Position = Data.lines[l].lineData[ctr] + vperp * _linewidth;
                            }

                            if (ctr > 0 && ctr + 1 < Data.lines[l].lineData.Length) {
                                Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr], Data.lines[l].lineData[ctr - 1])); //incoming line
                                Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr + 1], Data.lines[l].lineData[ctr])); //outgoing line
                                vnorm = Vector2.Normalize(new Vector2((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y)));
                                float len = _linewidth / Vector2.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = Data.lines[l].lineData[ctr] - vperp * (len);
                                v2.Position = Data.lines[l].lineData[ctr] + vperp * (len);
                            }

                            //line end
                            if (ctr + 1 == Data.lines[l].lineData.Length) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr], Data.lines[l].lineData[ctr - 1]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = Data.lines[l].lineData[ctr] - vperp * _linewidth;
                                v2.Position = Data.lines[l].lineData[ctr] + vperp * _linewidth;
                            }

                            lineVertices.Insert(vCount + ctr * 2, v1);
                            lineVertices.Insert(vCount + ctr * 2 + 1, v2);

                        }
                        vCount += Data.lines[l].lineData.Length * 2;
                    }

                    if (Data.lines[l].type == lineType.QuadraticBezier) {
                        //generate points
                        Vector3[] points = new Vector3[101];
                        Vector3 A = Data.lines[l].widthData[0];
                        Vector3 B = Data.lines[l].widthData[1];
                        Vector3 C = Data.lines[l].widthData[2];

                        for (int p = 0; p <= 100; p++) {
                            float t = ((float)p) / 100f;
                            points[p] = (1f - t) * (1f - t) * A + 2 * (1f - t) * t * B + t * t * C;
                        }
                        for (int pctr = 0; pctr < points.Length; pctr++) {
                            LineVertex v1 = new LineVertex();
                            v1.Width = points[pctr].Z;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = points[pctr].Z;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector3 vnorm;
                            //line start
                            if (pctr == 0) {
                                vnorm = Vector3.Normalize(Vector3.Subtract(points[pctr + 1], points[pctr]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X) * points[pctr].Z*_linewidth;
                                v1.Position = new Vector2(points[pctr].X - vperp.X,points[pctr].Y - vperp.Y);
                                v2.Position = new Vector2(points[pctr].X + vperp.X,points[pctr].Y + vperp.Y);
                                // v2.Position = Vector2((points[pctr] + vperp * v2.Width).X,;
                            }

                            if (pctr > 0 && pctr + 1 < points.Length) {
                                Vector3 vnorm1 = Vector3.Normalize(Vector3.Subtract(points[pctr], points[pctr - 1])); //incoming line
                                Vector3 vnorm2 = Vector3.Normalize(Vector3.Subtract(points[pctr + 1], points[pctr])); //outgoing line
                                vnorm = Vector3.Normalize(new Vector3((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y), (vnorm1.Z + vnorm2.Z)));
                                // float len = (_linewidth) / Vector3.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X) * ((points[pctr].Z*_linewidth)/ Vector3.Dot(vnorm1, vnorm));
                                v1.Position = new Vector2(points[pctr].X - vperp.X,points[pctr].Y - vperp.Y);
                                v2.Position = new Vector2(points[pctr].X + vperp.X,points[pctr].Y + vperp.Y);
                            }

                            //line end
                            if (pctr + 1 == points.Length) {
                                vnorm = Vector3.Normalize(Vector3.Subtract(points[pctr], points[pctr - 1]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X) * points[pctr].Z*_linewidth;
                                v1.Position = new Vector2(points[pctr].X - vperp.X,points[pctr].Y - vperp.Y);
                                v2.Position = new Vector2(points[pctr].X + vperp.X,points[pctr].Y + vperp.Y);                                
                            }

                            lineVertices.Insert(vCount + pctr * 2, v1);
                            lineVertices.Insert(vCount + pctr * 2 + 1, v2);
                        }
                        vCount += points.Length * 2;

                    }

                    if (Data.lines[l].type == lineType.CatmullRom) {
                        //generate points
                        Vector2[] points = new Vector2[101];
                        Vector2 A = Data.lines[l].lineData[0];
                        Vector2 B = Data.lines[l].lineData[1];
                        Vector2 C = Data.lines[l].lineData[2];

                        for (int p = 0; p <= 100; p++) {
                            float t = ((float)p) / 100f;
                            points[p] = (1f - t) * (1f - t) * A + 2 * (1f - t) * t * B + t * t * C;
                        }
                        for (int pctr = 0; pctr < points.Length; pctr++) {
                            LineVertex v1 = new LineVertex();
                            v1.Width = _linewidth;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = _linewidth;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector2 vnorm;
                            //line start
                            if (pctr == 0) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(points[pctr + 1], points[pctr]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * v1.Width;
                                v2.Position = points[pctr] + vperp * v2.Width;
                            }

                            if (pctr > 0 && pctr + 1 < points.Length) {
                                Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(points[pctr], points[pctr - 1])); //incoming line
                                Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(points[pctr + 1], points[pctr])); //outgoing line
                                vnorm = Vector2.Normalize(new Vector2((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y)));
                                float len = (_linewidth) / Vector2.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * (len);
                                v2.Position = points[pctr] + vperp * (len);
                            }

                            //line end
                            if (pctr + 1 == points.Length) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(points[pctr], points[pctr - 1]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * v1.Width;
                                v2.Position = points[pctr] + vperp * v2.Width;
                            }

                            lineVertices.Insert(vCount + pctr * 2, v1);
                            lineVertices.Insert(vCount + pctr * 2 + 1, v2);
                        }
                        vCount += points.Length * 2;

                    }

                    if (Data.lines[l].type == lineType.CubicBezier) {
                        //generate points
                        Vector2[] points = new Vector2[400];
                        Vector2 A = Data.lines[l].lineData[0];
                        Vector2 B = Data.lines[l].lineData[1];
                        Vector2 C = Data.lines[l].lineData[2];
                        Vector2 D = Data.lines[l].lineData[3];

                        for (int p = 0; p < 400; p++) {
                            float t = ((float)p) / 400f;
                            points[p] = (1f - t) * (1f - t) * (1f - t) * A + 3 * (1f - t) * (1f - t) * t * B + 3 * (1f - t) * t * t * C + t * t * t * D;
                        }

                        for (int pctr = 0; pctr < points.Length; pctr++) {
                            LineVertex v1 = new LineVertex();
                            v1.Width = _linewidth;
                            v1.Edge = 0;
                            v1.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            LineVertex v2 = new LineVertex();
                            v2.Width = _linewidth;
                            v2.Edge = 1;
                            v2.Color = new RgbaFloat(_drawColor.X, _drawColor.Y, _drawColor.Z, 1.0f);

                            Vector2 vnorm;
                            //line start
                            if (pctr == 0) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(points[pctr + 1], points[pctr]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * _linewidth;
                                v2.Position = points[pctr] + vperp * _linewidth;
                            }

                            if (pctr > 0 && pctr + 1 < points.Length) {
                                Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(points[pctr], points[pctr - 1])); //incoming line
                                Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(points[pctr + 1], points[pctr])); //outgoing line
                                vnorm = Vector2.Normalize(new Vector2((vnorm1.X + vnorm2.X), (vnorm1.Y + vnorm2.Y)));
                                float len = _linewidth / Vector2.Dot(vnorm1, vnorm);
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * (len);
                                v2.Position = points[pctr] + vperp * (len);
                            }

                            //line end
                            if (pctr + 1 == points.Length) {
                                vnorm = Vector2.Normalize(Vector2.Subtract(points[pctr], points[pctr - 1]));
                                Vector2 vperp = new Vector2(-vnorm.Y, vnorm.X);
                                v1.Position = points[pctr] - vperp * _linewidth;
                                v2.Position = points[pctr] + vperp * _linewidth;
                            }

                            lineVertices.Insert(vCount + pctr * 2, v1);
                            lineVertices.Insert(vCount + pctr * 2 + 1, v2);
                        }
                        vCount += points.Length * 2;
                    }
                }

                _graphicsDevice.DisposeWhenIdle(_lineVertexBuffer);
                _lineVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)vCount * 36, BufferUsage.VertexBuffer));

                _graphicsDevice.UpdateBuffer(_lineVertexBuffer, 0, lineVertices.ToArray());
                _recreateLineVerticeArray = false;
            }
        }

        private static void UpdateGridLineGeometry() {
            {
                Data.gridLines.Clear();
                int layer = 0;
                int lastlayer = 0;
                if (_gridSize[0] > 0 && _gridSize[1] > 0) {
                    int w = (_drawSize[0] / _gridSize[0]) * (int) (5120f / (R2 * MathF.PI));
                    int h = (_drawSize[1] / _gridSize[1]) * (int) (5120f / (R2 * MathF.PI));
                    for (int xtile = 0; xtile < _gridSize[0]; xtile++) {
                        if (lastlayer == layer) layer = (layer + 1) % 2;
                        lastlayer = layer;
                        for (int ytile = 0; ytile < _gridSize[1]; ytile++) {
                            layer = (layer + 1) % 2;
                            Line l = new Line();
                            l.type = lineType.Straight;
                            l.lineData = new Vector2[] { new Vector2(xtile * w, ytile * h), new Vector2(w, h) };
                            l.layer = layer;
                            Data.gridLines.Add(l);
                        }
                    }
                }
            }
            if (_recreateGridLineVerticeArray) {
                int vCount = 0;
                int cCount = 0;
                List<LineVertex> gridLineVertices = new List<LineVertex>();

                for (int l = 0; l < Data.gridLines.Count; l++) {
                    if (Data.gridLines[l].type == lineType.Straight) {
                        RgbaFloat c = new RgbaFloat(0, 0, 0, 0);

                        if (Data.gridLines[l].layer == 0) {
                            float r = 0f, g = 0f, b = 0f;
                            if (_clearColor.X > 0.5) r = _clearColor.X - 0.5f * _gridIntensity;
                            if (_clearColor.X <= 0.5) r = _clearColor.X + 0.5f * _gridIntensity;
                            if (_clearColor.Y > 0.5) g = _clearColor.Y - 0.5f * _gridIntensity;
                            if (_clearColor.Y <= 0.5) g = _clearColor.Y + 0.5f * _gridIntensity;
                            if (_clearColor.Z > 0.5) b = _clearColor.Z - 0.5f * _gridIntensity;
                            if (_clearColor.Z <= 0.5) b = _clearColor.Z + 0.5f * _gridIntensity;
                            c = new RgbaFloat(r, g, b, 1.0f);
                        }
                        if (Data.gridLines[l].layer == 1) {
                            float r = 0f, g = 0f, b = 0f;
                            if (_clearColor.X > 0.5) r = _clearColor.X - 1.0f * _gridIntensity;
                            if (_clearColor.X <= 0.5) r = _clearColor.X + 1.0f * _gridIntensity;
                            if (_clearColor.Y > 0.5) g = _clearColor.Y - 1.0f * _gridIntensity;
                            if (_clearColor.Y <= 0.5) g = _clearColor.Y + 1.0f * _gridIntensity;
                            if (_clearColor.Z > 0.5) b = _clearColor.Z - 1.0f * _gridIntensity;
                            if (_clearColor.Z <= 0.5) b = _clearColor.Z + 1.0f * _gridIntensity;
                            c = new RgbaFloat(r, g, b, 1.0f);
                        }
                        //c = new RgbaFloat(_clearColor.X+0.05f, _clearColor.Y+0.05f, _clearColor.Z+0.05f, 1.0f);

                        LineVertex v1 = new LineVertex();
                        v1.Width = _gridlinewidth;
                        v1.Edge = 0;
                        // v1.Color = new RgbaFloat(_gridColor.X+_clearColor.X, _gridColor.Y, _gridColor.Z, 1.0f);
                        v1.Color = c;
                        v1.Position = Data.gridLines[l].lineData[0];

                        LineVertex v2 = new LineVertex();
                        v2.Width = _gridlinewidth;
                        v2.Edge = 1;
                        v2.Color = c;
                        v2.Position = Data.gridLines[l].lineData[0] + new Vector2(0, Data.gridLines[l].lineData[1].Y);

                        LineVertex v3 = new LineVertex();
                        v3.Width = _gridlinewidth;
                        v3.Edge = 0;
                        // v3.Color = new RgbaFloat(_gridColor.X, _gridColor.Y, _gridColor.Z, 1.0f);
                        v3.Color = c;
                        v3.Position = Data.gridLines[l].lineData[0] + new Vector2(Data.gridLines[l].lineData[1].X, 0);

                        LineVertex v4 = new LineVertex();
                        v4.Width = _gridlinewidth;
                        v4.Edge = 1;
                        // v4.Color = new RgbaFloat(_gridColor.X, _gridColor.Y, _gridColor.Z, 1.0f);
                        v4.Color = c;
                        v4.Position = Data.gridLines[l].lineData[0] + Data.gridLines[l].lineData[1];

                        gridLineVertices.Insert(vCount + 0, v1);
                        gridLineVertices.Insert(vCount + 1, v2);
                        gridLineVertices.Insert(vCount + 2, v3);
                        gridLineVertices.Insert(vCount + 3, v4);
                        vCount += 4;
                        cCount++;
                    }
                }

                _graphicsDevice.DisposeWhenIdle(_gridLineVertexBuffer);
                _gridLineVertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)vCount * 36, BufferUsage.VertexBuffer));

                _graphicsDevice.UpdateBuffer(_gridLineVertexBuffer, 0, gridLineVertices.ToArray());
                _recreateGridLineVerticeArray = false;
            }
        }

        private static void DrawDots() {
            _commandList.SetPipeline(_dotPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _dotVertexBuffer);

            uint vStart = 0;
            uint vLength = 4;
            for (int l = 0; l < Data.dots.Count; l++) {
                _commandList.Draw(vertexCount: vLength, instanceCount: 1, vertexStart: vStart, instanceStart: 0);
                vStart += vLength;
            }
        }

        private static void DrawLines() {
            _commandList.SetPipeline(_linePipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _lineVertexBuffer);

            uint vStart = 0;
            uint vLength = 0;
            for (int l = 0; l < Data.lines.Count; l++) {
                // if (Data.lines[l].type == lineType.CubicBezier) {
                //     vLength = (uint)Data.lines[l].lineData.Length * 2 * 100;
                // }
                if (Data.lines[l].type == lineType.QuadraticBezier) {
                    // vLength = (uint)Data.lines[l].lineData.Length + 1;
                    vLength = 101*2;
                }
                // if (Data.lines[l].type == lineType.Straight) {
                //     vLength = (uint)Data.lines[l].lineData.Length * 2;
                // }
                _commandList.Draw(vertexCount: vLength, instanceCount: 1, vertexStart: vStart, instanceStart: 0);
                vStart += vLength;
            }
        }

        private static void DrawGridLines() {
            _commandList.SetPipeline(_gridLinePipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _gridLineVertexBuffer);

            uint vStart = 0;
            uint vLength = 0;
            for (int l = 0; l < Data.gridLines.Count; l++) {
                if (Data.gridLines[l].type == lineType.Straight) {
                    vLength = (uint)Data.gridLines[l].lineData.Length * 2;
                }
                _commandList.Draw(vertexCount: vLength, instanceCount: 1, vertexStart: vStart, instanceStart: 0);
                vStart += vLength;
            }
        }

        private static void UpdateInput(InputSnapshot snapshot, float deltaSeconds) {
            //handle input that is not used by ImGui
            if (!ImGui.GetIO().WantCaptureMouse) {
                // if (snapshot.IsMouseDown(MouseButton.Left)) {
                // } else {
                //     ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                // }
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    // Console.WriteLine("leftDoubleClicked");
                }
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                    // Console.WriteLine("leftReleased");
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                }

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left)) {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                    _cameraPosition = _cameraPosition - new Vector2(ImGui.GetMouseDragDelta().X * ((25600f / MathF.PI) / _zoomlevels[_zoom]), -ImGui.GetMouseDragDelta().Y * ((25600f / MathF.PI) / _zoomlevels[_zoom]));
                    _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                    _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                    _iniParser.WriteFile("Configuration.ini", _iniData);

                    ImGui.ResetMouseDragDelta();
                }

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
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

                if (snapshot.WheelDelta > 0) {
                    // _cameraPosition = _cameraPosition - new Vector3(0,0,10);
                    if (_zoom < _zoomlevels.Count() - 1) {
                        _zoom += 1;
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);

                    }
                    // Console.WriteLine("zoomin");
                }
                if (snapshot.WheelDelta < 0) {
                    // Console.WriteLine("zoomout");
                    // _cameraPosition = _cameraPosition + new Vector3(0,0,10);
                    if (_zoom > 0) {
                        _zoom -= 1;
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }


                //check if Keyboared in use by ImGui
                if (!ImGui.GetIO().WantCaptureKeyboard) {
                    for (int i = 0; i < snapshot.KeyEvents.Count; i++) {
                        KeyEvent ke = snapshot.KeyEvents[i];
                        if (ke.Down) {
                            // Console.Write("keydown: ");
                            // Console.WriteLine(ke.Key);
                            // Data.DebugConsole.Add("keydown: " + ke.Key.ToString());
                        } else {
                            // Console.Write("keyup: ");
                            // Console.WriteLine(ke.Key);
                            // Data.DebugConsole.Add("keyup: " + ke.Key.ToString());
                        }
                    }
                }
            }
        }

        private static void rebuildGUI() {
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.BeginMenu("File")) {
                    if (ImGui.MenuItem("Quit", "", false, true)) {
                        _window.Close();
                    };
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Window")) {
                    ImGui.MenuItem("Settings", string.Empty, ref _showSettingsWindow, true);
                    ImGui.MenuItem("ImGui Demo", string.Empty, ref _showImGuiDemoWindow, true);
                    ImGui.Separator();
                    ImGui.MenuItem("Draw Settings", string.Empty, ref _openDrawSettingsHeader, true);
                    ImGui.MenuItem("Camera", string.Empty, ref _openCameraHeader, true);
                    ImGui.MenuItem("Serial Monitor", string.Empty, ref _openSerialMonitorHeader, true);
                    ImGui.MenuItem("Statistics", string.Empty, ref _openStatisticsHeader, true);
                    ImGui.MenuItem("Debug Console", string.Empty, ref _openDebugConsoleHeader, true);
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            if (_showImGuiDemoWindow) {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            }

            if (_showSettingsWindow) {
                ImGui.SetNextWindowPos(new Vector2(_window.Width - 225f, 20f));
                ImGui.SetNextWindowSize(new Vector2(225f, _window.Height - 30f));
                ImGui.Begin("Settings", ref _showSettingsWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openDrawSettingsHeader);
                if (ImGui.CollapsingHeader("Draw Settings")) {
                    ImGui.PushStyleColor(ImGuiCol.Text, textcolor1);

                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openDrawSettingsHeader) {
                        _openDrawSettingsHeader = true;
                        _iniData["Header"]["DrawSettings"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();

                    ImGui.Text("Clear color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(2, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.ColorEdit3("##clearcol", ref _clearColor)) {
                        _iniData["ClearColor"]["R"] = _clearColor.X.ToString();
                        _iniData["ClearColor"]["G"] = _clearColor.Y.ToString();
                        _iniData["ClearColor"]["B"] = _clearColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Draw color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(3, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.ColorEdit3("##drawcol", ref _drawColor)) {
                        _iniData["DrawColor"]["R"] = _drawColor.X.ToString();
                        _iniData["DrawColor"]["G"] = _drawColor.Y.ToString();
                        _iniData["DrawColor"]["B"] = _drawColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateLineVerticeArray = true;
                    }

                    ImGui.Text("Line width:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(7, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragFloat("##line", ref _linewidth, 100f, 0.1f, 20000f)) {
                        _iniData["Line"]["Width"] = _linewidth.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateLineVerticeArray = true;
                    };


                    ImGui.Spacing();
                    ImGui.Spacing();

                    ImGui.Text("Grid color:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(10, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.SliderFloat("##gridcol", ref _gridIntensity, 0, 1)) {
                        _iniData["Grid"]["Intensity"] = _gridIntensity.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    }

                    ImGui.Text("Size (mm):");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(8, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragInt2("##drawsize", ref _drawSize[0], 1, 1, 1500)) {
                        _iniData["Draw"]["Width"] = _drawSize[0].ToString();
                        _iniData["Draw"]["Height"] = _drawSize[1].ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    };

                    ImGui.Text("Grid:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(46, 0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(130);
                    if (ImGui.DragInt2("##gridsub", ref _gridSize[0], 1, 1, 1500)) {
                        _iniData["Grid"]["Width"] = _gridSize[0].ToString();
                        _iniData["Grid"]["Height"] = _gridSize[1].ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                        _recreateGridLineVerticeArray = true;
                    };

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.PopStyleColor();
                    ImGui.PopFont();

                } else {
                    if (_openDrawSettingsHeader) {
                        _openDrawSettingsHeader = false;
                        _iniData["Header"]["DrawSettings"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openCameraHeader);
                if (ImGui.CollapsingHeader("Camera")) {
                    ImGui.PushStyleColor(ImGuiCol.Text, textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openCameraHeader) {
                        _openCameraHeader = true;
                        _iniData["Header"]["Camera"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Spacing();

                    ImGui.Text("Position (px) ");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(16, 0));
                    ImGui.SameLine();
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("center", new Vector2(104, 20))) {
                        _cameraPosition = new Vector2(_drawSize[0] * (2560f / (R2 * MathF.PI)), _drawSize[1] * (2560f / (R2 * MathF.PI)));
                        _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                        _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);

                    }
                    ImGui.PopStyleVar();


                    ImGui.SetNextItemWidth(212.0f);
                    if (ImGui.DragFloat2("##_camPos", ref _cameraPosition)) {
                        _iniData["Camera"]["PosX"] = _cameraPosition.X.ToString();
                        _iniData["Camera"]["PosY"] = _cameraPosition.Y.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    };
                    ImGui.Spacing();
                    ImGui.Spacing();

                    ImGui.Text("Rotation              Zoom");
                    ImGui.SetNextItemWidth(104.0f);
                    if (ImGui.DragFloat("##_camRot", ref _cameraRotation)) {
                        _iniData["Camera"]["Rotation"] = _cameraRotation.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(104.0f);
                    if (ImGui.SliderInt("##_camZoom2", ref _zoom, 0, _zoomlevels.Count() - 1, _zoomlevels[_zoom].ToString() + "%%")) {
                        _iniData["Camera"]["Zoom"] = _zoom.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                    ImGui.Spacing();
                    ImGui.Spacing();

                    ImGui.PopStyleColor();
                    ImGui.PopFont();

                } else {
                    if (_openCameraHeader) {
                        _openCameraHeader = false;
                        _iniData["Header"]["Camera"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                //// Generator
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openGeneratorHeader);
                if (ImGui.CollapsingHeader("Generator")) {
                    ImGui.PushStyleColor(ImGuiCol.Text, textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);

                    if (!_openGeneratorHeader) {
                        _openGeneratorHeader = true;
                        _iniData["Header"]["Generator"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();
                    ImGui.Text("Script");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(62, 0));
                    ImGui.SameLine();
                    ImGui.SameLine();
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("refresh##scriptfiles", new Vector2(104f, 20f))) {
                        scriptNames = Directory.GetFiles("scripts", "*.cs");
                    }
                    ImGui.PopStyleVar();

                    if (scriptNames.Count() > 0) {
                        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushItemWidth(210);
                        if (ImGui.BeginCombo("##comboscriptfiles", scriptNames[_selectedScript].Split("scripts/")[1])) {
                            for (int n = 0; n < scriptNames.Count(); n++) {
                                bool is_selected = (scriptNames[_selectedScript] == scriptNames[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(scriptNames[n].Split("scripts/")[1], is_selected))
                                    _selectedScript = n;
                                if (is_selected) {
                                    ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                    } else {
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("No scripts found.");
                        ImGui.SameLine();
                        ImGui.Dummy(new Vector2(109f, 0f));
                    }

                    ImGui.Spacing();

                    ImGui.SetNextItemWidth(110);
                    ImGui.DragInt("##RandomSeed", ref _seed);
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(5f, 0f));
                    ImGui.SameLine();
                    ImGui.Checkbox(" Use Seed", ref _useRandomSeed);

                    ImGui.Spacing();
                    // ImGui.Dummy(new Vector2(0f, 10f));

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("compile and generate", new Vector2(210, 22))) {
                        Generate();
                    }
                    ImGui.PopStyleVar();

                    ImGui.Dummy(new Vector2(0, 15f));
                    ImGui.Text("File export");

                    ImGui.Spacing();
                    ImGui.SetNextItemWidth(210f);
                    if (ImGui.InputText("##expfilepath", ref _exportfilepath, 80)) {
                        _iniData["Generator"]["FilePath"] = _exportfilepath;
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    };
                    ImGui.SetNextItemWidth(210f);
                    if (ImGui.InputText("##expfilename", ref _exportfilename, 80)) {
                        _iniData["Generator"]["FileName"] = _exportfilename;
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    };

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
                    if (ImGui.Button("choose file", new Vector2(100, 22))) {
                        List<String> outputs = new List<string>();
                        string arg = " --getsavefilename ";
                        if (System.IO.Directory.Exists(_exportfilepath)) {
                            arg += _exportfilepath;
                        } else {
                            arg += Environment.GetEnvironmentVariable("HOME");
                        }
                        ProcessStartInfo startInfo = new ProcessStartInfo() {
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
                        while ((line = reader.ReadLine()) != null) {
                            lastline = line;
                        }
                        if (lastline != "") {
                            string[] dirs = lastline.Split('/');
                            _exportfilename = dirs.Last<String>();
                            _exportfilepath = lastline.Substring(0, lastline.Length - _exportfilename.Length);

                            _iniData["Generator"]["FileName"] = _exportfilename;
                            _iniData["Generator"]["FilePath"] = _exportfilepath;
                            _iniParser.WriteFile("Configuration.ini", _iniData);
                        }
                    }
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(4, 0));
                    ImGui.SameLine();
                    if (ImGui.Button("export file", new Vector2(100, 22))) {
                        if (_exportfilename.Length > 0) {
                            if (System.IO.Directory.Exists(_exportfilepath)) {
                                Export(_exportfilepath + _exportfilename);
                            } else {
                                Console.WriteLine("directory does not exist.");
                                Data.DebugConsole.Add("Directory does not exist.");
                            }
                        } else {
                            Console.WriteLine("No filename specified.");
                            Data.DebugConsole.Add("No filename specified.");
                        }
                    }

                    ImGui.PopStyleVar();

                    ImGui.PopFont();
                    ImGui.Spacing();
                    ImGui.Spacing();
                } else {
                    if (_openGeneratorHeader) {
                        _openGeneratorHeader = false;
                        _iniData["Header"]["Generator"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                
                //// Statistics 
                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openStatisticsHeader);
                if (ImGui.CollapsingHeader("Stats")) {
                    ImGui.PushStyleColor(ImGuiCol.Text, textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openStatisticsHeader) {
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

                    ImGui.Dummy(new Vector2(0, 12f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();

                } else {
                    if (_openStatisticsHeader) {
                        _openStatisticsHeader = false;
                        _iniData["Header"]["Statistics"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                ImGui.PushStyleColor(ImGuiCol.Text, textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openDebugConsoleHeader);
                if (ImGui.CollapsingHeader("Debug Console")) {
                    ImGui.PushStyleColor(ImGuiCol.Text, textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);

                    if (!_openDebugConsoleHeader) {
                        _openDebugConsoleHeader = true;
                        _iniData["Header"]["DebugConsole"] = "true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }

                    ImGui.Spacing();
                    ImGui.SetNextWindowSizeConstraints(new Vector2(100f, 100f), new Vector2(1500f, 200f));
                    ImGui.BeginChild("DebugConsoleChild", new Vector2(300f, 240f), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);
                    foreach (String msg in Data.DebugConsole) {
                        ImGui.Text(msg);
                    }
                    ImGui.EndChild();

                    ImGui.Dummy(new Vector2(0, 15f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                } else {
                    if (_openDebugConsoleHeader) {
                        _openDebugConsoleHeader = false;
                        _iniData["Header"]["DebugConsole"] = "false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.End();
            }

        }
    }
}