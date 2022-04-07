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


namespace Janus
{
    public enum lineType {
        Straight,
        Bezier,
        CatmullRom
    }
    public class Line {
        public Vector2[] lineData;
        public lineType type;
    }

    public static class Data {
        public static List<Line> lines = new List<Line>();     
        public static List<String> DebugConsole = new List<string>();
    }

    class Program
    {
        private static Sdl2Window _window;        
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;

        private static DeviceBuffer _projectionBuffer;
        private static DeviceBuffer _cameraBuffer;
        private static DeviceBuffer _rotationBuffer;
        private static DeviceBuffer _colorBuffer;
        private static ResourceSet _viewportResourceSet;

        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static ImGuiRenderer _imGuiRenderer;
    
        private static String[] scriptNames;
        private static int _selectedScript;

        private static SerialPort _serialPort;
        private static String[] serialPortNames;
        private static int _selectedSerialPort = 0;
        private static int[] baudrates = {300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 128000, 256000};
        private static int _selectedSerialBaudrate = 10;
 
        private static Vector3 _clearColor = new Vector3(0.0f, 0.0f, 0.0f);
        private static Vector3 _drawColor = new Vector3(0.3f, 0.6f, 0.4f);
        private static Vector2 _cameraPosition = new Vector2(0.0f, 0.0f);
        private static int _zoom = 13;
        private static float[] _zoomlevels = {1f,1.5f,2f,3.33f,5f,6.25f,8.33f,12.5f,16.67f,25f,33.33f,50f,66.67f,100f,150f,200f,300f,400f,500f,625f,833,1000f,1500f,2000f,3000f,4000f};

        private static float _cameraRotation = 0.0f;
        private static int _cameraType = 1;

       // UI state
        private static bool _showSettingsWindow = true;
        private static bool _showImGuiDemoWindow = false;

        private static bool _openDrawSettingsHeader=false;
        private static bool _openCameraHeader=false;
        private static bool _openSerialMonitorHeader=false;
        private static bool _openStatisticsHeader=false;
        private static bool _openDebugConsoleHeader=false;
        private static bool _openGeneratorHeader=false;

        private static FileIniDataParser _iniParser;
        private static IniData _iniData;

        private static float _ticks=0;

        private static Vector4 textcolor1 = new Vector4(0.81f, 0.88f, 0.72f, 1.00f);
        private static Vector4 textcolor2 = new Vector4(0.0f,0.0f,0.0f,1.0f);


        private static float _linewidth=3f;
        private static int _vertexCount=0;
        private static VertexPosition[] _quadVertices = new VertexPosition[0];
        private static UInt16[] _quadIndices = new UInt16[0];   
        private static bool _recreateVerticeArray = true;   

        private static bool _useRandomSeed = false;
        private static int _seed=209284240;

        private static CodeCompiler _compiler;

        static void Main(string[] args) {

            _compiler = new CodeCompiler();

            //load user settings
            _iniParser = new FileIniDataParser();
            _iniData = _iniParser.ReadFile("Configuration.ini");
            if (_iniData["Header"]["DrawSettings"]=="true")  _openDrawSettingsHeader=true;
            if (_iniData["Header"]["Camera"]=="true")        _openCameraHeader=true;
            if (_iniData["Header"]["SerialMonitor"]=="true") _openSerialMonitorHeader=true;
            if (_iniData["Header"]["Statistics"]=="true")    _openStatisticsHeader=true;
            if (_iniData["Header"]["DebugConsole"]=="true")  _openDebugConsoleHeader=true;
            if (_iniData["Header"]["Generator"]=="true")     _openGeneratorHeader=true;

            if (_iniData["Camera"]["PositionZ"]!=null) _cameraPosition = new Vector2(0f,0f);
            
            if (_iniData["DrawColor"]["R"]!=null) _drawColor.X=float.Parse(_iniData["DrawColor"]["R"]);
            if (_iniData["DrawColor"]["G"]!=null) _drawColor.Y=float.Parse(_iniData["DrawColor"]["G"]);
            if (_iniData["DrawColor"]["B"]!=null) _drawColor.Z=float.Parse(_iniData["DrawColor"]["B"]);
            if (_iniData["ClearColor"]["R"]!=null) _clearColor.X=float.Parse(_iniData["ClearColor"]["R"]);
            if (_iniData["ClearColor"]["G"]!=null) _clearColor.Y=float.Parse(_iniData["ClearColor"]["G"]);
            if (_iniData["ClearColor"]["B"]!=null) _clearColor.Z=float.Parse(_iniData["ClearColor"]["B"]);

            if (_iniData["Line"]["Width"]!=null) _linewidth=float.Parse(_iniData["Line"]["Width"]);

            //get list of serialports
            serialPortNames = SerialPort.GetPortNames();

            //get a list of script files
            scriptNames = Directory.GetFiles("scripts","*.cs");

            // SDL_WindowFlags.Fullscreen;
            // SDL_WindowFlags.Maximized;
            // SDL_WindowFlags.Minimized;
            // SDL_WindowFlags.FullScreenDesktop;
            // SDL_WindowFlags.Hidden;
            // SDL_WindowFlags.OpenGL;
            // SDL_WindowFlags.Resizable;

            //create Window            
            SDL_WindowFlags flags = SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable | SDL_WindowFlags.Shown;
            _window = new Sdl2Window("Janus v0.1.0",1550,25,1000,1385,flags,false);

            Sdl2Native.SDL_ShowCursor(0);
            
            //create Graphics Device
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(
                _window,
                new GraphicsDeviceOptions {
                    SwapchainDepthFormat = PixelFormat.R32_Float,
                    ResourceBindingModel = ResourceBindingModel.Improved,
                    PreferStandardClipSpaceYDirection = true,
                    PreferDepthRangeZeroToOne = true,
                    SyncToVerticalBlank = false,
                    Debug = false},
                GraphicsBackend.Vulkan
            );

            _window.Resized += () =>
            {
                _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            };

            //The ResourceFactory is the source for graphicsdevice related objects such as buffers, shaders, pipeline
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            ////  Setup Geometry
            ////  create vertexdata arrays, index arrays and transfer those to the Vertex and IndexDeviceBuffers (that will live on GPU)
            // VertexPositionColor[] quadVertices = {};
            Vector2[] quadVertices = {};
            UInt16[] quadIndices = {};

            _vertexBuffer = factory.CreateBuffer(new BufferDescription(0 * VertexPosition.SizeInBytes, BufferUsage.VertexBuffer));
            // _vertexBuffer = factory.CreateBuffer(new BufferDescription(0 * 16, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription(0 * sizeof(UInt16), BufferUsage.IndexBuffer));

            //fill buffers with array (after which this data is available to GPU)
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

            //describe our vertexdata
            VertexLayoutDescription vertexBufferLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("CenterPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
                // new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
            );
            /// SETUP Rendering

            //load shaders
            _shaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex,File.ReadAllBytes("Shaders/shader-vert.glsl"),"main"), 
                new ShaderDescription(ShaderStages.Fragment,File.ReadAllBytes("Shaders/shader-frag.glsl"),"main")
            );

            // we set up the viewport as 2 buffers holding our camera and projection transformation matrices.
            // To use matrices in the shader, we need to describe the buffer layout (uniform buffers for use by the vertex shader) for the pipeline and create a ResourceSet for the commandlist
            _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _cameraBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _rotationBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _colorBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            ResourceLayout viewportResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("RotationBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ColorBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment)
            ));
            _viewportResourceSet = factory.CreateResourceSet(new ResourceSetDescription(viewportResourceLayout,_projectionBuffer,_cameraBuffer, _rotationBuffer,_colorBuffer));

            //describe our graphics pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            
            //Our output in this case is the the graphics device framebuffer
            pipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;

            //The BlendState controls how the results from rendering are blended into the output textures.
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;

            //The DepthStencilState controls depth testing, writing and comparison
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);
            
            // The RasterizerState controls the rasterizer (culling,filling,clipping,etc of polygons)
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: true);
            
            //What topology do we use ? should match our indices
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            //Here we tell the pipeline what shaders to use and how our vertexbufferdata is laid out
            pipelineDescription.ShaderSet = new ShaderSetDescription(vertexLayouts: new VertexLayoutDescription[] { vertexBufferLayout },shaders: _shaders);
            
            //Here we tell the pipeline how other resources (for example our camera and projection matrices) are laid out
            pipelineDescription.ResourceLayouts = new[] { viewportResourceLayout };
            // pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();


            //create pipeline now
            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            //create the commandlist, which lets us record and execute graphics commands
            _commandList = factory.CreateCommandList();


            //create ImGuiRenderer
            _imGuiRenderer = new ImGuiRenderer(_graphicsDevice,_graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width,_window.Height,ColorSpaceHandling.Linear);          
             
            Stopwatch sw = Stopwatch.StartNew();
            double previousTime = sw.Elapsed.TotalSeconds;

            //RENDERING LOOP
            while (_window.Exists) {
                InputSnapshot snapshot = _window.PumpEvents();
                if (_window.Exists) {
                    double newTime = sw.Elapsed.TotalSeconds;
                    float deltaSeconds = (float)(newTime - previousTime);

                    _imGuiRenderer.UpdateInput(snapshot,deltaSeconds,_window.Width,_window.Height);
                    ImGui.NewFrame();
                    UpdateInput(snapshot,deltaSeconds);

                    rebuildGUI();    
                    ImGui.Render();  

                    //recreate the buffers used by gui, prepare for drawing
                    _imGuiRenderer.UpdateGeometry(_graphicsDevice);

                    //Update our geometry if needed, update vertex buffer ready for drawing
                    UpdateGeometry();

                    //We stage all drawwing commands in the commandlist
                    _commandList.Begin();
                    //set and clear framebuffer
                    _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                    _commandList.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                    _commandList.ClearDepthStencil(1f);

                    //create view and projection matrix
                    _commandList.UpdateBuffer(_projectionBuffer, 0, Matrix4x4.CreateOrthographic(_window.Width * 100f/_zoomlevels[_zoom],_window.Height * 100f/_zoomlevels[_zoom],0.1f,10000f));
                    _commandList.UpdateBuffer(_cameraBuffer, 0, Matrix4x4.CreateLookAt(new Vector3(_cameraPosition.X,_cameraPosition.Y,1000), new Vector3(_cameraPosition.X,_cameraPosition.Y,0f), Vector3.UnitY));
                    _commandList.UpdateBuffer(_rotationBuffer, 0, Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, _cameraRotation*0.01745329252f));
                    _commandList.UpdateBuffer(_colorBuffer, 0, new Vector4(_drawColor,1.0f));
                    
                    DrawGeometry();

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
            _pipeline.Dispose();
            foreach (Shader shader in _shaders) {
                shader.Dispose();
            }
            _cameraBuffer.Dispose();
            _projectionBuffer.Dispose();
            _rotationBuffer.Dispose();
            _imGuiRenderer.Dispose();        
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _graphicsDevice.Dispose();

            //DONE
        }

       private static void Generate() {
            if (File.Exists(scriptNames[_selectedScript])) {
                //run script
                if (!_useRandomSeed) {
                    _seed=new Random().Next();
                }
                if (_compiler.CompileAndRun(scriptNames[_selectedScript],_seed)==1) {
                    Console.WriteLine("Scipt executed succesfully.");
                    Data.DebugConsole.Add(("Scipt executed succesfully."));
                } else {
                    Console.WriteLine("Script execution failed..");
                    Data.DebugConsole.Add(("Scipt executed succesfully."));
                }
                _recreateVerticeArray=true;
            }
       }

        private static void UpdateGeometry() {
            if (_recreateVerticeArray) {
                _vertexCount = 0;
                List<Vector2> vertexLineData = new List<Vector2>();
                List<Vector2> vertexLineProperties = new List<Vector2>();
                
                for(int l=0;l<Data.lines.Count;l++) {
                    for (int ctr = 0; ctr < Data.lines[l].lineData.Length; ctr++) {
                        Vector2 vnorm;
                        //line start
                        if (ctr == 0) {
                            vnorm = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr+1],Data.lines[l].lineData[ctr]));
                            Vector2 vperp = new Vector2(-vnorm.Y,vnorm.X);
                            vertexLineData.Insert(_vertexCount + ctr*2  ,Data.lines[l].lineData[ctr]-vperp*_linewidth);
                            vertexLineData.Insert(_vertexCount + ctr*2+1,Data.lines[l].lineData[ctr]+vperp*_linewidth);
                        }

                        if (ctr > 0 && ctr+1 < Data.lines[l].lineData.Length) {
                            Vector2 vnorm1 = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr],Data.lines[l].lineData[ctr-1])); //incoming line
                            Vector2 vnorm2 = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr+1],Data.lines[l].lineData[ctr])); //outgoing line
                            
                            vnorm = Vector2.Normalize(new Vector2((vnorm1.X+vnorm2.X),(vnorm1.Y+vnorm2.Y)));
                            float len = _linewidth / Vector2.Dot(vnorm1,vnorm);

                            Vector2 vperp = new Vector2(-vnorm.Y,vnorm.X);

                            vertexLineData.Insert(_vertexCount + ctr*2  , Data.lines[l].lineData[ctr]-vperp*(len));                     
                            vertexLineData.Insert(_vertexCount + ctr*2+1, Data.lines[l].lineData[ctr]+vperp*(len));                     
                        }

                        //line end
                        if (ctr+1 == Data.lines[l].lineData.Length) {
                            vnorm = Vector2.Normalize(Vector2.Subtract(Data.lines[l].lineData[ctr],Data.lines[l].lineData[ctr-1]));
                            Vector2 vperp = new Vector2(-vnorm.Y,vnorm.X);
                            vertexLineData.Insert(_vertexCount + ctr*2  , Data.lines[l].lineData[ctr]-vperp*_linewidth);                     
                            vertexLineData.Insert(_vertexCount + ctr*2+1, Data.lines[l].lineData[ctr]+vperp*_linewidth);                     
                        }

                        vertexLineProperties.Insert(_vertexCount + ctr*2  ,new Vector2(0,_linewidth));
                        vertexLineProperties.Insert(_vertexCount + ctr*2+1,new Vector2(1,_linewidth));

                    }   
                    _vertexCount += Data.lines[l].lineData.Length*2;
                
                }
                _graphicsDevice.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)_vertexCount * VertexPosition.SizeInBytes, BufferUsage.VertexBuffer));

                _quadVertices = new VertexPosition[_vertexCount];
                for (int i=0; i < _vertexCount; i++) {
                    _quadVertices[i].Position = vertexLineData[i];
                    _quadVertices[i].Properties = vertexLineProperties[i];
                };

                _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _quadVertices);
                _recreateVerticeArray=false;
            }
        }

        private static void DrawGeometry() {
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _viewportResourceSet);
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

            uint vStart = 0;
            uint vLength = 0;

            for(int l=0;l<Data.lines.Count;l++) {
                vLength = (uint)Data.lines[l].lineData.Length*2;
                _commandList.Draw(vertexCount:vLength,instanceCount:1,vertexStart:vStart,instanceStart:0);
                vStart+=vLength;
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
                    Console.WriteLine("leftDoubleClicked");  
                }
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                    Console.WriteLine("leftReleased");  
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                }

                if(ImGui.IsMouseDragging(ImGuiMouseButton.Left)) {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                    // Console.WriteLine(ImGui.GetMouseDragDelta());
                    _cameraPosition = _cameraPosition - new Vector2(ImGui.GetMouseDragDelta().X*(100f/_zoomlevels[_zoom]),-ImGui.GetMouseDragDelta().Y*(100f/_zoomlevels[_zoom]));
                    ImGui.ResetMouseDragDelta();
                }

                if(ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
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
                    if (_zoom < _zoomlevels.Count()-1) {
                        _zoom+=1;
                    }
                    // Console.WriteLine("zoomin");
                }
                if (snapshot.WheelDelta < 0) {
                    // Console.WriteLine("zoomout");
                    // _cameraPosition = _cameraPosition + new Vector3(0,0,10);
                    if (_zoom > 0) {
                        _zoom-=1;
                    }
                }

                
                //check if Keyboared in use by ImGui
                if (!ImGui.GetIO().WantCaptureKeyboard) {
                    for (int i = 0; i < snapshot.KeyEvents.Count; i++)
                    {
                        KeyEvent ke = snapshot.KeyEvents[i];
                        if (ke.Down)
                        {
                            Console.Write("keydown: ");
                            Console.WriteLine(ke.Key);
                            Data.DebugConsole.Add("keydown: " + ke.Key.ToString());
                        }
                        else
                        {
                            Console.Write("keyup: ");
                            Console.WriteLine(ke.Key);
                            Data.DebugConsole.Add("keyup: " + ke.Key.ToString());

                        }
                    }
                }
            }  
        }

        private static void rebuildGUI() {
            ImGui.PushStyleColor(ImGuiCol.Header,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.BeginMenu("File")) {
                    if (ImGui.MenuItem("Quit","", false, true)){
                        _window.Close();
                    };
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Window")) {
                    ImGui.MenuItem("Settings" , string.Empty, ref _showSettingsWindow, true);
                    ImGui.MenuItem("ImGui Demo", string.Empty, ref _showImGuiDemoWindow, true);
                    ImGui.Separator();
                    ImGui.MenuItem("Draw Settings" , string.Empty, ref _openDrawSettingsHeader, true);
                    ImGui.MenuItem("Camera" , string.Empty, ref _openCameraHeader, true);
                    ImGui.MenuItem("Serial Monitor" , string.Empty, ref _openSerialMonitorHeader, true);
                    ImGui.MenuItem("Statistics" , string.Empty, ref _openStatisticsHeader, true);
                    ImGui.MenuItem("Debug Console" , string.Empty, ref _openDebugConsoleHeader, true);
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            if (_showImGuiDemoWindow)
            {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            }

            if (_showSettingsWindow) {
                ImGui.SetNextWindowPos(new Vector2(_window.Width-320f,20f));
                ImGui.SetNextWindowSize(new Vector2(320f,_window.Height-30f));
                ImGui.Begin("Settings",ref _showSettingsWindow,ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize);

                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                // ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openDrawSettingsHeader);
                if (ImGui.CollapsingHeader("Draw Settings")) {
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    // ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,0f);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openDrawSettingsHeader) {
                        _openDrawSettingsHeader=true;
                        _iniData["Header"]["DrawSettings"]="true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                    // ImGui.SetStateStorage(storage);
                    ImGui.Spacing();
                    // ImGui.Dummy(new Vector2(0f,2f));

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Clear color:");
                    ImGui.SameLine();
                    if (ImGui.ColorEdit3("##clearcol", ref _clearColor)) {
                        _iniData["ClearColor"]["R"]=_clearColor.X.ToString();
                        _iniData["ClearColor"]["G"]=_clearColor.Y.ToString();
                        _iniData["ClearColor"]["B"]=_clearColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);        
                    }        

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Draw color: ");
                    ImGui.SameLine();
                    if (ImGui.ColorEdit3("##drawcol", ref _drawColor)) {
                        _iniData["DrawColor"]["R"]=_drawColor.X.ToString();
                        _iniData["DrawColor"]["G"]=_drawColor.Y.ToString();
                        _iniData["DrawColor"]["B"]=_drawColor.Z.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }        

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Line width:");
                    ImGui.SameLine();
                    if(ImGui.DragFloat("##line", ref _linewidth,0.1f,0.1f,20f)) {
                        _iniData["Line"]["Width"]=_linewidth.ToString();
                        _iniParser.WriteFile("Configuration.ini", _iniData);   
                        _recreateVerticeArray=true;                           
                    };   


                    // ImGui.Spacing();
                    ImGui.Dummy(new Vector2(0,12f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                    // ImGui.PopStyleVar();
                } else {
                  if (_openDrawSettingsHeader) {
                        _openDrawSettingsHeader=false;
                        _iniData["Header"]["DrawSettings"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                // ImGui.PopStyleVar();                    

                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                // ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openCameraHeader);
                if (ImGui.CollapsingHeader("Camera")) {
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    // ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,0f);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openCameraHeader) {
                        _openCameraHeader=true;
                        _iniData["Header"]["Camera"]="true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                    // ImGui.Spacing();
                    // ImGui.RadioButton("Perspective", ref _cameraType, 0);
                    // ImGui.SameLine();
                    // ImGui.RadioButton("Orthographic", ref _cameraType, 1);
                    ImGui.Spacing();
                    // ImGui.TextColored(new Vector4(1f,0f,0f,1f),"hello");
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Position:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(14,0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200.0f);
                    if(ImGui.DragFloat2("##_camPos", ref _cameraPosition)) {
                        // Console.WriteLine("position changed");
                    };   

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Rotation:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(13f,0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200.0f);
                    ImGui.DragFloat("##_camRot", ref _cameraRotation);

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Zoom:");
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(30,0));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200.0f);
                    ImGui.SliderInt("##_camZoom2", ref _zoom,0,_zoomlevels.Count()-1,_zoomlevels[_zoom].ToString() + "%%"); 

                    // ImGui.SliderInt("camera type",ref _cameraType,0,1,((CameraType)_cameraType).ToString());
                    /*
                    ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  

                    ImGui.Checkbox("ImGui Demo Window", ref _showImGuiDemoWindow);                
                    if (ImGui.Button("Button"))                                     
                        _counter++;
                    ImGui.SameLine(0, -1);
                    ImGui.Text($"counter = {_counter}");

                    ImGui.DragInt("Draggable Int", ref _dragInt);
                    */
                    ImGui.Dummy(new Vector2(0,12f));
                    // ImGui.Spacing();
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                    // ImGui.PopStyleVar();
                } else {
                  if (_openCameraHeader) {
                        _openCameraHeader=false;
                        _iniData["Header"]["Camera"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                //// Generator
                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openGeneratorHeader);
                if (ImGui.CollapsingHeader("Generator")) {   
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);

                    if (!_openGeneratorHeader) {
                        _openGeneratorHeader=true;
                        _iniData["Header"]["Generator"]="true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }

                    ImGui.Spacing();

                   if (scriptNames.Count() > 0) {
                        ImGui.PushStyleColor(ImGuiCol.Header,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushItemWidth(228);
                        if (ImGui.BeginCombo("##comboscriptfiles",scriptNames[_selectedScript].Split("scripts/")[1])) {
                            for (int n = 0; n < scriptNames.Count(); n++)
                            {
                                bool is_selected = (scriptNames[_selectedScript] == scriptNames[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable( scriptNames[n].Split("scripts/")[1], is_selected)) 
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
                        ImGui.Dummy(new Vector2(109f,0f));                        
                    }
                    ImGui.SameLine();
                    
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);
                    if (ImGui.Button("Refresh##scriptfiles",new Vector2(70f,20f))) {
                        scriptNames = Directory.GetFiles("scripts","*.cs");
                    }
                    ImGui.PopStyleVar();

                    ImGui.DragInt("##RandomSeed", ref _seed);
                    ImGui.SameLine();
                    ImGui.Checkbox("Use Seed", ref _useRandomSeed);

                    ImGui.Dummy(new Vector2(0f,10f));

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);
                    if (ImGui.Button(" Compile and Generate! ",new Vector2(180,30))) {
                        Generate();
                    }
                    ImGui.PopStyleVar();


                    ImGui.Dummy(new Vector2(0,15f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                } else {
                  if (_openGeneratorHeader) {
                        _openGeneratorHeader=false;
                        _iniData["Header"]["Generator"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                //// Serial Monitor
                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openSerialMonitorHeader);
                if (ImGui.CollapsingHeader("Serial Monitor")) {   
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openSerialMonitorHeader) {
                        _openSerialMonitorHeader=true;
                        _iniData["Header"]["SerialMonitor"]="true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }

                    ImGui.Spacing();

                    if (serialPortNames.Count() > 0) {
                        ImGui.PushStyleColor(ImGuiCol.Header,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushItemWidth(228f);
                        if (ImGui.BeginCombo("##comboserialports",serialPortNames[_selectedSerialPort])) {
                            for (int n = 0; n < serialPortNames.Count(); n++)
                            {
                                bool is_selected = (serialPortNames[_selectedSerialPort] == serialPortNames[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(serialPortNames[n], is_selected)) 
                                    _selectedSerialPort = n;
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
                        ImGui.Text("No serialports found.");
                        ImGui.SameLine();                 
                        ImGui.Dummy(new Vector2(85f,0f));
                    }
                    ImGui.SameLine();                 
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);
                    if (ImGui.Button("Refresh##SerialPorts",new Vector2(70f,20f))) {
                        serialPortNames = SerialPort.GetPortNames();
                    }
                    ImGui.PopStyleVar();

                    if (serialPortNames.Count() > 0) {
                        ImGui.PushStyleColor(ImGuiCol.Header,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive,new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
                        ImGui.PushItemWidth(228f);
                        if (ImGui.BeginCombo("##combobaudrate",baudrates[_selectedSerialBaudrate].ToString())) {
                            for (int n = 0; n < baudrates.Count(); n++)
                            {
                                bool is_selected = (baudrates[_selectedSerialBaudrate] == baudrates[n]); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(baudrates[n].ToString(), is_selected)) 
                                    _selectedSerialBaudrate = n;
                                if (is_selected) {
                                    ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                                } 
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();
                        ImGui.PopStyleColor();

                        ImGui.SameLine();
                        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5f);                              
                        if (ImGui.Button("Connect",new Vector2(70f,20f))) {
                            Console.WriteLine($"Connecting to: {serialPortNames[_selectedSerialPort].ToString()} at {baudrates[_selectedSerialBaudrate].ToString()}");
                        }
                        ImGui.PopStyleVar();

                    } else {
                        ImGui.Dummy(new Vector2(0f,18f));
                    }
                    ImGui.Spacing();
                    ImGui.SetNextWindowSizeConstraints(new Vector2(100f,100f),new Vector2(1500f,200f));
                    ImGui.BeginChild("SerialMonitorChild",new Vector2(300f,240f),true,ImGuiWindowFlags.AlwaysVerticalScrollbar);
                    ImGui.EndChild();

                    ImGui.Dummy(new Vector2(0,12f));

                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                } else {
                    if (_openSerialMonitorHeader) {
                        _openSerialMonitorHeader=false;
                        _iniData["Header"]["SerialMonitor"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                //// Statistics 
                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openStatisticsHeader);
                if (ImGui.CollapsingHeader("Stats")) {
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);
                    if (!_openStatisticsHeader) {
                        _openStatisticsHeader=true;
                        _iniData["Header"]["Statistics"]="true";
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

                    ImGui.Dummy(new Vector2(0,12f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();

                } else {
                  if (_openStatisticsHeader) {
                        _openStatisticsHeader=false;
                        _iniData["Header"]["Statistics"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();

                ImGui.PushStyleColor(ImGuiCol.Text,textcolor2);
                ImGui.PushFont(_imGuiRenderer.fontBold);
                ImGui.SetNextItemOpen(_openDebugConsoleHeader);
                if (ImGui.CollapsingHeader("Debug Console")) {   
                    ImGui.PushStyleColor(ImGuiCol.Text,textcolor1);
                    ImGui.PushFont(_imGuiRenderer.fontRegular);

                    if (!_openDebugConsoleHeader) {
                        _openDebugConsoleHeader=true;
                        _iniData["Header"]["DebugConsole"]="true";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }

                    ImGui.Spacing();
                    ImGui.SetNextWindowSizeConstraints(new Vector2(100f,100f),new Vector2(1500f,200f));
                    ImGui.BeginChild("DebugConsoleChild",new Vector2(300f,240f),true,ImGuiWindowFlags.AlwaysVerticalScrollbar);
                        foreach(String msg in Data.DebugConsole) {
                            ImGui.Text(msg);
                        }
                    ImGui.EndChild();

                    ImGui.Dummy(new Vector2(0,15f));
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                } else {
                  if (_openDebugConsoleHeader) {
                        _openDebugConsoleHeader=false;
                        _iniData["Header"]["DebugConsole"]="false";
                        _iniParser.WriteFile("Configuration.ini", _iniData);                  
                    }
                }
                ImGui.PopStyleColor();
                ImGui.PopFont();
                ImGui.End();
            }

        }
    }

    struct VertexPosition {
        public const uint SizeInBytes = 16;
        public Vector2 Position;
        public Vector2 Properties;
        public VertexPosition(Vector2 position, Vector2 properties) {
            Position = position;
            Properties = properties;
        }
    }

    enum CameraType {
        Perspective,
        Orthografic
    }
    
    public interface IGenerator {
        void Generate(int seed);
    }
}