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
            var simulator = new simulator.simulator(16, 1000);
            simulator.mainMemory[0] = 15.ToBinary();
            simulator.mainMemory[1] = 6.ToBinary();
            simulator.mainMemory[2] = 20.ToBinary();

            simulator.runSimulation();
            Assert.AreEqual(0, simulator.ARegister.ToNumeral());

        }

        [Test]
        public void canAdd()
        {
            var simulator = new simulator.simulator(16, 1000);
            simulator.mainMemory[0] = 6.ToBinary();
            simulator.mainMemory[1] = 20.ToBinary();
            simulator.mainMemory[2] = 3.ToBinary();
            simulator.mainMemory[3] = 100.ToBinary();
            simulator.mainMemory[4] = 2.ToBinary();
            simulator.mainMemory[5] = 15.ToBinary();

            simulator.mainMemory[6] = 6.ToBinary();
            simulator.mainMemory[7] = 20.ToBinary();

            simulator.mainMemory[100] = 5.ToBinary();

            simulator.runSimulation();
            Assert.AreEqual(25, simulator.ARegister.ToNumeral());
            Assert.AreEqual(25, simulator.OutRegister.ToNumeral());

        }

        [Test]
        public void canJump()
        {
            var simulator = new simulator.simulator(16, 1000);
            simulator.mainMemory[0] = 6.ToBinary();
            simulator.mainMemory[1] = 20.ToBinary();
            simulator.mainMemory[2] = 14.ToBinary();
            simulator.mainMemory[3] = 7.ToBinary();
            simulator.mainMemory[4] = 6.ToBinary();

            simulator.mainMemory[5] = 2.ToBinary();
            simulator.mainMemory[6] = 15.ToBinary();


            simulator.runSimulation();
            Assert.AreEqual(0, simulator.OutRegister.ToNumeral());
            Assert.AreEqual(20, simulator.ARegister.ToNumeral());

        }

        [Test]
        public void canJumpConditionally()
        {
            var simulator = new simulator.simulator(16, 1000);
            simulator.mainMemory[0] = 6.ToBinary();
            simulator.mainMemory[1] = 20.ToBinary();
            simulator.mainMemory[2] = 3.ToBinary();
            simulator.mainMemory[3] = 100.ToBinary();
            simulator.mainMemory[4] = 12.ToBinary();
            simulator.mainMemory[5] = 25.ToBinary();

            simulator.mainMemory[6] = 14.ToBinary();

            //conditionally jump to 2 if A < B... if A < 25 keep looping 
            simulator.mainMemory[7] = 9.ToBinary();
            simulator.mainMemory[8] = 2.ToBinary();

            simulator.mainMemory[9] = 2.ToBinary();

            simulator.mainMemory[10] = 15.ToBinary();

            simulator.mainMemory[100] = 1.ToBinary();

            simulator.runSimulation();
            Assert.AreEqual(25, simulator.OutRegister.ToNumeral());
            Assert.AreEqual(25, simulator.ARegister.ToNumeral());
            Assert.AreEqual(25, simulator.BRegister.ToNumeral());

        }
    }
}