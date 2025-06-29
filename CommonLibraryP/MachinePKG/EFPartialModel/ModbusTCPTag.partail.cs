using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public partial class ModbusTCPTag
    {
        public ModbusTCPTag() : base()
        {

        }
        public ModbusTCPTag(Guid CategoryID) : base(CategoryID)
        {
            //base(CategoryID);
        }

        protected override void InitVal()
        {
            switch (DataType)
            {
                case 1:
                    SetValue(false);
                    break;
                case 2:
                    SetValue((ushort)0);
                    break;
                case 4:
                    SetValue(string.Empty);
                    break;
                case 11:
                    SetValue(new bool[Offset]);
                    break;
                case 22:
                    SetValue(new ushort[Offset]);
                    break;
                default:
                    break;
            }
        }
    }
}
