using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.StartupUtilities;

namespace simulatorUI
{
    public class Program
    {
        private static Veldrid.Sdl2.Sdl2Window mainWindow;
        private static Veldrid.GraphicsDevice gd;
        private static Veldrid.CommandList commandList;
        private static Veldrid.ImGuiRenderer controller;

        private static IntPtr CPUframeBufferTextureId;
        static Dictionary<IntPtr, Texture> textureMap = new Dictionary<IntPtr, Texture>();


        static void Main(string[] args)
        {
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ImGui.NET Sample Program"),
                new GraphicsDeviceOptions(true, null, true),
                out mainWindow,
                out gd);

            mainWindow.Resized += () =>
            {
                gd.MainSwapchain.Resize((uint)mainWindow.Width, (uint)mainWindow.Height);
                controller.WindowResized(mainWindow.Width, mainWindow.Height);
            };
            commandList = gd.ResourceFactory.CreateCommandList();
            controller = new Veldrid.ImGuiRenderer(gd, gd.MainSwapchain.Framebuffer.OutputDescription, mainWindow.Width, mainWindow.Height);

            var random = new Random();
            float[] data = Enumerable.Range(0, 512 * 512).Select((i, index) => { return (float)random.NextDouble(); }).ToArray();
            //try creating an texture and binding it to an image which imgui will draw...
            //we'll need to modify this image every frame potentially...
            var texture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(512, 512, 1, 1, PixelFormat.R32_Float, TextureUsage.Sampled));
            CPUframeBufferTextureId = controller.GetOrCreateImGuiBinding(gd.ResourceFactory, texture);
            gd.UpdateTexture(texture, data, 0, 0, 0, 512, 512, 1, 0, 0);
            textureMap.Add(CPUframeBufferTextureId, texture);

            // Main application loop
            while (mainWindow.Exists)
            {
                InputSnapshot snapshot = mainWindow.PumpEvents();
                if (!mainWindow.Exists) { break; }
                controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                SubmitUI();

                commandList.Begin();
                commandList.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                commandList.ClearColorTarget(0, new RgbaFloat(.7f, .7f, 1f, 1f));
                controller.Render(gd, commandList);
                commandList.End();
                gd.SubmitCommands(commandList);
                gd.SwapBuffers(gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            gd.WaitForIdle();
            controller.Dispose();
            commandList.Dispose();
            gd.Dispose();
        }
        private static void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.BeginWindow()/ImGui.EndWindow() the widgets automatically appears in a window called "Debug".
            {
                ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)

                ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

                ImGui.SameLine(0, -1);

                float framerate = ImGui.GetIO().Framerate;
                ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
                var random = new Random();

                float[] data = Enumerable.Range(0, 512 * 512).Select((i, index) => { return (float)random.NextDouble(); }).ToArray();
                var texture = textureMap[CPUframeBufferTextureId];
                gd.UpdateTexture(texture as Texture, data, 0, 0, 0, 512, 512, 1, 0, 0);


                ImGui.Image(CPUframeBufferTextureId, new Vector2(1024));
            }


            ImGuiIOPtr io = ImGui.GetIO();

        }

    }
}
