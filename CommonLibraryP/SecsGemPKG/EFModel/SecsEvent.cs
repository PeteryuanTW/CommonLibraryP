using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsEvent
    {
        public Guid Id { get; set; }
        public int S { get; set; }
        public int F { get; set; }
        public Guid SourceNodeId {  get; set; }
        public Guid ReplyNodeId { get; set; }
    }
}
