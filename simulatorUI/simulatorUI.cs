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

        private static simulator.eightChipsSimulator simulatorInstance;
        private static IntPtr CPUframeBufferTextureId;
        static Dictionary<IntPtr, Texture> textureMap = new Dictionary<IntPtr, Texture>();

        private static readonly object balanceLock = new object();

        private static Task simulationThread;
        private static uint width = 256;
        private static uint height = 256;
        private static int cpuSpeed = 100000;
        private static bool halted = false;

        private static string[] expandedCode;
        private static int currentProgramLine;

        static string testVGAOutputProgram =
@"increment = 1
width = 256
height = 256
page = 0

(START)
LOADAIMMEDIATE
1100
(ADD_1)
ADD
increment
OUTA
STOREA
pixelindex

(COLORWHITE)
LOADA
pixelindex
LOADPAGE
page
STOREAATPOINTER
pixelindex
LOADPAGEIMMEDIATE
0

(DONECHECK)
LOADA
pixelindex
LOADBIMMEDIATE
32000
UPDATEFLAGS
JUMPIFLESS
ADD_1
//if page is 0 - set it to 1
//if page is 1 - set it to 0
LOADA 
page
LOADBIMMEDIATE
0
UPDATEFLAGS
JUMPIFEQUAL
SET_PAGE_TO_1

LOADAIMMEDIATE
0
STOREA
page

JUMP
START

(SET_PAGE_TO_1)
LOADAIMMEDIATE
1
STOREA
page

JUMP
START";

        static void Main(string[] args)
        {

            //AppDomain.CurrentDomain.ProcessExit+= (s,e)=>{}

            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, testVGAOutputProgram);
            var assemblerInst = new assembler.Assembler(path);
            var assembledResult = assemblerInst.ConvertToBinary();
            var binaryProgram = assembledResult.Select(x => Convert.ToInt16(x, 16));

            //lets convert our final assembled program back to assembly instructions so we can view it.
            //dissasembly)
            var assembler2 = new assembler.Assembler(path);
            expandedCode = assembler2.ExpandMacros().ToArray();


            simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());

            //TODO use cancellation token here.
            simulationThread = Task.Run(() =>
              {
                  simulatorInstance.ProgramCounter = (short)assembler.Assembler.MemoryMap[assembler.Assembler.MemoryMapKeys.user_code].StartOnPage;
                  simulatorInstance.runSimulation();
              });


            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "8 chips simulator 2"),
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

            var data = simulatorInstance.mainMemory.Select(x => convertShortFormatToFullColor(Convert.ToInt32(x).ToBinary())).ToArray();

            //try creating an texture and binding it to an image which imgui will draw...
            //we'll need to modify this image every frame potentially...

            var texture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            CPUframeBufferTextureId = controller.GetOrCreateImGuiBinding(gd.ResourceFactory, texture);
            gd.UpdateTexture(texture, data, 0, 0, 0, width, height, 1, 0, 0);
            textureMap.Add(CPUframeBufferTextureId, texture);

            /* 
                        var state = new object();
                        var timer = new System.Threading.Timer((o) =>
                        {
                            int[] newdata = simulatorInstance.mainMemory.Select(x => convertShortFormatToFullColor(x)).ToArray();
                            var currenttexture = textureMap[CPUframeBufferTextureId];
                            gd.UpdateTexture(currenttexture, newdata, 0, 0, 0, 256, 256, 1, 0, 0);
                        }, state, 1000, 150);

            */
            // Main application loop
            while (mainWindow.Exists)
            {

                InputSnapshot snapshot = mainWindow.PumpEvents();
                if (!mainWindow.Exists) { break; }
                controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                SubmitUI();

                commandList.Begin();
                commandList.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                commandList.ClearColorTarget(0, new RgbaFloat(.1f, .1f, .1f, .2f));
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
            ImGui.Begin("8Chips Simulator");
            {
                ImGui.StyleColorsDark();
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(.2f, 1f, .4f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.5f, 1f, .7f, 1f));
                ImGui.SetWindowFontScale(1.5f);
                ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)

                ImGui.SameLine(0, -1);

                float framerate = ImGui.GetIO().Framerate;
                ImGui.NewLine();
                ImGui.Text($"Total CPU Instruction Count {simulatorInstance.TotalInstructionCount}");


                ImGui.Begin("Registers: ");
                ImGui.SetWindowFontScale(1.5f);

                simulatorInstance.Registers.ToList().ForEach(x =>
                {
                    ImGui.Text($"{x.Key} : {x.Value}");
                });


                ImGui.End();

                if (ImGui.Checkbox("Halt CPU", ref halted))
                {
                    simulatorInstance.HALT = halted;
                    if (!halted)
                    {
                        Task.Run(() =>
                        {
                            simulatorInstance.runSimulation();
                        });
                    }
                }
                if (ImGui.SliderInt("CPU speed", ref cpuSpeed, 1, 100000))
                {
                    simulatorInstance.instructionBundleSize = cpuSpeed;
                }

                ImGui.Begin("Assembly");
                ImGui.Text("Expanded Assembly");
                ImGui.ListBox("", ref currentProgramLine, expandedCode, expandedCode.Length, expandedCode.Length);
                //TODO I think this is going to be offset incorrectly based on how many labels were removed during expansion...
                currentProgramLine = simulatorInstance.ProgramCounter - assembler.Assembler.MemoryMap[assembler.Assembler.MemoryMapKeys.user_code].AbsoluteStart;

                int[] data = simulatorInstance.mainMemory.Select(x => convertShortFormatToFullColor(Convert.ToInt32(x).ToBinary())).ToArray();
                var texture = textureMap[CPUframeBufferTextureId];
                gd.UpdateTexture(texture, data, 0, 0, 0, width, height, 1, 0, 0);



                ImGui.Begin("FrameBuffer_Window");
                ImGui.Text("FrameBuffer");
                ImGui.Image(CPUframeBufferTextureId, new Vector2(width * 2, height * 2));
                ImGui.End();
            }


            ImGuiIOPtr io = ImGui.GetIO();

        }

    }
}
