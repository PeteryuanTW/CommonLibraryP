using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class TMRobotModbusTCP : ModbusTCPMachine
    {
        public TMRobotModbusTCP() : base()
        {

        }

        protected override async Task UpdateStatus()
        {
            //start address casting to ushort
            bool[] statusList = await master.ReadInputsAsync((byte)1, (ushort)7200, (ushort)14);
            if (statusList[8])
            {
                Stop();
            }
            else
            {
                if (statusList[1])
                {
                    Error("Error from robot");
                }
                else
                {
                    if (statusList[4])
                    {
                        Pause();
                    }
                    else
                    {
                        if (statusList[2])
                        {
                            Running();
                        }
                        else
                        {
                            Idle();
                        }
                    }
                }
            }
            //await base.UpdateStatus();
        }

        public override async Task ManualRun()
        {
            await master?.WriteSingleCoilAsync((byte)1, (ushort)7103, true);
        }

        public async Task ManualPause()
        {
            await master?.WriteSingleCoilAsync((byte)1, (ushort)7108, true);
        }

        public override async Task ManualStop()
        {
            await master?.WriteSingleCoilAsync((byte)1, 7105, true);
        }

        public async Task ManualSpeedUp()
        {
            await master?.WriteSingleCoilAsync((byte)1, 7106, true);
        }

        public async Task ManualSlowDown()
        {
            await master?.WriteSingleCoilAsync((byte)1, 7107, true);
        }
    }
}
