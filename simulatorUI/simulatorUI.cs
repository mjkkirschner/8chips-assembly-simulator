using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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

        private static simulator.simulator simulatorInstance;
        private static IntPtr CPUframeBufferTextureId;
        static Dictionary<IntPtr, Texture> textureMap = new Dictionary<IntPtr, Texture>();

        private static readonly object balanceLock = new object();

        private static Task simulationThread;

        static string testVGAOutputProgram =
@"increment = 1
width = 256
height = 256
(START)
LOADAIMMEDIATE
1100
(ADD_1)
ADD
increment
OUTA
STOREA
pixelindex
LOADAATPOINTER
pixelindex
LOADBIMMEDIATE
0
UPDATEFLAGS
JUMPIFEQUAL
COLORWHITE

(COLORBLACK)
LOADAIMMEDIATE
0
STOREAATPOINTER
pixelindex
JUMP
DONECHECK

(COLORWHITE)
LOADA
pixelindex
MODULO
width
STOREA
x
LOADA
pixelindex
DIVIDE
width
STOREA
y
LOADA
x
MULTIPLY
x
DIVIDE
y
STOREAATPOINTER
pixelindex

//a comment

(DONECHECK)
LOADA
pixelindex
LOADBIMMEDIATE
65000
UPDATEFLAGS
JUMPIFLESS
ADD_1
JUMP
START";



        static void Main(string[] args)
        {

            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, testVGAOutputProgram);
            var assembler = new assembler.Assembler(path);
            var assembledResult = assembler.ConvertToBinary();
            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16).ToBinary());

            simulatorInstance = new simulator.simulator(16, 256 * 256);

            //TODO we probably need to insert this like it was being loaded by the boot loader...
            simulatorInstance.mainMemory.InsertRange(255, binaryProgram.ToList());

            //TODO use cancellation token here.
            simulationThread = Task.Run(() =>
              {
                  simulatorInstance.ProgramCounter = 255.ToBinary();
                  simulatorInstance.runSimulation();
              });


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

            var data = simulatorInstance.mainMemory.Select(x => convertShortFormatToFullColor(x)).ToArray();
            //try creating an texture and binding it to an image which imgui will draw...
            //we'll need to modify this image every frame potentially...

            //we need a conversion function here that converts from our format to a standard pixel format...

            var texture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(256, 256, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            CPUframeBufferTextureId = controller.GetOrCreateImGuiBinding(gd.ResourceFactory, texture);
            gd.UpdateTexture(texture, data, 0, 0, 0, 256, 256, 1, 0, 0);
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

        private static int convertShortFormatToFullColor(BitArray memoryCell)
        {
            //bit array reverse the bit order of bytes

            var color = new bool[]{

                //lsb - RED
              false, false, false, false, memoryCell[8], memoryCell[9], memoryCell[10], memoryCell[11],
              //GREEN
            false, false, false, false,memoryCell[4], memoryCell[5], memoryCell[6], memoryCell[7],
            //BLUE
             false, false, false, false, memoryCell[0], memoryCell[1], memoryCell[2], memoryCell[3],

             //ALPHA
             true, true, true, true, true, true, true, true};


            var remappedColor = new BitArray(color);

            return remappedColor.ToNumeral();
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


                int[] data = simulatorInstance.mainMemory.Select(x => convertShortFormatToFullColor(x)).ToArray();
                var texture = textureMap[CPUframeBufferTextureId];
                gd.UpdateTexture(texture, data, 0, 0, 0, 256, 256, 1, 0, 0);




                ImGui.Image(CPUframeBufferTextureId, new Vector2(256));
            }


            ImGuiIOPtr io = ImGui.GetIO();

        }

    }
}
