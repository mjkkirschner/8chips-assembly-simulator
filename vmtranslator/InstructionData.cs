//Information about a parsed VM command
using System.IO;

namespace vmtranslator
{
    public class InstructionData
    {

        public vmILParser.vmCommandType CommandType { get; private set; }
        public object CommmandObject { get; private set; }

        public string[] Operands { get; private set; }

        public string VMFilePath { get; private set; }
        public string VMFunction { get; private set; }

        public InstructionData(vmILParser.vmCommandType commandType, object parsedCommandObject, string[] stringOperands, string vmfilePath, string vmfunction)
        {
            this.CommandType = commandType;
            this.CommmandObject = parsedCommandObject;
            this.Operands = stringOperands;
            this.VMFilePath = vmfilePath;
            this.VMFunction = vmfunction;
        }

    }

}
