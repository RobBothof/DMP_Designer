using System.Numerics;
using ImGuiNET;
using Veldrid.SPIRV;
using System.Runtime.CompilerServices;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using Veldrid;

namespace Veldrid
{
    /// Identifies the kind of color space handling that an <see cref="ImGuiRenderer"/> uses.
    public enum ColorSpaceHandling
    {
        /// Legacy-style color space handling. In this mode, the renderer will not convert sRGB vertex colors into linear space before blending them.
        Legacy = 0,
        /// Improved color space handling. In this mode, the render will convert sRGB vertex colors into linear space before blending them with colors from user Textures.
        Linear = 1,
    }

    /// <summary>
    /// Can render draw lists produced by ImGui.
    /// Also provides functions for updating ImGui input.
    /// </summary>
    public class ImGuiRenderer : IDisposable
    {
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _projMatrixBuffer;
        private Texture _fontTexture;
        private static Shader[] _shaders;        
        private ResourceLayout _layout;
        private ResourceLayout _textureLayout;
        private Pipeline _pipeline;
        private ResourceSet _mainResourceSet;
        private ResourceSet _fontTextureResourceSet;

        public ImFontPtr fontRegular;
        public ImFontPtr fontBold;

        /// <summary>
        /// Constructs a new ImGuiRenderer.
        /// </summary>
        /// <param name="gd">The GraphicsDevice used to create and update resources.</param>
        /// <param name="outputDescription">The output format.</param>
        /// <param name="width">The initial width of the rendering target. Can be resized.</param>
        /// <param name="height">The initial height of the rendering target. Can be resized.</param>
        /// <param name="colorSpaceHandling">Identifies how the renderer should treat vertex colors.</param>

        public ImGuiRenderer(GraphicsDevice gd, OutputDescription outputDescription, int windowWidth, int windowHeight, ColorSpaceHandling colorSpaceHandling) {
            ImGui.SetCurrentContext(ImGui.CreateContext());
            ImGuiIOPtr io = ImGui.GetIO();

            //Set KeyMappingsa
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;

            io.DisplaySize = new Vector2(windowWidth,windowHeight);
            io.DisplayFramebufferScale = Vector2.One;
            io.DeltaTime = 1f / 60f; 


            ResourceFactory factory = gd.ResourceFactory;

            _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = "ImGui.NET Index Buffer";

            _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            //create shaders
            _shaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex,File.ReadAllBytes("Shaders/imgui-vert.glsl"),"main"), 
                new ShaderDescription(ShaderStages.Fragment,File.ReadAllBytes("Shaders/imgui-frag.glsl"),"main")
            );

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[] { 
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm)
                )
            };

            _layout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );
            
            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment))
            );

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts,_shaders, new[] {
                        new SpecializationConstant(0, gd.IsClipSpaceYInverted),
                        new SpecializationConstant(1, colorSpaceHandling == ColorSpaceHandling.Legacy)}),
                new ResourceLayout[] { _layout, _textureLayout },
                outputDescription,
                ResourceBindingModel.Default
            );
                
            _pipeline = factory.CreateGraphicsPipeline(ref pd);
            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,_projMatrixBuffer,gd.PointSampler));

            //add custom fonts
            unsafe {
                var config = ImGuiNative.ImFontConfig_ImFontConfig();
                
                (*config).OversampleH = 4;
                (*config).OversampleV = 4;
                (*config).RasterizerMultiply = 1f;
                (*config).GlyphExtraSpacing = new Vector2(1f,0f);
                fontRegular = io.Fonts.AddFontFromFileTTF("Font/Roboto-Regular.ttf",18f,config);
                fontBold = io.Fonts.AddFontFromFileTTF("Font/Roboto-Bold.ttf",18f,config);
                
                ImGuiNative.ImFontConfig_destroy(config);
                
                io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int texwidth, out int texheight, out int bytesPerPixel);

                _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)texwidth,(uint)texheight,1,1,PixelFormat.R8_G8_B8_A8_UNorm,TextureUsage.Sampled));
                _fontTexture.Name = "ImGui.NET Font Texture";
                gd.UpdateTexture(_fontTexture,(IntPtr)pixels,(uint)(bytesPerPixel * texwidth * texheight),0,0,0,(uint)texwidth,(uint)texheight,1,0,0);
                _fontTextureResourceSet = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTexture));

                io.Fonts.ClearTexData();
            }       

            //we save our own ini file
            io.IniSavingRate=0f;
            //create style

            io.ConfigDragClickToInputText = true;

            ImGuiStylePtr style = ImGui.GetStyle();
            style.ScrollbarRounding=0;
            style.ItemSpacing=new Vector2(3,3);
            style.ItemInnerSpacing=new Vector2(3,3);
            style.GrabMinSize=15;
            style.FrameBorderSize=1f;
            style.WindowBorderSize=1f;
            style.WindowPadding=new Vector2(8f,4f);
            style.FramePadding=new Vector2(3f,3f);
            style.ColorButtonPosition=ImGuiDir.Left;
            style.WindowTitleAlign=new Vector2(0.02f,0.5f);
            style.SelectableTextAlign=new Vector2(0f,0f);

            style.Colors[(int)ImGuiCol.Text]                   = new Vector4(0.81f, 0.88f, 0.72f, 1.00f);
            style.Colors[(int)ImGuiCol.TextDisabled]           = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
            style.Colors[(int)ImGuiCol.WindowBg]               = new Vector4(0.48f, 0.52f, 0.47f, 0.49f);
            style.Colors[(int)ImGuiCol.WindowBg]               = new Vector4(0.25f, 0.25f, 0.24f, 1.00f);
            style.Colors[(int)ImGuiCol.ChildBg]                = new Vector4(0.00f, 0.00f, 0.00f, 0.23f);
            style.Colors[(int)ImGuiCol.PopupBg]                = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
            style.Colors[(int)ImGuiCol.Border]                 = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.BorderShadow]           = new Vector4(0.00f, 0.00f, 0.00f, 0.50f);
            style.Colors[(int)ImGuiCol.FrameBg]                = new Vector4(0.13f, 0.13f, 0.08f, 0.84f);
            style.Colors[(int)ImGuiCol.FrameBgHovered]         = new Vector4(0.18f, 0.22f, 0.18f, 0.66f);
            style.Colors[(int)ImGuiCol.FrameBgActive]          = new Vector4(0.18f, 0.22f, 0.18f, 0.66f);
            style.Colors[(int)ImGuiCol.TitleBg]                = new Vector4(0.06f, 0.06f, 0.06f, 0.80f);
            style.Colors[(int)ImGuiCol.TitleBgActive]          = new Vector4(0.06f, 0.06f, 0.06f, 0.80f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed]       = new Vector4(0.06f, 0.06f, 0.06f, 0.80f);
            style.Colors[(int)ImGuiCol.MenuBarBg]              = new Vector4(0.09f, 0.10f, 0.10f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg]            = new Vector4(0.02f, 0.02f, 0.02f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab]          = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered]   = new Vector4(0.41f, 0.41f, 0.41f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive]    = new Vector4(0.51f, 0.51f, 0.51f, 1.00f);
            style.Colors[(int)ImGuiCol.CheckMark]              = new Vector4(0.93f, 0.62f, 0.10f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrab]             = new Vector4(0.71f, 0.55f, 0.19f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrabActive]       = new Vector4(0.93f, 0.54f, 0.06f, 1.00f);
            style.Colors[(int)ImGuiCol.Button]                 = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonHovered]          = new Vector4(0.78f, 0.51f, 0.13f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonActive]           = new Vector4(0.43f, 0.29f, 0.03f, 1.00f);
            style.Colors[(int)ImGuiCol.Header]                 = new Vector4(0.69f, 0.69f, 0.69f, 1.00f);
            style.Colors[(int)ImGuiCol.HeaderHovered]          = new Vector4(0.80f, 0.80f, 0.80f, 1.00f);
            style.Colors[(int)ImGuiCol.HeaderActive]           = new Vector4(0.54f, 0.54f, 0.54f, 1.00f);
            style.Colors[(int)ImGuiCol.Separator]              = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
            style.Colors[(int)ImGuiCol.SeparatorHovered]       = new Vector4(0.29f, 0.40f, 0.40f, 0.78f);
            style.Colors[(int)ImGuiCol.SeparatorActive]        = new Vector4(0.27f, 0.33f, 0.32f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGrip]             = new Vector4(0.49f, 0.49f, 0.49f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered]      = new Vector4(0.63f, 0.63f, 0.63f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGripActive]       = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            style.Colors[(int)ImGuiCol.Tab]                    = new Vector4(0.23f, 0.23f, 0.23f, 0.86f);
            style.Colors[(int)ImGuiCol.TabHovered]             = new Vector4(0.46f, 0.46f, 0.46f, 0.80f);
            style.Colors[(int)ImGuiCol.TabActive]              = new Vector4(0.32f, 0.31f, 0.31f, 1.00f);
            style.Colors[(int)ImGuiCol.TabUnfocused]           = new Vector4(0.13f, 0.13f, 0.13f, 0.97f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive]     = new Vector4(0.34f, 0.34f, 0.34f, 1.00f);
            style.Colors[(int)ImGuiCol.DockingPreview]         = new Vector4(0.73f, 0.33f, 0.33f, 0.70f);
            style.Colors[(int)ImGuiCol.DockingEmptyBg]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLines]              = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered]       = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogram]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered]   = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TableHeaderBg]          = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderStrong]      = new Vector4(0.31f, 0.31f, 0.35f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderLight]       = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);
            style.Colors[(int)ImGuiCol.TableRowBg]             = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt]          = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
            style.Colors[(int)ImGuiCol.TextSelectedBg]         = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            style.Colors[(int)ImGuiCol.DragDropTarget]         = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
            style.Colors[(int)ImGuiCol.NavHighlight]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight]  = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg]      = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg]       = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);

            style.MouseCursorScale=1.0f;
            io.MouseDrawCursor=true;

        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void UpdateInput(InputSnapshot snapshot,float deltaSeconds, int windowWidth, int windowHeight)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(windowWidth,windowHeight);
            io.DeltaTime = deltaSeconds; 

            //MOUSE INPUT
            io.MousePos   = snapshot.MousePosition;
            io.MouseWheel = snapshot.WheelDelta;
            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            io.MouseDown[0] = snapshot.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = snapshot.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = snapshot.IsMouseDown(MouseButton.Middle);            
            for (int i = 0; i < snapshot.MouseEvents.Count; i++) {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down) {
                    if (me.MouseButton == MouseButton.Left)   io.MouseDown[0] = true;
                    if (me.MouseButton == MouseButton.Middle) io.MouseDown[1] = true;
                    if (me.MouseButton == MouseButton.Right)  io.MouseDown[2] = true;
                }
            }

            //KEYBOARD INPUT
            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++) {
                char c = keyCharPresses[i];
                ImGui.GetIO().AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++) {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if ((keyEvent.Key == Key.ControlLeft) || (keyEvent.Key == Key.ControlRight)) io.KeyCtrl  = keyEvent.Down;
                if ((keyEvent.Key == Key.ShiftLeft)   || (keyEvent.Key == Key.ShiftRight))   io.KeyShift = keyEvent.Down;
                if ((keyEvent.Key == Key.AltLeft)     || (keyEvent.Key == Key.AltRight))     io.KeyAlt   = keyEvent.Down;               
                if (keyEvent.Key == Key.WinLeft)                                             io.KeySuper = keyEvent.Down;               
            }


        }

        /// <summary>
        /// Prepares the ImGui draw list data.
        /// </summary>
        public void UpdateGeometry(GraphicsDevice gd) {
            ImDrawDataPtr draw_data = ImGui.GetDrawData();

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            if (draw_data.CmdListsCount == 0) {
                return;
            }

            uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBuffer.SizeInBytes) {
                gd.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes) {
                gd.DisposeWhenIdle(_indexBuffer);
                _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++) {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];
                gd.UpdateBuffer(_vertexBuffer,vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),cmd_list.VtxBuffer.Data,(uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));
                gd.UpdateBuffer(_indexBuffer,indexOffsetInElements * sizeof(ushort),cmd_list.IdxBuffer.Data,(uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));
                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(0f,io.DisplaySize.X,io.DisplaySize.Y,0.0f,-1.0f,1.0f);
            gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);
            draw_data.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        }

        public void Draw(CommandList cl) {
            ImDrawDataPtr draw_data = ImGui.GetDrawData();

             // Render command lists
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++) {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++) {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero) {
                        throw new NotImplementedException();
                    }
                    else {
                        cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                        cl.SetScissorRect(0,(uint)pcmd.ClipRect.X,(uint)pcmd.ClipRect.Y,(uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),(uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
                        cl.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);
                    }
                    idx_offset += (int)pcmd.ElemCount;
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
            }
        }



        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _projMatrixBuffer.Dispose();
            _fontTexture.Dispose();

            foreach (Shader shader in _shaders) {
                shader.Dispose();
            }
             _layout.Dispose();
            _textureLayout.Dispose();
            _pipeline.Dispose();
            _mainResourceSet.Dispose();
            _fontTextureResourceSet.Dispose();
        }
    }
}
