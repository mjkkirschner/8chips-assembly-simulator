using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using System.Collections;

namespace Tests
{
    public class SimulatorTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void canHalt()
        {
            var simulator = new simulator.eightChipsSimulator(16, 1000);
            simulator.mainMemory[0] = 16;
            simulator.mainMemory[1] = 6;
            simulator.mainMemory[2] = 21;

            simulator.runSimulation();
            Assert.AreEqual(0, simulator.ARegister);

        }

        [Test]
        public void canAdd()
        {
            var simulator = new simulator.eightChipsSimulator(16, 1000);
            simulator.mainMemory[0] = 6;
            simulator.mainMemory[1] = 20;
            simulator.mainMemory[2] = 3;
            simulator.mainMemory[3] = 100;
            simulator.mainMemory[4] = 2;
            simulator.mainMemory[5] = 16;

            simulator.mainMemory[6] = 6;
            simulator.mainMemory[7] = 20;

            simulator.mainMemory[100] = 5;

            simulator.runSimulation();
            Assert.AreEqual(25, simulator.ARegister);
            Assert.AreEqual(25, simulator.OutRegister);

        }
        

        [Test]
        public void canJump()
        {
            var simulator = new simulator.eightChipsSimulator(16, 1000);
            simulator.mainMemory[0] = 6;
            simulator.mainMemory[1] = 20;
            simulator.mainMemory[2] = 15;
            simulator.mainMemory[3] = 7;
            simulator.mainMemory[4] = 6;

            simulator.mainMemory[5] = 2;
            simulator.mainMemory[6] = 16;


            simulator.runSimulation();
            Assert.AreEqual(0, simulator.OutRegister);
            Assert.AreEqual(20, simulator.ARegister);

        }

        [Test]
        public void canJumpConditionally()
        {
            var simulator = new simulator.eightChipsSimulator(16, 1000);
            simulator.mainMemory[0] = 6;
            simulator.mainMemory[1] = 20;
            simulator.mainMemory[2] = 3;
            simulator.mainMemory[3] = 100;
            simulator.mainMemory[4] = 13;
            simulator.mainMemory[5] = 25;

            simulator.mainMemory[6] = 15;

            //conditionally jump to 2 if A < B... if A < 25 keep looping 
            simulator.mainMemory[7] = 10;
            simulator.mainMemory[8] = 2;

            simulator.mainMemory[9] = 2;

            simulator.mainMemory[10] = 16;

            simulator.mainMemory[100] = 1;

            simulator.runSimulation();
            Assert.AreEqual(25, simulator.OutRegister);
            Assert.AreEqual(25, simulator.ARegister);
            Assert.AreEqual(25, simulator.BRegister);

        }
    }
}